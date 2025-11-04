using System.Text.Json;
using BitbucketPrReviewer.Api.Models;
using BitbucketPrReviewer.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BitbucketPrReviewer.Api.Controllers;

[ApiController]
[Route("api/webhook/bitbucket")]
public class WebhookController : ControllerBase
{
    private readonly ReviewService _reviewService;

    public WebhookController(ReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    public async Task<IActionResult> Handle()
    {
        var eventKey = Request.Headers["X-Event-Key"].ToString();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        BitbucketPullRequestEvent? prEvent = null;
        try
        {
            prEvent = JsonSerializer.Deserialize<BitbucketPullRequestEvent>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            // fallthrough
        }

        if (prEvent?.PullRequest == null)
        {
            return BadRequest("Invalid PR event payload");
        }

        if (string.IsNullOrWhiteSpace(eventKey))
        {
            // Allow manual/unknown triggers too
        }

        var result = await _reviewService.ReviewPullRequest(prEvent);
        return Ok(new { status = "queued", result.Summary, result.PostedComments });
    }
}


