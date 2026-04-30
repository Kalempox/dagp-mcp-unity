import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

export const uiClickSchema = z.object({
  name: z.string().optional().describe("UGUI GameObject name to click directly (preferred — bypasses raycast)."),
  instanceId: z.number().optional(),
  x: z.number().optional().describe("Screen X. Used with y when name not given — raycasts at this point."),
  y: z.number().optional().describe("Screen Y."),
});

export const sendKeySchema = z.object({
  keyCode: z.number().describe("Windows Virtual-Key code. Common: A=65, Space=32, Left=37, Up=38, Right=39, Down=40, Enter=13, Escape=27, W=87, S=83."),
  holdMs: z.number().optional().default(50).describe("How long to hold the key down before release (ms)."),
  focusGameView: z.boolean().optional().default(true).describe("Auto-focus the Game View before sending. Required for input to reach the game."),
});

export const inputTools = (unity: UnityClient) => [
  {
    name: "input.ui_click",
    description: "Triggers a UGUI button click. Pass a GameObject name (preferred) OR screen x+y (raycasts to find UI element). Works in Edit and Play mode. Requires an EventSystem in scene.",
    inputSchema: zodToJsonSchema(uiClickSchema),
    handler: async (args: unknown) => unity.call("input.ui_click", uiClickSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "input.send_key",
    description: "Sends a real Windows keyboard event via Win32 SendInput. Editor's Game View must be focused (auto-focused by default). Use Virtual-Key codes — see description for common values.",
    inputSchema: zodToJsonSchema(sendKeySchema),
    handler: async (args: unknown) => unity.call("input.send_key", sendKeySchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
