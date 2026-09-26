# RAG Server

Shared ASP.NET Core service for retrieval-augmented generation. It indexes a
curated, read-only Markdown corpus at startup, retrieves relevant sections with
deterministic weighted lexical search, and sends only those sections to
`ai-mode` for grounded generation through OpenRouter.

`POST /api/answers` accepts a question and the `student-3` scope. Responses
include source citations, a deterministic confidence category, and retrieval
counts. Questions without a relevant corpus match return
`insufficient_context` without calling the model.

The initial corpus contains `AGENTS.md`, `docs/architecture/data-flows.md`, and
`student-3/README.md`. Adding a vector store or embedding service is deferred
until corpus size or retrieval quality justifies the additional infrastructure.

The container is available only to services attached to the Docker Compose
`ai` network and does not publish a host port.
