import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

const vector3 = z.object({
  x: z.number().optional().default(0),
  y: z.number().optional().default(0),
  z: z.number().optional().default(0),
});

const target = {
  name: z.string().optional().describe("GameObject name or 'Parent/Child' path."),
  instanceId: z.number().optional().describe("Numeric Unity InstanceID."),
};

export const setSchema = z.object({
  ...target,
  position: vector3.optional().describe("Absolute position. Set with 'space'."),
  rotation: z.object({
    x: z.number().optional().default(0),
    y: z.number().optional().default(0),
    z: z.number().optional().default(0),
    w: z.number().optional().describe("If provided, treats rotation as quaternion. Otherwise euler degrees."),
  }).optional(),
  scale: vector3.optional().describe("Absolute localScale (always local space)."),
  space: z.enum(["world", "local"]).optional().default("world"),
});

export const moveSchema = z.object({
  ...target,
  delta: vector3.describe("Translation delta in chosen space."),
  space: z.enum(["world", "self"]).optional().default("world"),
});

export const rotateSchema = z.object({
  ...target,
  euler: vector3.describe("Rotation delta in degrees (x=pitch, y=yaw, z=roll)."),
  space: z.enum(["world", "self"]).optional().default("world"),
});

export const scaleSchema = z.object({
  ...target,
  scale: vector3.describe("Scale vector."),
  mode: z.enum(["set", "multiply"]).optional().default("set").describe("'set' replaces localScale; 'multiply' componentwise multiplies."),
});

export const transformTools = (unity: UnityClient) => [
  {
    name: "transform.set",
    description: "Sets absolute position, rotation (euler or quaternion), and/or localScale on a GameObject's Transform. Only specified fields change.",
    inputSchema: zodToJsonSchema(setSchema),
    handler: async (args: unknown) => unity.call("transform.set", setSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "transform.move",
    description: "Translates a GameObject by a delta vector in world or self space.",
    inputSchema: zodToJsonSchema(moveSchema),
    handler: async (args: unknown) => unity.call("transform.move", moveSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "transform.rotate",
    description: "Rotates a GameObject by euler delta degrees in world or self space.",
    inputSchema: zodToJsonSchema(rotateSchema),
    handler: async (args: unknown) => unity.call("transform.rotate", rotateSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "transform.scale",
    description: "Sets or multiplies a GameObject's localScale.",
    inputSchema: zodToJsonSchema(scaleSchema),
    handler: async (args: unknown) => unity.call("transform.scale", scaleSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
