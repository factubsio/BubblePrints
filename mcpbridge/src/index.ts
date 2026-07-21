import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import { WorkerManager } from "./worker.js";

const workerBinary =
  process.env.BUBBLEPRINT_WORKER ??
  "../BubbleprintMcpWorker/bin/Debug/net8.0/BubbleprintMcpWorker";
const worker = new WorkerManager(workerBinary);

const server = new McpServer({
  name: "bubbleprints-bridge",
  version: "0.1.0",
});

server.tool(
  "open_binz",
  "Open a BubblePrints .binz database and keep it loaded for exploration and profiling.",
  { path: z.string().describe("Path to the .binz file") },
  async ({ path }) => {
    const resolved = path.startsWith("~/")
      ? path.replace("~", process.env.HOME ?? "~")
      : path;
    try {
      const response = await worker.open(resolved);
      if (response.error) {
        return { content: [{ type: "text", text: response.error }], isError: true };
      }
      return { content: [{ type: "text", text: JSON.stringify(response.result, null, 2) }] };
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : String(error);
      return { content: [{ type: "text", text: message }], isError: true };
    }
  },
);

server.tool("close_binz", "Close the loaded database worker.", {}, async () => {
  await worker.close();
  return { content: [{ type: "text", text: "Closed." }] };
});

server.tool("list_tools", "List operations supported by the loaded worker.", {}, async () => {
  const response = await worker.request("list", {});
  if (response.error) {
    return { content: [{ type: "text", text: response.error }], isError: true };
  }
  return { content: [{ type: "text", text: JSON.stringify(response.result, null, 2) }] };
});

server.tool(
  "run",
  "Run an exploration or profiling operation against the loaded database.",
  {
    tool: z.string().describe("Worker tool name"),
    args: z.record(z.unknown()).optional().describe("Arguments for the worker tool"),
  },
  async ({ tool, args }) => {
    const response = await worker.request("run", { tool, args: args ?? {} });
    if (response.error) {
      return { content: [{ type: "text", text: response.error }], isError: true };
    }
    return { content: [{ type: "text", text: JSON.stringify(response.result, null, 2) }] };
  },
);

server.tool("status", "Report whether a binz worker is running.", {}, async () => {
  const text = worker.isRunning
    ? `Worker running. Binz: ${worker.currentPath}`
    : "No binz loaded.";
  return { content: [{ type: "text", text }] };
});

const transport = new StdioServerTransport();
await server.connect(transport);
