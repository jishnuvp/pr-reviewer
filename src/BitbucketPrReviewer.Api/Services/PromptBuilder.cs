using System.Linq;
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

    public (string systemPrompt, string userPrompt) Build(string prTitle, IEnumerable<(string path, string content, string diff)> files, string? additionalInformation = null)
    {
        var system = "You are a senior code reviewer. Focus ONLY on reviewing the changed lines (dirty lines) indicated in the code snippets. " +
                     "Do not review unchanged code. Produce concise, actionable review comments in strict JSON with fields: filePath, line, comment, severity (info|suggestion|warning|error). " +
                     "The 'line' field must correspond to the actual line number in the file where the change was made (marked with '>>>').";

        var sb = new StringBuilder();
        sb.AppendLine($"PR Title: {prTitle}");
        
        if (!string.IsNullOrWhiteSpace(additionalInformation))
        {
            sb.AppendLine();
            sb.AppendLine("Additional Context from Senior Developer:");
            sb.AppendLine(additionalInformation);
            sb.AppendLine();
        }
        
        sb.AppendLine("IMPORTANT: Review ONLY the changed lines (marked with '>>>'). Focus on the actual modifications, not the surrounding context.");
        sb.AppendLine();
        sb.AppendLine("Changed files and their modified lines:");

        foreach (var (path, content, diff) in files)
        {
            if (sb.Length > _settings.MaxPromptChars) break;
            
            // Parse diff to get changed line numbers
            var changedLines = DiffParser.ExtractChangedLineNumbers(diff);
            
            if (changedLines.Count == 0)
            {
                // If no changed lines detected, include the diff for context
                sb.AppendLine($"--- FILE: {path} ---");
                sb.AppendLine("# Unified Diff (no changed lines detected, showing full diff)");
                sb.AppendLine(diff);
                sb.AppendLine();
                continue;
            }

            // Extract only changed lines with context
            var changedSnippets = DiffParser.ExtractChangedLinesWithContext(content, changedLines, contextLines: 3);
            
            sb.AppendLine($"--- FILE: {path} ---");
            sb.AppendLine($"# Changed Lines (Total: {changedLines.Count} lines modified)");
            sb.AppendLine("# Unified Diff:");
            sb.AppendLine(diff);
            sb.AppendLine();
            sb.AppendLine("# Code Snippets (Changed lines marked with '>>>'):");
            
            foreach (var (lineNum, snippet) in changedSnippets.OrderBy(x => x.Key))
            {
                if (sb.Length > _settings.MaxPromptChars) break;
                sb.AppendLine(snippet);
                sb.AppendLine();
            }
            
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("Return JSON with shape: { \"summary\": string, \"comments\": [{ \"filePath\": string, \"line\": number|null, \"comment\": string, \"severity\": string }] }");
        sb.AppendLine("Remember: Only comment on lines that were actually changed (marked with '>>>'). The 'line' field must match the line number in the file.");

        return (system, sb.ToString());
    }
}


