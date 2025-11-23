using System.Text.RegularExpressions;
using BitbucketPrReviewer.Api.Models;

namespace BitbucketPrReviewer.Api.Services;

public sealed class PrUrlParser
{
    public static ParsedPrUrl? ParseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // Bitbucket URL patterns:
        // https://bitbucket.org/{workspace}/{repo}/pull-requests/{prId}
        // https://bitbucket.org/{workspace}/{repo}/pull-requests/{prId}/...
        var bitbucketPattern = @"bitbucket\.org/([^/]+)/([^/]+)/pull-requests/(\d+)";
        var bitbucketMatch = Regex.Match(url, bitbucketPattern, RegexOptions.IgnoreCase);
        if (bitbucketMatch.Success)
        {
            if (int.TryParse(bitbucketMatch.Groups[3].Value, out var prId))
            {
                return new ParsedPrUrl
                {
                    Provider = "bitbucket",
                    Workspace = bitbucketMatch.Groups[1].Value,
                    RepoSlug = bitbucketMatch.Groups[2].Value,
                    PrId = prId
                };
            }
        }

        // GitHub URL patterns:
        // https://github.com/{owner}/{repo}/pull/{prNumber}
        // https://github.com/{owner}/{repo}/pull/{prNumber}/...
        var githubPattern = @"github\.com/([^/]+)/([^/]+)/pull/(\d+)";
        var githubMatch = Regex.Match(url, githubPattern, RegexOptions.IgnoreCase);
        if (githubMatch.Success)
        {
            if (int.TryParse(githubMatch.Groups[3].Value, out var prId))
            {
                return new ParsedPrUrl
                {
                    Provider = "github",
                    Workspace = githubMatch.Groups[1].Value,
                    RepoSlug = githubMatch.Groups[2].Value,
                    PrId = prId
                };
            }
        }

        return null;
    }
}

