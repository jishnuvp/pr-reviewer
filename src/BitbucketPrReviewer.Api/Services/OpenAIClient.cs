using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BitbucketPrReviewer.Api.Settings;
using Microsoft.Extensions.Options;

namespace BitbucketPrReviewer.Api.Services;

public sealed class OpenAIClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAISettings _settings;

    public OpenAIClient(IOptions<OpenAISettings> options)
    {
        _settings = options.Value;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_settings.Endpoint)
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    public async Task<string> GetReviewJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        if (string.Equals(_settings.Provider, "Azure", StringComparison.OrdinalIgnoreCase))
        {
            return await CallAzureChatCompletions(systemPrompt, userPrompt, ct);
        }
        return await CallOpenAIChatCompletions(systemPrompt, userPrompt, ct);
    }

    private async Task<string> CallOpenAIChatCompletions(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var url = "v1/chat/completions";
        var body = new
        {
            model = _settings.Model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        using var res = await _httpClient.PostAsJsonAsync(url, body, ct);
        res.EnsureSuccessStatusCode();
        using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return content ?? "{}";
    }

    private async Task<string> CallAzureChatCompletions(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var path = $"openai/deployments/{_settings.AzureDeployment}/chat/completions?api-version={_settings.ApiVersion}";

        using var client = new HttpClient { BaseAddress = new Uri(_settings.Endpoint) };
        client.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);

        var body = new
        {
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        using var res = await client.PostAsJsonAsync(path, body, ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return content ?? "{}";
    }
}


