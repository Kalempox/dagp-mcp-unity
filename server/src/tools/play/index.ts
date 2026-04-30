import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

export const enterSchema = z.object({});
export const exitSchema = z.object({});
export const isPlayingSchema = z.object({});

export const playTools = (unity: UnityClient) => [
  {
    name: "play.enter",
    description: "Requests Unity to enter Play Mode. Returns immediately — caller should poll play.is_playing. Note: by default Unity reloads the domain on Play, which briefly disconnects the bridge (~1-3s). Disable Domain Reload in Editor settings for instant transitions.",
    inputSchema: zodToJsonSchema(enterSchema),
    handler: async (args: unknown) => unity.call("play.enter", enterSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "play.exit",
    description: "Requests Unity to exit Play Mode. Returns immediately — caller should poll play.is_playing.",
    inputSchema: zodToJsonSchema(exitSchema),
    handler: async (args: unknown) => unity.call("play.exit", exitSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "play.is_playing",
    description: "Returns editor play state: isPlaying, isPaused, isCompiling, frameCount, timeSinceStartup. Poll this after play.enter / play.exit to confirm transition.",
    inputSchema: zodToJsonSchema(isPlayingSchema),
    handler: async (args: unknown) => unity.call("play.is_playing", isPlayingSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
