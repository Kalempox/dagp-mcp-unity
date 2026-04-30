# DAGP Unity MCP - Editor Package

Editor-only package. Spins up a WebSocket server on Editor load that accepts JSON-RPC tool calls from the [DAGP MCP Node server](../server) and dispatches them to Unity APIs.

See repo root [README](../README.md) for install + architecture.

## Settings

`Edit > Project Settings > DAGP MCP` (Phase 1+) or `EditorPrefs` keys:

- `DAGP.MCP.Port` - WebSocket port (default `8090`)
- `DAGP.MCP.AutoStart` - Start bridge on Editor load (default `true`)

## Menu

- `DAGP/MCP Status` - Shows bridge state, port, last message
- `DAGP/Restart Bridge` - Stop + start the WebSocket server
