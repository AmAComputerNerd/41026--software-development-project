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

Response is the raw OpenRouter response, passed through as-is.

## Model

Defaults to Nemotron 3 Ultra (`nvidia/nemotron-3-ultra-550b-a55b:free`) if
you omit `model`. Override by setting `model` in request body.

## docker-compose

Add `depends_on: ai-mode` to your backend service.
