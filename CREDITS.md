# Credits

`dagp-mcp-unity` is an independent reimplementation written from scratch for the DAGP workflow.

## Inspiration

The tool naming, WebSocket bridge pattern, and JSON-RPC message shape were inspired by:

- **[`com.gamelovers.mcp-unity`](https://github.com/CoderGamester/mcp-unity)** by CoderGamester - MIT license. Pioneered the Unity Editor + MCP integration pattern. We share no code with it, but several of our base tools have matching names so MCP clients written for one are easier to migrate to the other.

If you only need the base toolset (scene/gameobject/material introspection), `mcp-unity` is mature and battle-tested. `dagp-mcp-unity` exists because we needed game-view screenshot capture, play-mode automation, input simulation, and runtime probing - features outside the scope of `mcp-unity` at the time of writing (v1.2.0).

## Protocol

- [Model Context Protocol](https://modelcontextprotocol.io/) - Anthropic
- [`@modelcontextprotocol/sdk`](https://github.com/modelcontextprotocol/typescript-sdk) - TypeScript SDK
