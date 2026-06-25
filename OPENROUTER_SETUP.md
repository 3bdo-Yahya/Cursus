# OpenRouter AI Advisor Setup

This guide explains how to configure Cursus AI Advisor to use OpenRouter through the existing `OpenAi` configuration section.

The app uses an OpenAI-compatible chat client. For OpenRouter, configure:

| Key | Purpose |
|-----|---------|
| `OpenAi:ApiKey` | Secret OpenRouter API key. Never commit this value. |
| `OpenAi:BaseUrl` | OpenRouter API endpoint: `https://openrouter.ai/api/v1`. |
| `OpenAi:Model` | Exact OpenRouter model slug, such as `openrouter/free`. |
| `OpenAi:MaxOutputTokenCount` | Maximum answer length returned by AI Advisor. |
| `OpenAi:Temperature` | Creativity/randomness setting. Lower values are more consistent. |
| `OpenAi:TopP` | Nucleus sampling setting. Keep the default unless you need to tune it. |

## Local Setup

1. Create an OpenRouter account.
2. Create an API key from the OpenRouter keys page.
3. Store the API key and provider settings with .NET user-secrets.

From the `src` directory:

```bash
dotnet user-secrets set "OpenAi:ApiKey" "YOUR_OPENROUTER_KEY" --project Cursus.PL
dotnet user-secrets set "OpenAi:BaseUrl" "https://openrouter.ai/api/v1" --project Cursus.PL
dotnet user-secrets set "OpenAi:Model" "openrouter/free" --project Cursus.PL
```

From the repository root:

```bash
dotnet user-secrets set "OpenAi:ApiKey" "YOUR_OPENROUTER_KEY" --project src/Cursus.PL
dotnet user-secrets set "OpenAi:BaseUrl" "https://openrouter.ai/api/v1" --project src/Cursus.PL
dotnet user-secrets set "OpenAi:Model" "openrouter/free" --project src/Cursus.PL
```

Restart the web app after changing these values. `OpenAiChatClient` is registered as a singleton, so it reads the AI provider configuration when the application starts. The tracked default configuration already points `OpenAi:BaseUrl` to OpenRouter and `OpenAi:Model` to `openrouter/free`; only the API key must stay secret.

## Choosing A Specific Model

OpenRouter model names are provider slugs. To choose a model:

1. Open the OpenRouter model catalog: <https://openrouter.ai/models>
2. Pick a model your OpenRouter account can access.
3. Copy the exact model slug shown by OpenRouter.
4. Set that slug as `OpenAi:Model`.

Examples:

```bash
# Free-model router for local smoke tests
dotnet user-secrets set "OpenAi:Model" "openrouter/free" --project Cursus.PL

# Example only: use the exact GPT-5 slug shown in OpenRouter's model catalog
dotnet user-secrets set "OpenAi:Model" "openai/gpt-5" --project Cursus.PL
```

Model availability, pricing, and rate limits are controlled by OpenRouter and can change. If a model fails, copy the current slug from the OpenRouter catalog and try again.

## Production Configuration

Use environment variables instead of user-secrets:

```bash
OpenAi__ApiKey=YOUR_OPENROUTER_KEY
OpenAi__BaseUrl=https://openrouter.ai/api/v1
OpenAi__Model=openrouter/free
```

## Testing AI Advisor

1. Start the app.
2. Sign in with a seeded demo student:

```text
Email: junior.ai@cursus.demo
Password: Demo123!
```

3. Open `/Student/AiAdvisor`.
4. Send a short prompt, for example:

```text
Am I on track to graduate?
```

## Troubleshooting

If the browser shows `503 Service Unavailable`, check the application terminal.

| Error | Likely cause |
|-------|--------------|
| `openai_not_configured` | `OpenAi:ApiKey` is missing or the app was not restarted after setting secrets. |
| `401` or `403` from provider | API key is invalid, disabled, or lacks access to the selected model. |
| `404` from provider | The configured model slug does not exist. Copy the exact slug from OpenRouter. |
| `429` from provider | Rate limit, no credits, or free-model quota exhausted. |
| Empty or truncated answer | Increase `OpenAi:MaxOutputTokenCount` or choose a more capable model. |

Security note: `dotnet user-secrets list` prints secret values. Do not share screenshots or logs that include the output.
