using System.Text.Json.Serialization;

namespace BitbucketPrReviewer.Api.Models;

public sealed class BitbucketPullRequestEvent
{
    [JsonPropertyName("pullrequest")] public BitbucketPullRequest? PullRequest { get; set; }
    [JsonPropertyName("repository")] public BitbucketRepository? Repository { get; set; }
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


