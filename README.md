## PR Reviewer (Web API, .NET 8)

This service provides an API endpoint to automatically review Pull Requests from Bitbucket or GitHub. When triggered, it fetches changed files, builds a code+diff prompt, sends it to OpenAI or Azure OpenAI, and posts structured inline comments back on the PR.

### Stack
- .NET 8 Web API
- Blazor WebAssembly Client
- Bitbucket Cloud REST API v2
- GitHub REST API v3
- OpenAI / Azure OpenAI Chat Completions 

### Project Layout
- `src/BitbucketPrReviewer.Api` – Web API, controllers, services, settings
- `src/PrReviewer.Client` – Blazor WebAssembly client application

### Build & Run

#### API Server
1. Install .NET 8 SDK.
2. Set configuration via environment variables or `appsettings.json` (see below).
3. Run:
```bash
dotnet build
dotnet run --project src/BitbucketPrReviewer.Api
```

The API listens on the default Kestrel ports (e.g., `http://localhost:5242`).

#### Blazor WebAssembly Client
1. Ensure the API server is running (see above).
2. Update `src/PrReviewer.Client/appsettings.json` with the correct API base URL if needed (default: `http://localhost:5242`).
3. Run:
```bash
dotnet run --project src/PrReviewer.Client
```

The client application will be available at `http://localhost:5243` (or the port specified in launch settings).

#### Build All Projects
```bash
dotnet build PrReviewer.sln
```

### API Endpoints

#### Review Pull Request
- URL: `POST /api/review`
- Body: `{ "prUrl": "https://github.com/owner/repo/pull/123", "additionalInformation": "optional context" }`
- Supported URL formats:
  - Bitbucket: `https://bitbucket.org/{workspace}/{repo}/pull-requests/{id}`
  - GitHub: `https://github.com/{owner}/{repo}/pull/{number}`

#### Webhook Endpoint (Legacy - Bitbucket only)
- URL: `POST /api/webhook/bitbucket`
- Expects Bitbucket PR event payload (`pullrequest:created`, `pullrequest:updated`, etc.).
- Header `X-Event-Key` is optional; payload must include `pullrequest` and `repository.full_name`.

### Required Configuration
You can set via `appsettings.json` or environment variables (`:` becomes `__`).

**Bitbucket:**
- `Bitbucket:BaseUrl` (default `https://api.bitbucket.org/2.0/`)
- `Bitbucket:Workspace` (optional; inferred from PR URL when present)
- `Bitbucket:RepoSlug` (optional; inferred from PR URL)
- `Bitbucket:Username` (Bitbucket username)
- `Bitbucket:AppPassword` (Bitbucket app password with repo read/write)

**GitHub:**
- `GitHub:BaseUrl` (default `https://api.github.com/`)
- `GitHub:Owner` (optional; inferred from PR URL when present)
- `GitHub:Repo` (optional; inferred from PR URL)
- `GitHub:Token` (GitHub personal access token with `repo` scope for private repos, or `public_repo` for public repos)

**OpenAI / Azure OpenAI:**
- `OpenAI:Provider` – `OpenAI` or `Azure`
- `OpenAI:ApiKey`
- `OpenAI:Endpoint` – `https://api.openai.com/` for OpenAI; your endpoint for Azure
- `OpenAI:Model` – e.g., `gpt-4o-mini` (OpenAI only)
- `OpenAI:AzureDeployment` – your deployment name (Azure only)
- `OpenAI:ApiVersion` – e.g., `2024-02-15-preview` (Azure only)
- `OpenAI:MaxPromptChars` – upper bound for accumulated prompt size

Example environment variables (PowerShell):
```powershell
# Bitbucket
$env:Bitbucket__Username="your-user"
$env:Bitbucket__AppPassword="your-app-password"

# GitHub
$env:GitHub__Token="ghp_..."

# OpenAI
$env:OpenAI__Provider="OpenAI"
$env:OpenAI__ApiKey="sk-..."
$env:OpenAI__Endpoint="https://api.openai.com/"
$env:OpenAI__Model="gpt-4o-mini"
```

### Provider Setup

#### Bitbucket Setup
1. Create an App Password in Bitbucket with permissions: Repositories (Read), Pull requests (Read/Write), and Code (Read).
2. Configure the `Bitbucket:Username` and `Bitbucket:AppPassword` in your settings.
3. (Optional) For webhook-based automation: In your repo settings → Webhooks → Add webhook
   - Title: `PR Reviewer`
   - URL: `https://your-host/api/webhook/bitbucket`
   - Triggers: Pull request created/updated

#### GitHub Setup
1. Create a Personal Access Token (PAT) in GitHub:
   - Go to Settings → Developer settings → Personal access tokens → Tokens (classic)
   - Generate a new token with `repo` scope (for private repos) or `public_repo` scope (for public repos)
   - Required permissions: `repo` (includes read/write access to code, pull requests, and comments)
2. Configure the `GitHub:Token` in your settings.
3. Use the API endpoint with GitHub PR URLs: `POST /api/review` with `{ "prUrl": "https://github.com/owner/repo/pull/123" }`

### How It Works
1. API receives PR URL (via `/api/review` endpoint) or webhook event (Bitbucket only via `/api/webhook/bitbucket`).
2. Service parses the URL to detect provider (Bitbucket or GitHub).
3. Service fetches PR details, changed files, each file's content, and file-level unified diff using the appropriate provider API.
4. Builds a compact prompt including PR title, diffs, and file contents.
5. Calls OpenAI/Azure OpenAI with `response_format=json_object` to force JSON.
6. Parses `{ summary, comments[] }`.
7. Posts inline comments via the provider's API (Bitbucket or GitHub).

### Notes
- **Bitbucket**: Inline comments require a file path and optionally a `to` line (added lines). If `line` is missing or invalid, the comment post may fail; the service skips failed comments and continues.
- **GitHub**: Inline comments require a file path, commit SHA, and line number. Comments are posted on the right side (new code) of the diff.
- For large PRs, the prompt truncates after `OpenAI:MaxPromptChars`.
- The service automatically detects the provider from the PR URL format, so you can use the same endpoint for both Bitbucket and GitHub PRs.

### Security
- Use environment variables or secret managers for credentials.
- Restrict ingress to known IPs if exposing publicly.
- For GitHub, use fine-grained personal access tokens with minimal required permissions when possible.
- Never commit credentials to version control.


