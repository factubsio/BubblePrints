# BubblePrints MCP bridge

The Bun bridge remains connected to the MCP client and owns a disposable C#
worker. The worker loads one `.binz` file, keeps it resident, and communicates
with the bridge using newline-delimited JSON over stdin/stdout.

Build and install:

```bash
dotnet build BubbleprintMcpWorker/BubbleprintMcpWorker.csproj
cd mcpbridge
bun install
```

Codex configuration:

```toml
[mcp_servers.bubbleprints]
command = "bun"
args = ["run", "/path/to/BubblePrints/mcpbridge/src/index.ts"]

[mcp_servers.bubbleprints.env]
BUBBLEPRINT_WORKER = "/path/to/BubblePrints/BubbleprintMcpWorker/bin/Debug/net8.0/BubbleprintMcpWorker"
```

Bridge tools are `open_binz`, `close_binz`, `list_tools`, `run`, and `status`.
Use `list_tools` after opening a database to discover worker operations,
including `referenced_by` for incoming blueprint references. Pass
`includeContext: true` to locate each reference in its enclosing JSON object
at runtime without increasing the binz size.

Use `json_query` to return bounded portions of one blueprint. It supports
property paths, array indexes and wildcards, recursive filtering, and direct
property projection.
