using System.Net.Http.Json;
using PrReviewer.Client.Models;

namespace PrReviewer.Client.Services;

public class PrReviewService : IPrReviewService
{
    private readonly HttpClient _httpClient;

    public PrReviewService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ReviewResponse?> SubmitReviewRequestAsync(ReviewRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/review", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ReviewResponse>();
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>();
                throw new HttpRequestException(error?.Error ?? $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
            }
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Failed to submit review request: {ex.Message}", ex);
        }
    }
}

