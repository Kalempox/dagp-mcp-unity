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
  timer: NodeJS.Timeout;
};

/**
 * Persistent TCP connection to Unity Editor's DAGPBridge.
 * Protocol: newline-delimited JSON-RPC 2.0.
 */
export class UnityClient {
  private socket?: net.Socket;
  private pending = new Map<string, Pending>();
  private connecting?: Promise<void>;
  private buffer = "";

  constructor(
    private readonly host: string,
    private readonly port: number,
    private readonly requestTimeoutMs = 30_000
  ) {}

  async ensureConnected(): Promise<void> {
    if (this.socket && !this.socket.destroyed) return;
    if (this.connecting) return this.connecting;
    this.connecting = this.connectWithRetry();
    try { await this.connecting; } finally { this.connecting = undefined; }
  }

  /**
   * Retries connect with exponential-ish backoff. 12 attempts, ~45s budget.
   * Absorbs Unity bridge restarts during recompile / domain reload — the most common
   * cause of ECONNREFUSED. Larger budget than naive (Unity 6 + URP recompile can take 20s+).
   */
  private async connectWithRetry(maxAttempts = 12): Promise<void> {
    const delays = [250, 500, 1000, 2000, 3000, 4000, 4000, 5000, 5000, 5000, 5000, 5000];
    let lastErr: Error | undefined;
    for (let i = 0; i < maxAttempts; i++) {
      try { await this.connect(); return; }
      catch (err) {
        lastErr = err as Error;
        const delay = delays[Math.min(i, delays.length - 1)];
        await new Promise((r) => setTimeout(r, delay));
      }
    }
    throw new Error(`Unity bridge unreachable after ${maxAttempts} attempts: ${lastErr?.message}`);
  }

  private connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      const sock = net.createConnection({ host: this.host, port: this.port });
      const onConnect = () => {
        sock.off("error", onErr);
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
    sock.on("error", () => { /* surfaced via close */ });
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
    clearTimeout(pending.timer);
    this.pending.delete(msg.id);
    if (msg.error) pending.reject(new Error(`Unity error ${msg.error.code}: ${msg.error.message}`));
    else pending.resolve(msg.result);
  }

  private failAllPending(err: Error) {
    for (const [, p] of this.pending) { clearTimeout(p.timer); p.reject(err); }
    this.pending.clear();
  }

  /**
   * Send a JSON-RPC tool call to Unity and await the response.
   * Auto-retries up to 3 times on mid-flight connection drops (Unity recompile / domain reload).
   */
  async call<T = unknown>(method: string, params: Record<string, unknown> = {}): Promise<T> {
    let lastErr: Error | undefined;
    for (let attempt = 0; attempt < 3; attempt++) {
      try { return await this.callOnce<T>(method, params); }
      catch (err) {
        lastErr = err as Error;
        const msg = lastErr.message ?? "";
        const isTransient =
          msg.includes("Unity bridge closed") ||
          msg.includes("Unity bridge not connected") ||
          msg.includes("ECONNREFUSED") ||
          msg.includes("ECONNRESET") ||
          msg.includes("EPIPE");
        if (!isTransient) throw lastErr;
        // Wait briefly for Unity to come back, then retry.
        await new Promise((r) => setTimeout(r, 1500));
      }
    }
    throw new Error(`Unity call '${method}' failed after 3 attempts: ${lastErr?.message}`);
  }

  private async callOnce<T = unknown>(method: string, params: Record<string, unknown> = {}): Promise<T> {
    await this.ensureConnected();
    if (!this.socket || this.socket.destroyed) throw new Error("Unity bridge not connected");

    const id = randomUUID();
    const req: JsonRpcRequest = { jsonrpc: "2.0", id, method, params };

    return new Promise<T>((resolve, reject) => {
      const timer = setTimeout(() => {
        this.pending.delete(id);
        reject(new Error(`Unity call '${method}' timed out after ${this.requestTimeoutMs}ms`));
      }, this.requestTimeoutMs);
      this.pending.set(id, { resolve: (v) => resolve(v as T), reject, timer });
      this.socket!.write(JSON.stringify(req) + "\n", (err) => {
        if (err) { clearTimeout(timer); this.pending.delete(id); reject(err); }
      });
    });
  }

  close() {
    this.failAllPending(new Error("client closing"));
    try { this.socket?.destroy(); } catch { /* noop */ }
  }
}
