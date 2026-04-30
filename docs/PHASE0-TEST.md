# Phase 0 — End-to-End Test

Goal: prove `Claude → Node MCP → WebSocket → Unity Editor → back` pipeline works with the `ping` tool.

## Step 1 — Build the Node server

```bash
cd C:/Users/Abdulkadir\ Kalender/Desktop/dagp-mcp-unity/server
npm install
npm run build
```

Expected: `build/index.js` exists. No TypeScript errors.

## Step 2 — Install the Unity package

In a Unity project (e.g. Tetris3D at `D:/Projects/UnityAI/tetris3d/`), edit `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.dagp.unity-mcp": "file:C:/Users/Abdulkadir Kalender/Desktop/dagp-mcp-unity/unity-package",
    "...": "..."
  }
}
```

If the existing `com.gamelovers.mcp-unity` causes port conflicts, change our port via `Edit > Preferences > DAGP MCP` (Phase 1+) or temporarily set `DAGPSettings.Port = 8091` in code.

Open Unity. Console should print:

```
[DAGP-MCP] Registered 1 tool(s): ping
[DAGP-MCP] Bridge listening on ws://localhost:8090/
```

Verify via `DAGP > MCP Status` menu.

## Step 3 — Register the MCP server with Claude

Edit `~/.claude.json` (Windows: `C:/Users/<you>/.claude.json`), add under `mcpServers`:

```json
{
  "mcpServers": {
    "dagp-unity": {
      "command": "node",
      "args": ["C:/Users/Abdulkadir Kalender/Desktop/dagp-mcp-unity/server/build/index.js"]
    }
  }
}
```

Restart Claude Code.

## Step 4 — Call `ping` from Claude

Ask Claude to call the `ping` tool from `dagp-unity` server.

Expected response:

```json
{
  "pong": true,
  "unityVersion": "6000.0.x",
  "editorPlatform": "WindowsEditor",
  "activeScene": "<your scene name>",
  "isPlaying": false,
  "packageVersion": "0.1.0"
}
```

If you see this — Phase 0 PASS. Move to Phase 1.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Node fails: `Cannot find module @modelcontextprotocol/sdk` | `npm install` not run | `cd server && npm install` |
| Unity console: `Bridge start failed: Access denied` | Another process using port 8090 (often `com.gamelovers.mcp-unity`) | Stop the other server OR change `DAGPSettings.Port` |
| Claude: `Unity bridge not connected` | Unity Editor not open OR bridge not started | Open Unity, check `DAGP > MCP Status` |
| `ping` times out at 30s | Main thread blocked or `_listener.IsListening == false` | Restart bridge: `DAGP > Restart Bridge` |
| Unity console: `HttpListenerException: Access denied` | Windows requires URL ACL for non-localhost | Localhost is exempt; if you changed host, run: `netsh http add urlacl url=http://+:8090/ user=Everyone` |

## What Phase 0 Does NOT Have

- No scene/gameobject/material tools (Phase 1)
- No screenshot capture (Phase 2)
- No play-mode control (Phase 3)
- No frame stats (Phase 4)
- No DAGP workflow integration (Phase 5)

Just `ping`. That's the point — prove the pipe before building 50+ tools on top of it.
