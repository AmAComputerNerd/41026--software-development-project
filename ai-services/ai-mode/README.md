# ai-mode gateway

Shared OpenRouter proxy. Use this once your feature needs AI-mode — no
need for your own OpenRouter key or account, gateway already holds shared
one.

## Calling it

From inside your own backend container:

```
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

Defaults to MiniMax-M3 (`minimax/minimax-m3:free`) if
you omit `model`. Override by setting `model` in request body.

## docker-compose

Add a health check and use `depends_on: condition: service_healthy` for
`ai-mode`. Its `/health/live` endpoint reports process liveness and
`/health/ready` verifies that the OpenRouter key is configured.
