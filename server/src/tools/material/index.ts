import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

const color = z.object({
  r: z.number().optional().default(1),
  g: z.number().optional().default(1),
  b: z.number().optional().default(1),
  a: z.number().optional().default(1),
});

const target = {
  name: z.string().optional().describe("Target GameObject name or 'Parent/Child' path."),
  instanceId: z.number().optional(),
};

export const createSchema = z.object({
  name: z.string().describe("Material asset name (without extension)."),
  shader: z.string().optional().describe("Shader name (e.g. 'Universal Render Pipeline/Lit', 'Standard'). Auto-detects URP/Standard if omitted."),
  savePath: z.string().optional().default("Assets/Materials").describe("Folder under Assets/ to save the .mat. Created if missing."),
  color: color.optional().describe("Initial main color (set on _BaseColor or _Color)."),
});

export const modifySchema = z.object({
  path: z.string().describe("Asset path of the material (e.g. 'Assets/Materials/Red.mat')."),
  color: color.optional().describe("Shorthand for main color (_BaseColor or _Color)."),
  floats: z.object({}).passthrough().optional().describe("Map of shader property name to float value, e.g. {_Smoothness: 0.5}."),
  vectors: z.object({}).passthrough().optional().describe("Map of shader property name to {x,y,z,w}."),
  colors: z.object({}).passthrough().optional().describe("Map of shader property name to {r,g,b,a}."),
  textures: z.object({}).passthrough().optional().describe("Map of shader property name to texture asset path."),
});

export const assignSchema = z.object({
  ...target,
  materialPath: z.string().describe("Asset path of the material to assign."),
  slot: z.number().optional().default(0).describe("Renderer material slot index. Default 0."),
});

export const getInfoSchema = z.object({
  path: z.string().describe("Asset path of the material to inspect."),
});

export const materialTools = (unity: UnityClient) => [
  {
    name: "material.create",
    description: "Creates a new Material asset under Assets/. Auto-detects URP vs Standard shader if not specified.",
    inputSchema: zodToJsonSchema(createSchema),
    handler: async (args: unknown) => unity.call("material.create", createSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "material.modify",
    description: "Modifies a Material's shader properties: color (main), floats, vectors, colors, textures (by asset path).",
    inputSchema: zodToJsonSchema(modifySchema),
    handler: async (args: unknown) => unity.call("material.modify", modifySchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "material.assign",
    description: "Assigns a Material to a Renderer's slot on a target GameObject (default slot 0).",
    inputSchema: zodToJsonSchema(assignSchema),
    handler: async (args: unknown) => unity.call("material.assign", assignSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "material.get_info",
    description: "Returns shader name + all key property values (colors, floats, vectors, texture paths) of a Material.",
    inputSchema: zodToJsonSchema(getInfoSchema),
    handler: async (args: unknown) => unity.call("material.get_info", getInfoSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
