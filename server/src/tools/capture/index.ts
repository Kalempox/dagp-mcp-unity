import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

export const gameViewSchema = z.object({
  width: z.number().optional().default(1920),
  height: z.number().optional().default(1080),
  camera: z.string().optional().describe("Camera GameObject name. Defaults to Camera.main."),
  path: z.string().optional().describe("Optional output path. Default: <project>/.dagp/screenshots/gameview_<ts>.png"),
});

export const sceneViewSchema = z.object({
  width: z.number().optional().default(1280),
  height: z.number().optional().default(720),
  path: z.string().optional(),
});

export const aspectMatrixSchema = z.object({
  preset: z.enum(["mobile", "desktop", "all"]).optional().default("mobile"),
  camera: z.string().optional(),
  resolutions: z.array(z.object({
    name: z.string(),
    width: z.number(),
    height: z.number(),
  })).optional().describe("Custom list overrides preset. Each: {name, width, height}."),
});

export const diffSchema = z.object({
  pathA: z.string().describe("Path of the first PNG."),
  pathB: z.string().describe("Path of the second PNG. Must match pathA dimensions."),
  threshold: z.number().optional().default(5).describe("Per-channel diff (0..255) to count as changed."),
  outPath: z.string().optional().describe("Diff image output path. Default: <project>/.dagp/screenshots/diff_<ts>.png"),
});

export const captureTools = (unity: UnityClient) => [
  {
    name: "capture.game_view",
    description: "Renders the active Camera to a PNG. Works in Edit + Play mode. Use Read on the returned path to view the image.",
    inputSchema: zodToJsonSchema(gameViewSchema),
    handler: async (args: unknown) => unity.call("capture.game_view", gameViewSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "capture.scene_view",
    description: "Renders the active SceneView (designer perspective, ignores game camera framing). Diagnoses 'asset placed but invisible' — SceneView shows everything.",
    inputSchema: zodToJsonSchema(sceneViewSchema),
    handler: async (args: unknown) => unity.call("capture.scene_view", sceneViewSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "capture.aspect_matrix",
    description: "Captures the same camera at multiple resolutions in one call. Detects UI overflow across phone/tablet/desktop. Presets: 'mobile' (3 phone aspects), 'desktop' (3 desktop aspects), 'all' (6).",
    inputSchema: zodToJsonSchema(aspectMatrixSchema),
    handler: async (args: unknown) => unity.call("capture.aspect_matrix", aspectMatrixSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "capture.diff",
    description: "Pixel-diffs two PNG captures of the same dimensions. Returns % changed + a diff image (changed pixels highlighted red). Verdict: identical / near-identical / minor-changes / major-changes.",
    inputSchema: zodToJsonSchema(diffSchema),
    handler: async (args: unknown) => unity.call("capture.diff", diffSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
