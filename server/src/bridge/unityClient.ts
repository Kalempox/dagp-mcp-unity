import net from "node:net";
import { randomUUID } from "node:crypto";

interface JsonRpcRequest {
  jsonrpc: "2.0";
  id: string;
  method: string;
  params: Record<string, unknown>;
}

interface JsonRpcResponse {
  jsonrpc?: "2.0";
  id: string;
  result?: unknown;
  error?: { code: number; message: string };
}

type Pending = {
  resolve: (value: unknown) => void;
  reject: (err: Error) => void;
};

/**
 * Persistent TCP connection to Unity Editor's DAGPBridge.
 * Protocol: newline-delimited JSON-RPC 2.0.
 *
 * Resilience policy: NEVER give up. No request timeouts, infinite reconnect.
 * The MCP host should always see a healthy server even if Unity is mid-recompile
 * for minutes. Calls block until Unity comes back and answers.
 */
export class UnityClient {
  private socket?: net.Socket;
  private pending = new Map<string, Pending>();
  private connecting?: Promise<void>;
  private buffer = "";
  private closed = false;

  constructor(
    private readonly host: string,
    private readonly port: number
  ) {}

  async ensureConnected(): Promise<void> {
    if (this.socket && !this.socket.destroyed) return;
    if (this.connecting) return this.connecting;
    this.connecting = this.connectForever();
    try { await this.connecting; } finally { this.connecting = undefined; }
  }

  /**
   * Reconnect indefinitely with capped backoff. Unity recompile / domain reload
   * can take 20s+ on big projects; sometimes the editor is paused or being
   * restarted. We just keep trying — never surface ECONNREFUSED to the caller.
   */
  private async connectForever(): Promise<void> {
    let attempt = 0;
    while (!this.closed) {
      try { await this.connect(); return; }
      catch (err) {
        attempt++;
        const delay = Math.min(5000, 250 * Math.pow(1.5, Math.min(attempt, 12)));
        if (attempt === 1 || attempt % 10 === 0) {
          process.stderr.write(`[dagp-mcp-unity] Unity bridge not reachable (attempt ${attempt}): ${(err as Error).message}\n`);
        }
        await new Promise((r) => setTimeout(r, delay));
      }
    }
    throw new Error("client closing");
  }

  private connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      const sock = net.createConnection({ host: this.host, port: this.port });
      // Attach permanent error handler immediately so a stray 'error' after
      // initial settle never escalates to uncaughtException.
      sock.on("error", () => { /* surfaced via close */ });
      const onConnect = () => {
        sock.setEncoding("utf8");
        this.socket = sock;
        this.attachHandlers(sock);
        resolve();
      };
      const onErr = (err: Error) => {
        sock.off("connect", onConnect);
        reject(err);
      };
      sock.once("connect", onConnect);
      sock.once("error", onErr);
    });
  }

  private attachHandlers(sock: net.Socket) {
    sock.on("data", (chunk: string) => this.onData(chunk));
    sock.on("close", () => {
      this.socket = undefined;
      this.buffer = "";
      this.failAllPending(new Error("Unity bridge closed"));
    });
  }

  private onData(chunk: string) {
    this.buffer += chunk;
    let nl: number;
    while ((nl = this.buffer.indexOf("\n")) !== -1) {
      const line = this.buffer.slice(0, nl).trim();
      this.buffer = this.buffer.slice(nl + 1);
      if (!line) continue;
      this.dispatchLine(line);
    }
  }

  private dispatchLine(line: string) {
    let msg: JsonRpcResponse;
    try { msg = JSON.parse(line); }
    catch { return; }
    const pending = this.pending.get(msg.id);
    if (!pending) return;
    this.pending.delete(msg.id);
    if (msg.error) pending.reject(new Error(`Unity error ${msg.error.code}: ${msg.error.message}`));
    else pending.resolve(msg.result);
  }

  private failAllPending(err: Error) {
    for (const [, p] of this.pending) { p.reject(err); }
    this.pending.clear();
  }

  /**
   * Wait for Unity to go through a domain reload cycle and come back online.
   * Phase 1: wait up to disconnectTimeoutMs for the socket to drop (reload started).
   * Phase 2: wait up to reconnectTimeoutMs for the socket to come back (reload done).
   *
   * Call this immediately after project.recompile or play.enter (when domain reload
   * is expected) so the next tool call is guaranteed to find a live bridge.
   */
  async awaitReconnect(disconnectTimeoutMs = 3000, reconnectTimeoutMs = 30_000): Promise<void> {
    // Phase 1: wait for the socket to drop (domain reload beginning)
    const t1 = Date.now() + disconnectTimeoutMs;
    while (Date.now() < t1) {
      if (!this.socket || this.socket.destroyed) break;
      await new Promise(r => setTimeout(r, 150));
    }

    // Phase 2: wait for socket to come back (domain reload finished)
    const t2 = Date.now() + reconnectTimeoutMs;
    while (Date.now() < t2 && !this.closed) {
      if (this.socket && !this.socket.destroyed) return;
      // Kick off reconnection if not already in progress
      if (!this.connecting) this.ensureConnected().catch(() => { /* retried internally */ });
      await new Promise(r => setTimeout(r, 300));
    }

    if (this.socket && !this.socket.destroyed) return;
    throw new Error("Unity bridge did not recover after domain reload");
  }

  /**
   * Send a JSON-RPC tool call to Unity and await the response.
   * No request timeout: blocks until Unity answers or the client is closed.
   * On mid-flight drops (Unity recompile / domain reload) retries indefinitely.
   */
  async call<T = unknown>(method: string, params: Record<string, unknown> = {}): Promise<T> {
    while (!this.closed) {
      try { return await this.callOnce<T>(method, params); }
      catch (err) {
        const msg = (err as Error).message ?? "";
        const isTransient =
          msg.includes("Unity bridge closed") ||
          msg.includes("Unity bridge not connected") ||
          msg.includes("ECONNREFUSED") ||
          msg.includes("ECONNRESET") ||
          msg.includes("EPIPE");
        if (!isTransient) throw err;
        process.stderr.write(`[dagp-mcp-unity] '${method}' interrupted (${msg}); retrying after Unity reconnects.\n`);
        await new Promise((r) => setTimeout(r, 750));
      }
    }
    throw new Error("client closing");
  }

  private async callOnce<T = unknown>(method: string, params: Record<string, unknown> = {}): Promise<T> {
    await this.ensureConnected();
    if (!this.socket || this.socket.destroyed) throw new Error("Unity bridge not connected");

    const id = randomUUID();
    const req: JsonRpcRequest = { jsonrpc: "2.0", id, method, params };

    return new Promise<T>((resolve, reject) => {
      this.pending.set(id, { resolve: (v) => resolve(v as T), reject });
      this.socket!.write(JSON.stringify(req) + "\n", (err) => {
        if (err) { this.pending.delete(id); reject(err); }
      });
    });
  }

  close() {
    this.closed = true;
    this.failAllPending(new Error("client closing"));
    try { this.socket?.destroy(); } catch { /* noop */ }
  }
}
