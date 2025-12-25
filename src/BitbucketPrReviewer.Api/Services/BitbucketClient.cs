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
    }

    private AuthenticationHeaderValue GetAuthHeader()
    {
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.Username}:{_settings.AppPassword}"));
        return new AuthenticationHeaderValue("Basic", basic);
    }

    public async Task<BitbucketPullRequest?> GetPullRequestAsync(string workspace, string repoSlug, int prId, CancellationToken ct)
    {
        var url = $"repositories/{workspace}/{repoSlug}/pullrequests/{prId}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = GetAuthHeader();
        using var res = await _httpClient.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        using var stream = await res.Content.ReadAsStreamAsync(ct);
        var pr = await JsonSerializer.DeserializeAsync<BitbucketPullRequest>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
        return pr;
    }

    public async Task<List<string>> GetChangedFilesAsync(string workspace, string repoSlug, int prId, CancellationToken ct)
    {
        var paths = new List<string>();
        var url = $"repositories/{workspace}/{repoSlug}/pullrequests/{prId}/diffstat";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = GetAuthHeader();
        req.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.UserAgent.ParseAdd(
                "ConsoleApp1-BitbucketClient/1.0");

        using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        if (res.StatusCode == System.Net.HttpStatusCode.Found)
        {
          // handle the redirect flow here   
          var redirectUrl = res.Headers.Location;
          using var redirectReq = new HttpRequestMessage(HttpMethod.Get, redirectUrl);
          redirectReq.Headers.Authorization = GetAuthHeader();
          using var redirectRes = await _httpClient.SendAsync(redirectReq, HttpCompletionOption.ResponseHeadersRead, ct);
          redirectRes.EnsureSuccessStatusCode();
          using var redirectStream = await redirectRes.Content.ReadAsStreamAsync(ct);
          using var redirectDoc = await JsonDocument.ParseAsync(redirectStream, cancellationToken: ct);
          if (redirectDoc.RootElement.TryGetProperty("values", out var redirectValues))
          {
            foreach (var v in redirectValues.EnumerateArray())
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
        return new List<string>();
    }

    public async Task<string> GetFileContentAsync(string workspace, string repoSlug, string commit, string path, CancellationToken ct)
    {
        var url = $"repositories/{workspace}/{repoSlug}/src/{commit}/{Uri.EscapeDataString(path)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = GetAuthHeader();
        using var res = await _httpClient.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            return string.Empty;
        }
        return await res.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> GetUnifiedDiffAsync(string workspace, string repoSlug, int prId, string? path, CancellationToken ct)
    {
        var url = $"repositories/{workspace}/{repoSlug}/pullrequests/{prId}/diff" + (string.IsNullOrEmpty(path) ? string.Empty : 
        $"?path={Uri.EscapeDataString(path)}");
        
        
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        
        req.Headers.Authorization = GetAuthHeader();
        /*req.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.UserAgent.ParseAdd(
                "ConsoleApp1-BitbucketClient/1.0");*/

        using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        if (res.StatusCode == System.Net.HttpStatusCode.Found)
        {
          // handle the redirect flow here   
          var redirectUrl = res.Headers.Location;
          using var redirectReq = new HttpRequestMessage(HttpMethod.Get, redirectUrl);
          redirectReq.Headers.Authorization = GetAuthHeader();
          using var redirectRes = await _httpClient.SendAsync(redirectReq, HttpCompletionOption.ResponseHeadersRead, ct);
          redirectRes.EnsureSuccessStatusCode();
          return await redirectRes.Content.ReadAsStringAsync(ct);
        }
        
        res.EnsureSuccessStatusCode();
        return string.Empty;
        /*req.Headers.Authorization = GetAuthHeader();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        using var res = await _httpClient.SendAsync(req, ct);




        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);*/
    }

    public async Task PostInlineCommentAsync(string workspace, string repoSlug, int prId, ReviewComment comment, CancellationToken ct)
    {
        var url = $"repositories/{workspace}/{repoSlug}/pullrequests/{prId}/comments";

        string jsonPayload = JsonSerializer.Serialize(new
        {
            content = new { raw = comment.Comment },
            inline = new
            {
                path = comment.FilePath,
                to = comment.Line
            }
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = GetAuthHeader();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        req.Headers.UserAgent.ParseAdd(
                "ConsoleApp1-BitbucketClient/1.0");
        req.Content = new StringContent(
            jsonPayload,
            Encoding.UTF8,
            "application/json"
        );
        //req.Content = JsonContent.Create(payload);
        
        using var res = await _httpClient.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }
}


