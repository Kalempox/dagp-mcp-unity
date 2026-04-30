import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

export const addPackageSchema = z.object({
  identifier: z.string().describe("Unity package identifier: name ('com.unity.cinemachine'), name@version, file:./path, or git URL."),
});

export const recompileSchema = z.object({});

export const createPrefabSchema = z.object({
  name: z.string().optional().describe("Target GameObject name. Defaults to source GameObject's name."),
  instanceId: z.number().optional(),
  savePath: z.string().optional().default("Assets/Prefabs").describe("Folder under Assets/ to save the .prefab. Created if missing."),
  connect: z.boolean().optional().default(true).describe("If true, source GameObject becomes a prefab instance after save."),
});

export const projectTools = (unity: UnityClient) => [
  {
    name: "project.add_package",
    description: "Installs a Unity package via Package Manager. Supports name, name@version, file:, and git URLs.",
    inputSchema: zodToJsonSchema(addPackageSchema),
    handler: async (args: unknown) => unity.call("project.add_package", addPackageSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "project.recompile",
    description: "Forces a script recompile + asset refresh. Use after editing C# files outside Unity.",
    inputSchema: zodToJsonSchema(recompileSchema),
    handler: async (args: unknown) => unity.call("project.recompile", recompileSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "project.create_prefab",
    description: "Creates a prefab asset from a scene GameObject. Saves under Assets/Prefabs/ by default.",
    inputSchema: zodToJsonSchema(createPrefabSchema),
    handler: async (args: unknown) => unity.call("project.create_prefab", createPrefabSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
