import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

export const visibleRenderersSchema = z.object({
  camera: z.string().optional().describe("Camera name. Defaults to Camera.main."),
  includeUI: z.boolean().optional().default(false).describe("Include CanvasRenderer (UI). Default false."),
});

export const renderTools = (unity: UnityClient) => [
  {
    name: "render.get_visible_renderers",
    description: "Lists Renderers actually visible to a Camera (frustum + layer mask + enabled). Cheaper than a screenshot for binary 'is asset shown?' checks. Returns visible[] and hidden[] arrays with reason flags.",
    inputSchema: zodToJsonSchema(visibleRenderersSchema),
    handler: async (args: unknown) => unity.call("render.get_visible_renderers", visibleRenderersSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
