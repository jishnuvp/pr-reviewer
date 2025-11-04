namespace BitbucketPrReviewer.Api.Settings;

public sealed class BitbucketSettings
{
    public string BaseUrl { get; set; } = "https://api.bitbucket.org/2.0/";
    public string Workspace { get; set; } = string.Empty;
    public string RepoSlug { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
}

public sealed class OpenAISettings
{
    public string Provider { get; set; } = "OpenAI"; // OpenAI or Azure
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.openai.com/";
    public string Model { get; set; } = "gpt-4o-mini";
    public string AzureDeployment { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-15-preview";
    public int MaxPromptChars { get; set; } = 120000;
}


