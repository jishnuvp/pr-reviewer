## Bitbucket PR Reviewer (Web API, .NET 8)

This service exposes a webhook endpoint that can be configured in Bitbucket to automatically review Pull Requests. When triggered, it fetches changed files, builds a code+diff prompt, sends it to OpenAI or Azure OpenAI, and posts structured inline comments back on the PR.

### Stack
- .NET 8 Web API
- Bitbucket Cloud REST API v2
- OpenAI / Azure OpenAI Chat Completions

### Project Layout
- `src/BitbucketPrReviewer.Api` – Web API, controllers, services, settings

### Build & Run
1. Install .NET 8 SDK.
2. Set configuration via environment variables or `appsettings.json` (see below).
3. Run:
```bash
dotnet build
dotnet run --project src/BitbucketPrReviewer.Api
```

The API listens on the default Kestrel ports (e.g., `http://localhost:5242`).

### Webhook Endpoint
- URL: `POST /api/webhook/bitbucket`
- Expects Bitbucket PR event payload (`pullrequest:created`, `pullrequest:updated`, etc.).
- Header `X-Event-Key` is optional; payload must include `pullrequest` and `repository.full_name`.

### Required Configuration
You can set via `appsettings.json` or environment variables (`:` becomes `__`).

Bitbucket:
- `Bitbucket:BaseUrl` (default `https://api.bitbucket.org/2.0/`)
- `Bitbucket:Workspace` (optional; inferred from payload `repository.full_name` when present)
- `Bitbucket:RepoSlug` (optional; inferred from payload)
- `Bitbucket:Username` (Bitbucket username)
- `Bitbucket:AppPassword` (Bitbucket app password with repo read/write)

OpenAI / Azure OpenAI:
- `OpenAI:Provider` – `OpenAI` or `Azure`
- `OpenAI:ApiKey`
- `OpenAI:Endpoint` – `https://api.openai.com/` for OpenAI; your endpoint for Azure
- `OpenAI:Model` – e.g., `gpt-4o-mini` (OpenAI only)
- `OpenAI:AzureDeployment` – your deployment name (Azure only)
- `OpenAI:ApiVersion` – e.g., `2024-02-15-preview` (Azure only)
- `OpenAI:MaxPromptChars` – upper bound for accumulated prompt size

Example environment variables (PowerShell):
```powershell
$env:Bitbucket__Username="your-user"
$env:Bitbucket__AppPassword="your-app-password"
$env:OpenAI__Provider="OpenAI"
$env:OpenAI__ApiKey="sk-..."
$env:OpenAI__Endpoint="https://api.openai.com/"
$env:OpenAI__Model="gpt-4o-mini"
```

### Bitbucket Setup
1. Create an App Password in Bitbucket with permissions: Repositories (Read), Pull requests (Read/Write), and Code (Read).
2. In your repo settings: Webhooks → Add webhook
   - Title: `PR Reviewer`
   - URL: `https://your-host/api/webhook/bitbucket`
   - Triggers: Pull request created/updated
3. Save.

### How It Works
1. Webhook receives PR event.
2. Service fetches PR details, changed files, each file’s content, and file-level unified diff.
3. Builds a compact prompt including PR title, diffs, and file contents.
4. Calls OpenAI/Azure OpenAI with `response_format=json_object` to force JSON.
5. Parses `{ summary, comments[] }`.
6. Posts inline comments via Bitbucket API.

### Notes
- Bitbucket inline comments require a file path and optionally a `to` line (added lines). If `line` is missing or invalid, the comment post may fail; the service skips failed comments and continues.
- For large PRs, the prompt truncates after `OpenAI:MaxPromptChars`.

### Security
- Use environment variables or secret managers for credentials.
- Restrict ingress to known Bitbucket IPs if exposing publicly.


