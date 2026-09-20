# RAG Server

Shared ASP.NET Core service for retrieval-augmented generation.

The current project is an infrastructure stub with live and ready health
endpoints. Corpus ingestion, retrieval, citations, confidence classification,
and insufficient-context handling will be added in a later implementation
slice.

The container is available only to services attached to the Docker Compose
`ai` network and does not publish a host port.
