using System.Linq;

namespace BitbucketPrReviewer.Api.Services;

public sealed class DiffParser
{
    /// <summary>
    /// Parses a unified diff and extracts line numbers of changed lines (added/modified lines in the new file).
    /// Returns a set of line numbers (1-based) that were added or modified.
    /// </summary>
    public static HashSet<int> ExtractChangedLineNumbers(string diff)
    {
        var changedLines = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(diff))
        {
            return changedLines;
        }

        var lines = diff.Split('\n');
        int currentNewLine = 0;
        bool inHunk = false;
        string? hunkHeader = null;

        foreach (var line in lines)
        {
            // Parse hunk header: @@ -old_start,old_count +new_start,new_count @@
            if (line.StartsWith("@@"))
            {
                inHunk = true;
                hunkHeader = line;
                // Extract new line start from hunk header
                // Format: @@ -old_start,old_count +new_start,new_count @@
                var parts = line.Split(' ');
                foreach (var part in parts)
                {
                    if (part.StartsWith("+") && part.Length > 1)
                    {
                        var newPart = part.Substring(1); // Remove '+'
                        var commaIndex = newPart.IndexOf(',');
                        if (commaIndex > 0)
                        {
                            if (int.TryParse(newPart.Substring(0, commaIndex), out var newStart))
                            {
                                currentNewLine = newStart;
                            }
                        }
                        else if (int.TryParse(newPart, out var newStart))
                        {
                            currentNewLine = newStart;
                        }
                    }
                }
                continue;
            }

            if (!inHunk) continue;

            // Skip file headers
            if (line.StartsWith("---") || line.StartsWith("+++"))
            {
                continue;
            }

            // Track line numbers
            if (line.StartsWith("+") && !line.StartsWith("+++"))
            {
                // Added or modified line (in new file)
                changedLines.Add(currentNewLine);
                currentNewLine++;
            }
            else if (line.StartsWith("-") && !line.StartsWith("---"))
            {
                // Removed line (in old file) - don't increment new line counter
                // Don't add to changedLines as it's removed, not changed
            }
            else if (line.StartsWith("\\"))
            {
                // No newline at end of file marker
                continue;
            }
            else
            {
                // Context line (unchanged) - increment both counters
                currentNewLine++;
            }
        }

        return changedLines;
    }

    /// <summary>
    /// Extracts code snippets for changed lines with context.
    /// Returns a dictionary mapping line numbers to code snippets with surrounding context.
    /// </summary>
    public static Dictionary<int, string> ExtractChangedLinesWithContext(string fileContent, HashSet<int> changedLineNumbers, int contextLines = 3)
    {
        var result = new Dictionary<int, string>();
        if (string.IsNullOrWhiteSpace(fileContent) || changedLineNumbers.Count == 0)
        {
            return result;
        }

        var lines = fileContent.Split('\n');
        var processedRanges = new HashSet<(int start, int end)>();

        foreach (var changedLine in changedLineNumbers.OrderBy(x => x))
        {
            // Check if this line is already covered by a previous range
            bool alreadyCovered = false;
            foreach (var (start, end) in processedRanges)
            {
                if (changedLine >= start && changedLine <= end)
                {
                    alreadyCovered = true;
                    break;
                }
            }

            if (alreadyCovered) continue;

            // Calculate range with context
            int startLine = Math.Max(1, changedLine - contextLines);
            int endLine = Math.Min(lines.Length, changedLine + contextLines);

            // Extend range to include nearby changed lines
            foreach (var otherChangedLine in changedLineNumbers)
            {
                if (otherChangedLine >= startLine && otherChangedLine <= endLine)
                {
                    // Extend range to include this changed line
                    startLine = Math.Max(1, Math.Min(startLine, otherChangedLine - contextLines));
                    endLine = Math.Min(lines.Length, Math.Max(endLine, otherChangedLine + contextLines));
                }
            }

            // Build snippet with line numbers
            var snippet = new System.Text.StringBuilder();
            snippet.AppendLine($"Lines {startLine}-{endLine}:");
            for (int i = startLine - 1; i < endLine && i < lines.Length; i++)
            {
                var lineNum = i + 1;
                var prefix = changedLineNumbers.Contains(lineNum) ? ">>> " : "    ";
                snippet.AppendLine($"{prefix}{lineNum,4}: {lines[i]}");
            }

            result[changedLine] = snippet.ToString();
            processedRanges.Add((startLine, endLine));
        }

        return result;
    }
}

