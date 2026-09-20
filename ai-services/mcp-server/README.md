# MCP Server

Shared ASP.NET Core service for controlled Model Context Protocol tools.

The current project is an infrastructure stub with live and ready health
endpoints. Tool registration, caller boundaries, and structured tool contracts
will be added in a later implementation slice.

The container is available only to services attached to the Docker Compose
`ai` network and does not publish a host port.
