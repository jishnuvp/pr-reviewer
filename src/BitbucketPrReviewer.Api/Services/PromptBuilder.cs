using System.Text;
using BitbucketPrReviewer.Api.Models;
using BitbucketPrReviewer.Api.Settings;
using Microsoft.Extensions.Options;

namespace BitbucketPrReviewer.Api.Services;

public sealed class PromptBuilder
{
    private readonly OpenAISettings _settings;
    public PromptBuilder(IOptions<OpenAISettings> options)
    {
        _settings = options.Value;
    }

    public (string systemPrompt, string userPrompt) Build(string prTitle, IEnumerable<(string path, string content, string diff)> files)
    {
        var system = "You are a senior code reviewer. Produce concise, actionable review comments in strict JSON with fields: filePath, line, comment, severity (info|suggestion|warning|error).";

        var sb = new StringBuilder();
        sb.AppendLine($"PR Title: {prTitle}");
        sb.AppendLine("Files to review:");

        foreach (var (path, content, diff) in files)
        {
            if (sb.Length > _settings.MaxPromptChars) break;
            sb.AppendLine($"--- FILE: {path} ---");
            sb.AppendLine("# Unified Diff");
            sb.AppendLine(diff);
            sb.AppendLine("# Full Content");
            sb.AppendLine("""\n""");
            sb.AppendLine(content);
            sb.AppendLine("""\n""");
        }

        sb.AppendLine();
        sb.AppendLine("Return JSON with shape: { \"summary\": string, \"comments\": [{ \"filePath\": string, \"line\": number|null, \"comment\": string, \"severity\": string }] }");

        return (system, sb.ToString());
    }
}


