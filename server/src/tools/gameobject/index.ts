import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

// ─── Shared sub-schemas ───────────────────────────────────────────────────────

const vector3 = z.object({
  x: z.number().optional().default(0),
  y: z.number().optional().default(0),
  z: z.number().optional().default(0),
});

const target = {
  name: z.string().optional().describe("GameObject name or 'Parent/Child' path."),
  instanceId: z.number().optional().describe("Numeric Unity InstanceID. Preferred when names collide."),
};

// ─── Schemas ──────────────────────────────────────────────────────────────────

export const getSchema = z.object({ ...target });

export const addAssetSchema = z.object({
  assetPath: z.string().optional().describe("Asset path of a prefab to instantiate (e.g. 'Assets/Prefabs/Cube.prefab'). Mutually exclusive with primitiveType."),
  primitiveType: z.enum(["Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad"]).optional().describe("Built-in primitive to create. Mutually exclusive with assetPath."),
  name: z.string().optional().describe("Override name of the created object."),
  parent: z.string().optional().describe("Parent GameObject name to attach under. Defaults to scene root."),
  position: vector3.optional().describe("World position. Defaults to (0,0,0)."),
});

export const deleteSchema = z.object({ ...target });
export const duplicateSchema = z.object({ ...target, newName: z.string().optional() });
export const reparentSchema = z.object({
  ...target,
  parent: z.string().optional().describe("New parent GameObject name. Omit / null to detach to scene root."),
  worldPositionStays: z.boolean().optional().default(true),
});
export const updateSchema = z.object({
  ...target,
  newName: z.string().optional(),
  active: z.boolean().optional(),
  tag: z.string().optional(),
  layer: z.string().optional().describe("Layer name (string). Use a recognised Unity layer."),
  isStatic: z.boolean().optional(),
});
export const selectSchema = z.object({ ...target });

// ─── Handlers ─────────────────────────────────────────────────────────────────

export const gameObjectTools = (unity: UnityClient) => [
  {
    name: "gameobject.get",
    description: "Returns a GameObject's transform, components, parent, children, scene, layer, tag.",
    inputSchema: zodToJsonSchema(getSchema),
    handler: async (args: unknown) => unity.call("gameobject.get", getSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "gameobject.add_asset",
    description: "Adds a GameObject to the active scene. Either instantiates a prefab (assetPath) or creates a primitive (primitiveType). Provide one or neither (creates empty).",
    inputSchema: zodToJsonSchema(addAssetSchema),
    handler: async (args: unknown) => unity.call("gameobject.add_asset", addAssetSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "gameobject.delete",
    description: "Destroys a GameObject (with Undo).",
    inputSchema: zodToJsonSchema(deleteSchema),
    handler: async (args: unknown) => unity.call("gameobject.delete", deleteSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "gameobject.duplicate",
    description: "Clones a GameObject (and its hierarchy). Optionally renames the clone.",
    inputSchema: zodToJsonSchema(duplicateSchema),
    handler: async (args: unknown) => unity.call("gameobject.duplicate", duplicateSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "gameobject.reparent",
    description: "Re-parents a GameObject. Pass parent='' or omit to detach to scene root.",
    inputSchema: zodToJsonSchema(reparentSchema),
    handler: async (args: unknown) => unity.call("gameobject.reparent", reparentSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "gameobject.update",
    description: "Updates GameObject properties: newName, active, tag, layer, isStatic. Only specified fields change.",
    inputSchema: zodToJsonSchema(updateSchema),
    handler: async (args: unknown) => unity.call("gameobject.update", updateSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "gameobject.select",
    description: "Selects a GameObject in the Editor (Hierarchy + Inspector focus + ping).",
    inputSchema: zodToJsonSchema(selectSchema),
    handler: async (args: unknown) => unity.call("gameobject.select", selectSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
