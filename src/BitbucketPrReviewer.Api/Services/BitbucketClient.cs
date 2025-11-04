using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BitbucketPrReviewer.Api.Models;
using BitbucketPrReviewer.Api.Settings;
using Microsoft.Extensions.Options;

namespace BitbucketPrReviewer.Api.Services;

public sealed class BitbucketClient
{
    private readonly HttpClient _httpClient;
    private readonly BitbucketSettings _settings;

    public BitbucketClient(HttpClient httpClient, IOptions<BitbucketSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;

        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.Username}:{_settings.AppPassword}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
    }

    public async Task<BitbucketPullRequest?> GetPullRequestAsync(string workspace, string repoSlug, int prId, CancellationToken ct)
    {
        var url = $"repositories/{workspace}/{repoSlug}/pullrequests/{prId}";
        using var res = await _httpClient.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        using var stream = await res.Content.ReadAsStreamAsync(ct);
        var pr = await JsonSerializer.DeserializeAsync<BitbucketPullRequest>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
        return pr;
    }

    public async Task<List<string>> GetChangedFilesAsync(string workspace, string repoSlug, int prId, CancellationToken ct)
    {
        var paths = new List<string>();
        var url = $"repositories/{workspace}/{repoSlug}/pullrequests/{prId}/diffstat";
        using var res = await _httpClient.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("values", out var values))
        {
            foreach (var v in values.EnumerateArray())
            {
                var np = v.GetProperty("new").GetProperty("path").GetString();
                var op = v.GetProperty("old").TryGetProperty("path", out var oldPathProp) ? oldPathProp.GetString() : null;
                var path = np ?? op;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    paths.Add(path!);
                }
            }
        }
        return paths.Distinct().ToList();
    }

    public async Task<string> GetFileContentAsync(string workspace, string repoSlug, string commit, string path, CancellationToken ct)
    {
        var url = $"repositories/{workspace}/{repoSlug}/src/{commit}/{Uri.EscapeDataString(path)}";
        using var res = await _httpClient.GetAsync(url, ct);
        if (!res.IsSuccessStatusCode)
        {
            return string.Empty;
        }
        return await res.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> GetUnifiedDiffAsync(string workspace, string repoSlug, int prId, string? path, CancellationToken ct)
    {
        var url = $"repositories/{workspace}/{repoSlug}/pullrequests/{prId}/diff" + (string.IsNullOrEmpty(path) ? string.Empty : $"/{Uri.EscapeDataString(path)}");
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        using var res = await _httpClient.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }

    public async Task PostInlineCommentAsync(string workspace, string repoSlug, int prId, ReviewComment comment, CancellationToken ct)
    {
        var url = $"repositories/{workspace}/{repoSlug}/pullrequests/{prId}/comments";

        var payload = new
        {
            content = new { raw = comment.Comment },
            inline = new
            {
                path = comment.FilePath,
                to = comment.Line,
                line_type = "added"
            }
        };

        using var res = await _httpClient.PostAsJsonAsync(url, payload, ct);
        res.EnsureSuccessStatusCode();
    }
}


