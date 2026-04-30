import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

export const updateSchema = z.object({
  name: z.string().optional().describe("Target GameObject name or 'Parent/Child' path."),
  instanceId: z.number().optional(),
  componentType: z.string().describe("Component short name ('Rigidbody', 'BoxCollider') or full type ('UnityEngine.Rigidbody')."),
  add: z.boolean().optional().default(false).describe("If true, add the component when missing. Default false (errors if missing)."),
  fields: z.object({}).passthrough().optional().describe("Map of serialized field name to value. Supports primitives, Vector3 ({x,y,z}), Color ({r,g,b,a})."),
});

export const componentTools = (unity: UnityClient) => [
  {
    name: "component.update",
    description: "Adds (optional) and/or modifies serialized fields on a Component of a GameObject. Uses SerializedObject so changes are Editor-safe.",
    inputSchema: zodToJsonSchema(updateSchema),
    handler: async (args: unknown) => unity.call("component.update", updateSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
