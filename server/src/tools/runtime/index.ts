import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

export const getFieldSchema = z.object({
  name: z.string().optional().describe("GameObject name."),
  instanceId: z.number().optional(),
  componentType: z.string().describe("Component class name (e.g. 'ScoreManager', 'PlayerHealth')."),
  field: z.string().describe("Field or property name on the component."),
});

export const invokeMethodSchema = z.object({
  name: z.string().optional(),
  instanceId: z.number().optional(),
  componentType: z.string().describe("Component class name."),
  method: z.string().describe("Public method name."),
  args: z.array(z.any()).optional().describe("Positional arguments matched to method signature."),
});

export const waitUntilSchema = z.object({
  condition: z.enum(["gameobject_exists", "gameobject_active", "field_equals", "is_playing", "scene_loaded"]),
  name: z.string().optional().describe("GameObject name (for gameobject_* and field_equals)."),
  componentType: z.string().optional().describe("Component class (for field_equals)."),
  field: z.string().optional().describe("Field name (for field_equals)."),
  value: z.any().optional().describe("Expected value (for field_equals; or true/false for is_playing)."),
  sceneName: z.string().optional().describe("Scene name (for scene_loaded)."),
  timeoutMs: z.number().optional().default(5000),
  pollMs: z.number().optional().default(100),
});

export const runtimeTools = (unity: UnityClient) => [
  {
    name: "runtime.get_field",
    description: "Reads a field or property from a MonoBehaviour at runtime. Includes private fields. Returns kind ('field'/'property') and value. Use to inspect game state (score, health, etc).",
    inputSchema: zodToJsonSchema(getFieldSchema),
    handler: async (args: unknown) => unity.call("runtime.get_field", getFieldSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "runtime.invoke_method",
    description: "Calls a public method on a MonoBehaviour at runtime. Bypasses input — useful for direct game actions (e.g. Score.AddPoints(10), Player.Jump()).",
    inputSchema: zodToJsonSchema(invokeMethodSchema),
    handler: async (args: unknown) => unity.call("runtime.invoke_method", invokeMethodSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "runtime.wait_until",
    description: "Polls a condition every pollMs until satisfied or timeoutMs. Conditions: gameobject_exists, gameobject_active, field_equals, is_playing, scene_loaded. Throws on timeout.",
    inputSchema: zodToJsonSchema(waitUntilSchema),
    handler: async (args: unknown) => unity.call("runtime.wait_until", waitUntilSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
