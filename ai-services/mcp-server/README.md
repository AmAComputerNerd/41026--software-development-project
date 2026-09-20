# MCP Server

Shared ASP.NET Core service for controlled Model Context Protocol tools using
the official C# MCP SDK and stateless Streamable HTTP transport.

## Registered tools

### `deadlines_list_upcoming`

- Caller: `student-3-backend`
- Inputs: `days` (`1`-`90`), `limit` (`1`-`50`)
- Output: incomplete tasks due in the requested window
- Boundary: reads through Student 3's bounded HTTP API; it never accesses the
  Student 3 database service or volume directly

The MCP endpoint is `/mcp`. Access is limited by Docker network membership:
the container is available only to services attached to the Docker Compose
`ai` network and does not publish a host port.
