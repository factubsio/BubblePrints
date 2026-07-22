# BubblePrints MCP Installation

BubblePrints MCP is a local stdio server. Your MCP client starts the executable
and communicates with it over standard input and output. It does not listen on
a network port.

## Build

Install the .NET 8 SDK, clone this repository, and run from the repository root:

```sh
dotnet publish BubbleprintsMcpServer/BubbleprintsMcpServer.csproj -c Release
```

The executable is written to:

```text
BubbleprintsMcpServer/bin/Release/net8.0/publish/BubbleprintsMcpServer
```

On Windows the filename ends in `.exe`. Use the absolute path to this
executable in your MCP client configuration. The machine running it needs the
.NET 8 runtime.

To publish a self-contained build for a machine without .NET installed, replace
`linux-x64` with the target runtime identifier, such as `win-x64`,
`osx-x64`, or `osx-arm64`:

```sh
dotnet publish BubbleprintsMcpServer/BubbleprintsMcpServer.csproj \
  -c Release -r linux-x64 --self-contained true
```

## Generic MCP Configuration

Configure a stdio MCP server named `bubbleprints`, with no arguments:

```json
{
  "mcpServers": {
    "bubbleprints": {
      "command": "/absolute/path/to/BubbleprintsMcpServer",
      "args": []
    }
  }
}
```

This JSON shape is used by Claude Desktop and several other MCP clients. Put
the `mcpServers` entry in the location specified by your client's
documentation.

Run the server as the same user as the BubblePrints UI. It automatically uses
the same platform application-data directory and therefore sees datasets
downloaded by the UI. On Linux, preserve the UI's `XDG_DATA_HOME` value if you
have overridden it:

```json
{
  "mcpServers": {
    "bubbleprints": {
      "command": "/absolute/path/to/BubbleprintsMcpServer",
      "args": [],
      "env": {
        "XDG_DATA_HOME": "/absolute/path/to/local/share"
      }
    }
  }
}
```

## Codex

Add this to `~/.codex/config.toml`:

```toml
[mcp_servers.bubbleprints]
command = "/absolute/path/to/BubbleprintsMcpServer"
args = []
startup_timeout_sec = 10
tool_timeout_sec = 300
```

If required, add environment variables in a separate table:

```toml
[mcp_servers.bubbleprints.env]
XDG_DATA_HOME = "/absolute/path/to/local/share"
```

Restart Codex after changing the configuration.

## Claude

For Claude Desktop, use the generic JSON configuration above and restart
Claude Desktop.

For Claude Code, register the executable from a terminal:

```sh
claude mcp add --transport stdio bubbleprints -- \
  /absolute/path/to/BubbleprintsMcpServer
```

Confirm registration with:

```sh
claude mcp list
```

The generic JSON configuration can also be imported or adapted for other MCP
clients that support local stdio servers.

## First Test

After restarting the client:

1. Call `list_binz` with `includeRemote: false`.
2. Call `list_binz` with `includeRemote: true` to fetch available versions.
3. Call `download_binz` with a game such as `KM`, optionally using an exact
   version returned by `list_binz`.
4. Pass the returned path to `open_binz`.
5. Call `list_tools`, then use `run` to execute a BubblePrints query.

Supported game names are `Wrath`, `KM`, `RT`, and `DH`. Downloads are saved in
the shared BubblePrints cache but are not opened automatically. `open_binz`
may omit its path when the BubblePrints UI has a valid `LastLoaded` setting.

If startup fails, run the executable directly in a terminal. It should print
`bubbleprints-mcp: ready` to standard error and wait for MCP input. Runtime and
dataset errors are also written to standard error.
