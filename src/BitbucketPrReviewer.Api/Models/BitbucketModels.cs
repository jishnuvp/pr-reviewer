using System.Text.Json.Serialization;

namespace BitbucketPrReviewer.Api.Models;

public sealed class BitbucketPullRequestEvent
{
    [JsonPropertyName("pullrequest")] public BitbucketPullRequest? PullRequest { get; set; }
    [JsonPropertyName("repository")] public BitbucketRepository? Repository { get; set; }
    [JsonPropertyName("additionalInformation")] public string? AdditionalInformation { get; set; }
}

public sealed class BitbucketRepository
{
    [JsonPropertyName("full_name")] public string? FullName { get; set; } // workspace/repo
}

public sealed class BitbucketPullRequest
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("source")] public BitbucketRef? Source { get; set; }
    [JsonPropertyName("destination")] public BitbucketRef? Destination { get; set; }
}

public sealed class BitbucketRef
{
    [JsonPropertyName("branch")] public BitbucketBranch? Branch { get; set; }
    [JsonPropertyName("commit")] public BitbucketCommit? Commit { get; set; }
}

public sealed class BitbucketBranch
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public sealed class BitbucketCommit
{
    [JsonPropertyName("hash")] public string? Hash { get; set; }
}

public sealed class DiffStatEntry
{
    [JsonPropertyName("new")] public DiffStatPath? New { get; set; }
    [JsonPropertyName("old")] public DiffStatPath? Old { get; set; }
}

public sealed class DiffStatPath
{
    [JsonPropertyName("path")] public string? Path { get; set; }
}

public sealed class ReviewComment
{
    public string FilePath { get; set; } = string.Empty;
    public int? Line { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
}

public sealed class ReviewResult
{
    public string Summary { get; set; } = string.Empty;
    public List<ReviewComment> Comments { get; set; } = new();
    public int PostedComments { get; set; }
}

public sealed class ReviewRequest
{
    public string PrUrl { get; set; } = string.Empty;
    public string? AdditionalInformation { get; set; }
}

public sealed class ParsedPrUrl
{
    public string Provider { get; set; } = string.Empty; // "bitbucket" or "github"
    public string Workspace { get; set; } = string.Empty; // workspace/owner
    public string RepoSlug { get; set; } = string.Empty; // repo name
    public int PrId { get; set; }
}

// GitHub Models
public sealed class GitHubPullRequest
{
    [JsonPropertyName("number")] public int Number { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("head")] public GitHubRef? Head { get; set; }
    [JsonPropertyName("base")] public GitHubRef? Base { get; set; }
}

public sealed class GitHubRef
{
    [JsonPropertyName("ref")] public string? Ref { get; set; }
    [JsonPropertyName("sha")] public string? Sha { get; set; }
}

public sealed class GitHubFileChange
{
    [JsonPropertyName("filename")] public string? Filename { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
}

public sealed class GitHubPullRequestDiff
{
    [JsonPropertyName("files")] public List<GitHubFileChange>? Files { get; set; }
}


