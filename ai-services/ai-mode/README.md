# ai-mode gateway

Shared OpenRouter proxy. Use this once your feature needs AI-mode — no
need for your own OpenRouter key or account, gateway already holds shared
one.

```mermaid
flowchart LR
    subgraph Backends["Containerised Backends"]
        S1["student-1-backend"]
        S2["student-2-backend"]
        S3["student-3-backend"]
        S4["student-4-backend"]
        S5["student-5-backend"]
    end

    subgraph AIServices["AI Services"]
        RAG["rag-server (:8080, internal)"]
        AIM["ai-mode (:8080, internal)<br>(Holds OPENROUTER_API_KEY)"]
    end

    Backends -- "ai-mode:8080<br>/v1/chat/completions" --> AIM
    RAG -- "ai-mode:8080" --> AIM
    AIM -- "HTTPS (Bearer Token)" --> OpenRouter["OpenRouter API<br>(nemotron-3.5-lightning:free)"]
```

## Calling it

Start the shared AI services from the repository root:

```bash
docker compose up --build -d ai-mode mcp-server rag-server
```

From inside a backend container:

```http
POST http://ai-mode:8080/v1/chat/completions
```

Same request shape as OpenAI's chat completions API:

```json
{
  "messages": [
    { "role": "user", "content": "Summarise these notifications..." }
  ]
}
```

The response body is passed through unchanged. If a provider embeds an
`error` object inside an HTTP 200 response, the gateway converts its error
code into the HTTP response status so callers can handle and retry it
correctly.

## Model

When a request omits `model`, AI Mode uses the required `OPENROUTER_MODEL`
value generated from `.env.example`. A request can still override that model
explicitly.

## Health

AI Mode runs in Docker Compose. Its `/health/live` endpoint reports
process liveness and `/health/ready` verifies that the OpenRouter key and
model loaded from the root `.env` are configured.
