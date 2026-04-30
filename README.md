# dagp-mcp-unity

DAGP-specific Model Context Protocol server for Unity Editor. Lets Claude (or any MCP-enabled host) drive Unity for asset placement, scene inspection, play-mode automation, screenshot capture, input simulation, and runtime probing.

Built for the [DAGP](https://github.com/Kalempox/DAGP) (Durable Autonomous Gamedev Project) workflow, but works standalone.

## Status — Phase 0 (Skeleton)

- [x] Repo scaffold (Unity UPM package + Node.js MCP server)
- [x] WebSocket bridge (Unity Editor <-> Node)
- [x] Tool registry pattern
- [x] `ping` tool (proves end-to-end pipeline)
- [ ] Phase 1: 22 base tools (scene/gameobject/material/component/console/menu/tests)
- [ ] Phase 2: Visual capture tools (game view, scene view, aspect matrix, visible renderers, canvas layout)
- [ ] Phase 3: Play mode + input simulation
- [ ] Phase 4: Runtime probing + perf
- [ ] Phase 5: DAGP workflow integration

## Architecture

```
Claude (MCP host) --stdio--> Node MCP Server --WebSocket--> Unity Editor Bridge --> Tool Registry --> Unity APIs
                  <----      <----JSON-RPC----  <----     <----                  <----            <----
```

- **Unity package** (`unity-package/`) - UPM package `com.dagp.unity-mcp`. Editor-only. Spins up a WebSocket server on Editor load, dispatches incoming tool calls on the main thread.
- **Node MCP server** (`server/`) - TypeScript. Implements MCP stdio protocol for Claude, forwards tool calls to Unity over WebSocket. One TS handler per tool.
- **Protocol** - JSON-RPC 2.0 over WebSocket. Unity is the server, Node is the client.

## Install (Phase 0)

### 1. Unity package

In your Unity project's `Packages/manifest.json` add:

```json
{
  "dependencies": {
    "com.dagp.unity-mcp": "file:C:/path/to/dagp-mcp-unity/unity-package"
  }
}
```

Or use Unity Package Manager - "Add package from disk" - select `unity-package/package.json`.

Open Unity. The bridge starts automatically on `ws://localhost:8090`. Check `DAGP/MCP Status` menu to verify.

### 2. Node MCP server

```bash
cd server
npm install
npm run build
```

Then register with Claude (e.g. in `~/.claude.json` `mcpServers`):

```json
{
  "mcpServers": {
    "dagp-unity": {
      "command": "node",
      "args": ["C:/path/to/dagp-mcp-unity/server/build/index.js"]
    }
  }
}
```

### 3. Verify

Restart Claude. Call the `ping` tool. Expected response includes `unityVersion`, `editorPlatform`, `activeScene`.

## Phase 0 Scope

Single tool: `ping`. Goal is to prove the pipeline (Claude -> Node -> WebSocket -> Unity -> response) works end-to-end before adding 50+ tools.

## License

MIT. See [LICENSE](LICENSE). Tool naming/protocol shape inspired by [`com.gamelovers.mcp-unity`](https://github.com/CoderGamester/mcp-unity) (CoderGamester, MIT) - see [CREDITS.md](CREDITS.md).
