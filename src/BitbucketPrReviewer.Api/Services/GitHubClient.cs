using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BitbucketPrReviewer.Api.Models;
using BitbucketPrReviewer.Api.Settings;
using Microsoft.Extensions.Options;

namespace BitbucketPrReviewer.Api.Services;

public sealed class GitHubClient
{
    private readonly HttpClient _httpClient;
    private readonly GitHubSettings _settings;

    public GitHubClient(HttpClient httpClient, IOptions<GitHubSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;

        if (!string.IsNullOrWhiteSpace(_settings.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
        }
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PR-Reviewer");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    public async Task<GitHubPullRequest?> GetPullRequestAsync(string owner, string repo, int prNumber, CancellationToken ct)
    {
        var url = $"repos/{owner}/{repo}/pulls/{prNumber}";
        using var res = await _httpClient.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        using var stream = await res.Content.ReadAsStreamAsync(ct);
        var pr = await JsonSerializer.DeserializeAsync<GitHubPullRequest>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
        return pr;
    }

    public async Task<List<string>> GetChangedFilesAsync(string owner, string repo, int prNumber, CancellationToken ct)
    {
        var paths = new List<string>();
        var url = $"repos/{owner}/{repo}/pulls/{prNumber}/files";
        using var res = await _httpClient.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        using var stream = await res.Content.ReadAsStreamAsync(ct);
        var files = await JsonSerializer.DeserializeAsync<List<GitHubFileChange>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
        if (files != null)
        {
            foreach (var file in files)
            {
                if (!string.IsNullOrWhiteSpace(file.Filename))
                {
                    paths.Add(file.Filename);
                }
            }
        }
        return paths.Distinct().ToList();
    }

    public async Task<string> GetFileContentAsync(string owner, string repo, string commit, string path, CancellationToken ct)
    {
        var url = $"repos/{owner}/{repo}/contents/{Uri.EscapeDataString(path)}?ref={commit}";
        using var res = await _httpClient.GetAsync(url, ct);
        if (!res.IsSuccessStatusCode)
        {
            return string.Empty;
        }
        using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("content", out var contentProp))
        {
            var base64Content = contentProp.GetString();
            if (!string.IsNullOrWhiteSpace(base64Content))
            {
                // GitHub returns base64 encoded content
                var bytes = Convert.FromBase64String(base64Content.Replace("\n", ""));
                return Encoding.UTF8.GetString(bytes);
            }
        }
        return string.Empty;
    }

    public async Task<string> GetUnifiedDiffAsync(string owner, string repo, int prNumber, string? path, CancellationToken ct)
    {
        // Get the PR to find base and head commits
        var pr = await GetPullRequestAsync(owner, repo, prNumber, ct);
        if (pr?.Base?.Sha == null || pr?.Head?.Sha == null)
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(path))
        {
            // Get full PR diff
            var url = $"repos/{owner}/{repo}/pulls/{prNumber}";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.diff"));
            using var res = await _httpClient.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync(ct);
        }
        else
        {
            // Get diff for specific file using compare API
            var url = $"repos/{owner}/{repo}/compare/{pr.Base.Sha}...{pr.Head.Sha}";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.diff"));
            using var res = await _httpClient.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();
            var fullDiff = await res.Content.ReadAsStringAsync(ct);
            
            // Extract the diff for the specific file
            var lines = fullDiff.Split('\n');
            var inFile = false;
            var filtered = new StringBuilder();
            foreach (var line in lines)
            {
                if (line.StartsWith($"--- a/{path}") || line.StartsWith($"+++ b/{path}"))
                {
                    inFile = true;
                    filtered.AppendLine(line);
                }
                else if (inFile && (line.StartsWith("--- a/") || line.StartsWith("+++ b/")))
                {
                    break; // Next file started
                }
                else if (inFile)
                {
                    filtered.AppendLine(line);
                }
            }
            return filtered.ToString();
        }
    }

    public async Task PostInlineCommentAsync(string owner, string repo, int prNumber, ReviewComment comment, CancellationToken ct)
    {
        var url = $"repos/{owner}/{repo}/pulls/{prNumber}/comments";
        
        // GitHub requires commit_id, path, and line for inline comments
        // We need to get the PR to find the head commit
        var pr = await GetPullRequestAsync(owner, repo, prNumber, ct);
        if (pr?.Head?.Sha == null)
        {
            throw new InvalidOperationException("Could not get PR head commit SHA");
        }

        var payload = new
        {
            body = comment.Comment,
            commit_id = pr.Head.Sha,
            path = comment.FilePath,
            line = comment.Line ?? 1,
            side = "RIGHT" // Comments on the right side (new code)
        };

        using var res = await _httpClient.PostAsJsonAsync(url, payload, ct);
        res.EnsureSuccessStatusCode();
    }
}

