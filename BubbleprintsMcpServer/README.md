# BubblePrints MCP Server

`BubbleprintsMcpServer` is the release stdio MCP host. It starts without a
game or dataset argument and exposes these tools:

See [INSTALL.md](INSTALL.md) for cross-platform build instructions and MCP
configuration examples for Codex, Claude, and other stdio clients.

- `open_binz`
- `list_binz`
- `download_binz`
- `close_binz`
- `list_tools`
- `run`
- `status`

Build it with:

```sh
dotnet publish BubbleprintsMcpServer/BubbleprintsMcpServer.csproj -c Release
```

Configure the resulting `BubbleprintsMcpServer` executable as an MCP stdio
command with no arguments. Call `open_binz` before using `list_tools` or `run`.
Its `path` is optional: when omitted, the server opens the UI's `LastLoaded`
dataset from the shared BubblePrints data directory.

`list_binz` reports cached datasets and can optionally fetch the remote version
catalog. `download_binz` downloads an explicit game/version into that shared
cache and returns its path; downloading never opens the dataset implicitly.

The server implements the small MCP JSON-RPC surface directly with
`System.Text.Json`; it does not depend on an MCP SDK. Operation implementations
are shared with `BubbleprintMcpWorker` through `BubbleprintMcpCore`.
