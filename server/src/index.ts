#!/usr/bin/env node
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { UnityClient } from "./bridge/unityClient.js";
import { ping, pingInputSchema } from "./tools/ping.js";
import { sceneTools } from "./tools/scene/index.js";
import { gameObjectTools } from "./tools/gameobject/index.js";
import { transformTools } from "./tools/transform/index.js";
import { materialTools } from "./tools/material/index.js";
import { componentTools } from "./tools/component/index.js";
import { projectTools } from "./tools/project/index.js";
import { editorTools } from "./tools/editor/index.js";
import { consoleTools } from "./tools/console/index.js";
import { batchTools } from "./tools/batch/index.js";

const UNITY_PORT = Number(process.env.DAGP_UNITY_PORT ?? 8090);
const UNITY_HOST = process.env.DAGP_UNITY_HOST ?? "127.0.0.1";

const unity = new UnityClient(UNITY_HOST, UNITY_PORT);

const server = new Server(
  { name: "dagp-mcp-unity", version: "0.1.0" },
  { capabilities: { tools: {} } }
);

// ---- Tool registry (single place to wire each tool) ----
const tools = [
  {
    name: "ping",
    description:
      "End-to-end health check. Returns Unity version, editor platform, active scene, play state, and package version. Use this first to verify the bridge is alive.",
    inputSchema: { type: "object", properties: {}, additionalProperties: false },
    handler: async (_args: unknown) => {
      pingInputSchema.parse(_args ?? {});
      return ping(unity);
    },
  },
  ...sceneTools(unity),
  ...gameObjectTools(unity),
  ...transformTools(unity),
  ...materialTools(unity),
  ...componentTools(unity),
  ...projectTools(unity),
  ...editorTools(unity),
  ...consoleTools(unity),
  ...batchTools(unity),
];

// ---- MCP wiring ----
server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: tools.map((t) => ({
    name: t.name,
    description: t.description,
    inputSchema: t.inputSchema,
  })),
}));

server.setRequestHandler(CallToolRequestSchema, async (req) => {
  const tool = tools.find((t) => t.name === req.params.name);
  if (!tool) throw new Error(`Unknown tool: ${req.params.name}`);
  try {
    const result = await tool.handler(req.params.arguments);
    return {
      content: [{ type: "text", text: JSON.stringify(result, null, 2) }],
    };
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    return {
      content: [{ type: "text", text: `Error: ${msg}` }],
      isError: true,
    };
  }
});

// ---- Bootstrap ----
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  // Note: stdout is the MCP transport — log only to stderr.
  process.stderr.write(`[dagp-mcp-unity] connected to MCP host. Unity bridge: tcp://${UNITY_HOST}:${UNITY_PORT}\n`);
}

main().catch((err) => {
  process.stderr.write(`[dagp-mcp-unity] fatal: ${err}\n`);
  process.exit(1);
});

// Graceful shutdown
const shutdown = () => { unity.close(); process.exit(0); };
process.on("SIGINT", shutdown);
process.on("SIGTERM", shutdown);
