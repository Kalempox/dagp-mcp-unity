import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

export const executeSchema = z.object({
  steps: z.array(z.object({
    method: z.string(),
    params: z.object({}).passthrough().optional(),
  })).describe("Ordered list of tool calls to execute."),
  stopOnError: z.boolean().optional().default(true).describe("If true, halt on first failure. If false, run all and report."),
});

export const batchTools = (unity: UnityClient) => [
  {
    name: "batch.execute",
    description: "Runs N tool calls sequentially in one round-trip. Useful for atomic multi-step setups (e.g. create cube + set transform + assign material).",
    inputSchema: zodToJsonSchema(executeSchema),
    handler: async (args: unknown) => unity.call("batch.execute", executeSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
