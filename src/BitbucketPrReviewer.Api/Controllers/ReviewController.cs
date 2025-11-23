using BitbucketPrReviewer.Api.Models;
using BitbucketPrReviewer.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BitbucketPrReviewer.Api.Controllers;

[ApiController]
[Route("api/review")]
public class ReviewController : ControllerBase
{
    private readonly ReviewService _reviewService;

    public ReviewController(ReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    public async Task<IActionResult> ReviewPullRequest([FromBody] ReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PrUrl))
        {
            return BadRequest(new { error = "PR URL is required" });
        }

        var parsedUrl = PrUrlParser.ParseUrl(request.PrUrl);
        if (parsedUrl == null)
        {
            return BadRequest(new { error = "Invalid PR URL. Supported formats: Bitbucket (bitbucket.org/{workspace}/{repo}/pull-requests/{id}) or GitHub (github.com/{owner}/{repo}/pull/{number})" });
        }

        if (parsedUrl.Provider != "bitbucket" && parsedUrl.Provider != "github")
        {
            return BadRequest(new { error = $"Provider '{parsedUrl.Provider}' is not supported. Supported providers: bitbucket, github" });
        }

        var result = await _reviewService.ReviewPullRequest(
            parsedUrl.Provider,
            parsedUrl.Workspace,
            parsedUrl.RepoSlug,
            parsedUrl.PrId,
            request.AdditionalInformation);

        return Ok(new
        {
            status = "completed",
            summary = result.Summary,
            postedComments = result.PostedComments,
            totalComments = result.Comments.Count
        });
    }
}

