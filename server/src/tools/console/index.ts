import { z } from "zod";
import type { UnityClient } from "../../bridge/unityClient.js";
import { zodToJsonSchema } from "../../utils/zodToJsonSchema.js";

export const getLogsSchema = z.object({
  count: z.number().optional().default(50).describe("Max entries to return (most recent). 1..2000."),
  type: z.enum(["Log", "Warning", "Error", "Assert", "Exception"]).optional().describe("Filter by log type."),
  contains: z.string().optional().describe("Substring filter on the log message."),
  includeStackTrace: z.boolean().optional().default(false).describe("Include stack trace per entry."),
});

export const sendLogSchema = z.object({
  message: z.string().describe("Message text to log."),
  type: z.enum(["Log", "Warning", "Error"]).optional().default("Log"),
});

export const runTestsSchema = z.object({
  mode: z.enum(["EditMode", "PlayMode"]).optional().default("EditMode"),
  testFilter: z.string().optional().describe("Substring filter on full test name. Omit to run all."),
});

export const consoleTools = (unity: UnityClient) => [
  {
    name: "console.get_logs",
    description: "Returns recent Unity Console log entries. Filter by type and substring. Most recent last.",
    inputSchema: zodToJsonSchema(getLogsSchema),
    handler: async (args: unknown) => unity.call("console.get_logs", getLogsSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "console.send_log",
    description: "Writes a message to Unity Console (Log/Warning/Error). Useful as a heartbeat or test marker.",
    inputSchema: zodToJsonSchema(sendLogSchema),
    handler: async (args: unknown) => unity.call("console.send_log", sendLogSchema.parse(args ?? {}) as Record<string, unknown>),
  },
  {
    name: "editor.run_tests",
    description: "Runs Unity Test Framework tests. Returns passed/failed/skipped counts and failure messages.",
    inputSchema: zodToJsonSchema(runTestsSchema),
    handler: async (args: unknown) => unity.call("editor.run_tests", runTestsSchema.parse(args ?? {}) as Record<string, unknown>),
  },
];
