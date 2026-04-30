import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

export const executeMenuItemSchema = z.object({
  menuPath: z.string().describe("Menu path (e.g. 'File/Save', 'Window/Package Manager', 'GameObject/3D Object/Cube')."),
});

export const editorTools = (unity: UnityClient) => [
  {
    name: "editor.execute_menu_item",
    description: "Executes a Unity Editor menu item by its menu path.",
    inputSchema: zodToJsonSchema(executeMenuItemSchema),
    handler: async (args: unknown) => unity.call("editor.execute_menu_item", executeMenuItemSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
