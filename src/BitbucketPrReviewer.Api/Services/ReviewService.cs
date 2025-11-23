using System.Text.Json;
using BitbucketPrReviewer.Api.Models;
using BitbucketPrReviewer.Api.Settings;
using Microsoft.Extensions.Options;

namespace BitbucketPrReviewer.Api.Services;

public sealed class ReviewService
{
    private readonly BitbucketClient? _bb;
    private readonly GitHubClient? _gh;
    private readonly PromptBuilder _promptBuilder;
    private readonly OpenAIClient _ai;
    private readonly BitbucketSettings _bbSettings;

    public ReviewService(
        BitbucketClient? bb,
        GitHubClient? gh,
        PromptBuilder promptBuilder,
        OpenAIClient ai,
        IOptions<BitbucketSettings> bbOptions)
    {
        _bb = bb;
        _gh = gh;
        _promptBuilder = promptBuilder;
        _ai = ai;
        _bbSettings = bbOptions.Value;
    }

    public async Task<ReviewResult> ReviewPullRequest(string provider, string workspace, string repoSlug, int prId, string? additionalInformation = null, CancellationToken ct = default)
    {
        if (provider == "github" && _gh != null)
        {
            return await ReviewGitHubPullRequest(workspace, repoSlug, prId, additionalInformation, ct);
        }
        else if (provider == "bitbucket" && _bb != null)
        {
            return await ReviewBitbucketPullRequest(workspace, repoSlug, prId, additionalInformation, ct);
        }
        else
        {
            return new ReviewResult { Summary = $"Provider '{provider}' is not supported or client is not configured." };
        }
    }

    private async Task<ReviewResult> ReviewBitbucketPullRequest(string workspace, string repoSlug, int prId, string? additionalInformation, CancellationToken ct)
    {
        if (_bb == null)
        {
            return new ReviewResult { Summary = "Bitbucket client is not configured." };
        }

        var pr = await _bb.GetPullRequestAsync(workspace, repoSlug, prId, ct);
        if (pr?.Source?.Commit?.Hash is null)
        {
            return new ReviewResult { Summary = "PR has no source commit hash" };
        }

        var changed = await _bb.GetChangedFilesAsync(workspace, repoSlug, prId, ct);

        var files = new List<(string path, string content, string diff)>();
        foreach (var path in changed)
        {
            var content = await _bb.GetFileContentAsync(workspace, repoSlug, pr.Source!.Commit!.Hash!, path, ct);
            var diff = await _bb.GetUnifiedDiffAsync(workspace, repoSlug, prId, path, ct);
            files.Add((path, content, diff));
        }

        var (system, user) = _promptBuilder.Build(pr.Title ?? $"PR #{prId}", files, additionalInformation);

        var reviewJson = await _ai.GetReviewJsonAsync(system, user, ct);

        var result = ParseReview(reviewJson);
        result.PostedComments = 0;

        foreach (var c in result.Comments)
        {
            try
            {
                await _bb.PostInlineCommentAsync(workspace, repoSlug, prId, c, ct);
                result.PostedComments++;
            }
            catch
            {
                // skip posting errors to not fail whole review
            }
        }

        return result;
    }

    private async Task<ReviewResult> ReviewGitHubPullRequest(string owner, string repo, int prNumber, string? additionalInformation, CancellationToken ct)
    {
        if (_gh == null)
        {
            return new ReviewResult { Summary = "GitHub client is not configured." };
        }

        var pr = await _gh.GetPullRequestAsync(owner, repo, prNumber, ct);
        if (pr?.Head?.Sha is null)
        {
            return new ReviewResult { Summary = "PR has no head commit SHA" };
        }

        var changed = await _gh.GetChangedFilesAsync(owner, repo, prNumber, ct);

        var files = new List<(string path, string content, string diff)>();
        foreach (var path in changed)
        {
            var content = await _gh.GetFileContentAsync(owner, repo, pr.Head.Sha, path, ct);
            var diff = await _gh.GetUnifiedDiffAsync(owner, repo, prNumber, path, ct);
            files.Add((path, content, diff));
        }

        var (system, user) = _promptBuilder.Build(pr.Title ?? $"PR #{prNumber}", files, additionalInformation);

        var reviewJson = await _ai.GetReviewJsonAsync(system, user, ct);

        var result = ParseReview(reviewJson);
        result.PostedComments = 0;

        foreach (var c in result.Comments)
        {
            try
            {
                await _gh.PostInlineCommentAsync(owner, repo, prNumber, c, ct);
                result.PostedComments++;
            }
            catch
            {
                // skip posting errors to not fail whole review
            }
        }

        return result;
    }

    // Legacy method for backward compatibility with webhook-based approach
    public async Task<ReviewResult> ReviewPullRequest(BitbucketPullRequestEvent prEvent, CancellationToken ct = default)
    {
        var (workspace, repoSlug) = ResolveRepo(prEvent.Repository?.FullName);
        var prId = prEvent.PullRequest!.Id;
        return await ReviewPullRequest("bitbucket", workspace, repoSlug, prId, prEvent.AdditionalInformation, ct);
    }

    private static (string workspace, string repoSlug) ResolveRepo(string? fullName)
    {
        if (!string.IsNullOrEmpty(fullName) && fullName.Contains('/'))
        {
            var parts = fullName.Split('/', 2);
            return (parts[0], parts[1]);
        }
        return (string.Empty, string.Empty);
    }

    private static ReviewResult ParseReview(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var result = new ReviewResult
            {
                Summary = root.TryGetProperty("summary", out var s) ? s.GetString() ?? string.Empty : string.Empty,
                Comments = new List<ReviewComment>()
            };
            if (root.TryGetProperty("comments", out var comments) && comments.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in comments.EnumerateArray())
                {
                    var item = new ReviewComment
                    {
                        FilePath = c.TryGetProperty("filePath", out var fp) ? fp.GetString() ?? string.Empty : string.Empty,
                        Line = c.TryGetProperty("line", out var ln) && ln.ValueKind is JsonValueKind.Number ? ln.GetInt32() : null,
                        Comment = c.TryGetProperty("comment", out var cm) ? cm.GetString() ?? string.Empty : string.Empty,
                        Severity = c.TryGetProperty("severity", out var sv) ? sv.GetString() ?? "info" : "info"
                    };
                    if (!string.IsNullOrWhiteSpace(item.FilePath) && !string.IsNullOrWhiteSpace(item.Comment))
                    {
                        result.Comments.Add(item);
                    }
                }
            }
            return result;
        }
        catch
        {
            return new ReviewResult { Summary = "AI response was not valid JSON", Comments = new List<ReviewComment>() };
        }
    }
}


