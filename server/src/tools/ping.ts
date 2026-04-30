import { z } from "zod";
import type { UnityClient } from "../bridge/unityClient.js";

export const pingInputSchema = z.object({}).describe("No parameters.");

export interface PingResult {
  pong: boolean;
  unityVersion: string;
  editorPlatform: string;
  activeScene: string;
  isPlaying: boolean;
  packageVersion: string;
}

/** End-to-end pipeline check: Claude -> Node MCP -> WebSocket -> Unity Editor -> back. */
export async function ping(unity: UnityClient): Promise<PingResult> {
  return unity.call<PingResult>("ping", {});
}
