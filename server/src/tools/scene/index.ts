import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";

// ─── Schemas ──────────────────────────────────────────────────────────────────

export const sceneGetInfoSchema = z.object({});

export const sceneCreateSchema = z.object({
  path: z.string().describe("Asset path, e.g. 'Assets/Scenes/MyScene.unity'. Must start with 'Assets/'."),
  addDefaults: z.boolean().optional().default(true).describe("Add default Camera + Light. Default true."),
  load: z.boolean().optional().default(true).describe("Open the scene after creating. Default true."),
});

export const sceneLoadSchema = z.object({
  path: z.string().describe("Asset path of the scene to load."),
  mode: z.enum(["single", "additive"]).optional().default("single"),
  saveCurrent: z.boolean().optional().default(false).describe("Save current dirty scenes before loading."),
});

export const sceneSaveSchema = z.object({
  all: z.boolean().optional().default(false).describe("Save all open scenes (true) or only the active one (false)."),
});

export const sceneUnloadSchema = z.object({
  path: z.string().describe("Asset path of the loaded scene to unload."),
});

export const sceneDeleteSchema = z.object({
  path: z.string().describe("Asset path of the scene asset to delete. Must be unloaded first."),
});

// ─── Handlers ─────────────────────────────────────────────────────────────────

export const sceneTools = (unity: UnityClient) => [
  {
    name: "scene.get_info",
    description: "Returns active scene name, path, build index, dirty flag, root GameObject names.",
    inputSchema: zodToJsonSchema(sceneGetInfoSchema),
    handler: async (args: unknown) => {
      sceneGetInfoSchema.parse(args ?? {});
      return unity.call("scene.get_info", {});
    },
  },
  {
    name: "scene.create",
    description: "Creates a new scene at an Assets/ path. Optionally adds default Camera+Light and loads it.",
    inputSchema: zodToJsonSchema(sceneCreateSchema),
    handler: async (args: unknown) => {
      const p = sceneCreateSchema.parse(args ?? {});
      return unity.call("scene.create", p as Record<string, unknown>);
    },
  },
  {
    name: "scene.load",
    description: "Loads a scene by path. Mode 'single' replaces current; 'additive' adds alongside.",
    inputSchema: zodToJsonSchema(sceneLoadSchema),
    handler: async (args: unknown) => {
      const p = sceneLoadSchema.parse(args ?? {});
      return unity.call("scene.load", p as Record<string, unknown>);
    },
  },
  {
    name: "scene.save",
    description: "Saves the active scene. Pass all=true to save all open scenes.",
    inputSchema: zodToJsonSchema(sceneSaveSchema),
    handler: async (args: unknown) => {
      const p = sceneSaveSchema.parse(args ?? {});
      return unity.call("scene.save", p as Record<string, unknown>);
    },
  },
  {
    name: "scene.unload",
    description: "Unloads a currently loaded scene by path. Cannot unload the only loaded scene.",
    inputSchema: zodToJsonSchema(sceneUnloadSchema),
    handler: async (args: unknown) => {
      const p = sceneUnloadSchema.parse(args ?? {});
      return unity.call("scene.unload", p as Record<string, unknown>);
    },
  },
  {
    name: "scene.delete",
    description: "Deletes a scene asset from the project. Must be unloaded first.",
    inputSchema: zodToJsonSchema(sceneDeleteSchema),
    handler: async (args: unknown) => {
      const p = sceneDeleteSchema.parse(args ?? {});
      return unity.call("scene.delete", p as Record<string, unknown>);
    },
  },
];

// Minimal Zod -> JSON Schema converter for our flat object schemas.
function zodToJsonSchema(schema: z.ZodObject<any>): Record<string, unknown> {
  const shape = (schema as any)._def.shape();
  const properties: Record<string, unknown> = {};
  const required: string[] = [];
  for (const [key, value] of Object.entries(shape)) {
    const def = (value as any)._def;
    let type: string = "string";
    if (def.typeName === "ZodNumber") type = "number";
    else if (def.typeName === "ZodBoolean") type = "boolean";
    else if (def.typeName === "ZodEnum") type = "string";
    else if (def.typeName === "ZodOptional" || def.typeName === "ZodDefault") {
      const inner = def.innerType._def.typeName;
      if (inner === "ZodNumber") type = "number";
      else if (inner === "ZodBoolean") type = "boolean";
      else type = "string";
    } else if (def.typeName === "ZodString") type = "string";

    const prop: Record<string, unknown> = { type };
    if (def.description) prop.description = def.description;
    if (def.typeName === "ZodEnum") prop.enum = def.values;
    properties[key] = prop;

    const isOptional = def.typeName === "ZodOptional" || def.typeName === "ZodDefault";
    if (!isOptional) required.push(key);
  }
  return {
    type: "object",
    properties,
    ...(required.length ? { required } : {}),
    additionalProperties: false,
  };
}
