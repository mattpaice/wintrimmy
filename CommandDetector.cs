using System.Text.RegularExpressions;

namespace WinTrimmy;

public class CommandDetector
{
    private readonly AppSettings _settings;

    public CommandDetector(AppSettings settings)
    {
        _settings = settings;
    }

    public string CleanBoxDrawingCharacters(string text)
    {
        if (!_settings.RemoveBoxDrawing) return text;

        // Remove box drawing characters (│) commonly used in terminal borders
        var cleaned = Regex.Replace(text, @"[│┃┆┇┊┋]", " ");
        // Collapse multiple spaces into single space
        cleaned = Regex.Replace(cleaned, @"  +", " ");
        return cleaned.Trim();
    }

    public string? TransformIfCommand(string text)
    {
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

        // Skip if too many lines (safety measure)
        if (lines.Length > 10 && _settings.Aggressiveness != Aggressiveness.High)
        {
            return null;
        }

        if (lines.Length > 10)
        {
            return null; // Even in high mode, skip very long content
        }

        // Single line doesn't need flattening
        if (lines.Length <= 1) return null;

        var score = CalculateCommandScore(lines);

        if (score >= _settings.Aggressiveness.ScoreThreshold())
        {
            return Flatten(text);
        }

        return null;
    }

    private int CalculateCommandScore(string[] lines)
    {
        int score = 0;

        // Check for line continuations (backslash at end)
        if (lines.Any(l => l.TrimEnd().EndsWith("\\")))
        {
            score += 4;
        }

        // Check for pipes
        if (lines.Any(l => l.Contains('|')))
        {
            score += 2;
        }

        // Check for common shell prompts
        if (lines.Any(l => l.TrimStart().StartsWith("$") || l.TrimStart().StartsWith(">")))
        {
            score += 2;
        }

        // Check for command-like patterns
        var commandPatterns = new[]
        {
            @"^\s*(sudo|apt|yum|brew|npm|yarn|pip|cargo|dotnet|git|docker|kubectl)\s+",
            @"^\s*\w+\s+--\w+",  // flags like --verbose
            @"^\s*\w+\s+-\w+",   // flags like -v
            @"&&|;|\|\|",        // command chaining
            @"^\s*cd\s+",
            @"^\s*export\s+",
            @"^\s*set\s+",
            @"^\s*\.[\\/]",      // ./script or .\script
        };

        foreach (var pattern in commandPatterns)
        {
            if (lines.Any(l => Regex.IsMatch(l, pattern)))
            {
                score += 2;
            }
        }

        // Penalty for prose-like content
        if (lines.Any(l => l.TrimEnd().EndsWith(".")))
        {
            score -= 3;
        }

        // Check if lines look like command continuations
        var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (nonEmptyLines.Length >= 2)
        {
            var hasIndentedContinuation = nonEmptyLines.Skip(1).Any(l =>
                l.StartsWith("  ") || l.StartsWith("\t"));
            if (hasIndentedContinuation)
            {
                score += 2;
            }
        }

        return score;
    }

    private bool IsLikelyCommandLine(string line)
    {
        var trimmed = line.Trim();

        // Reject if ends with period (likely prose)
        if (trimmed.EndsWith(".") && !trimmed.EndsWith(".."))
        {
            return false;
        }

        // Check for common command indicators
        return Regex.IsMatch(trimmed, @"^[\w\-./\\]+\s") ||
               trimmed.Contains('|') ||
               trimmed.Contains("&&") ||
               trimmed.StartsWith("$") ||
               trimmed.StartsWith(">");
    }

    public string Flatten(string text)
    {
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var result = new List<string>();
        var currentLine = "";

        const string blankPlaceholder = "\u0000BLANK\u0000";

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            // Handle blank lines
            if (string.IsNullOrWhiteSpace(line))
            {
                if (_settings.PreserveBlankLines && !string.IsNullOrEmpty(currentLine))
                {
                    result.Add(currentLine.Trim());
                    result.Add(blankPlaceholder);
                    currentLine = "";
                }
                continue;
            }

            // Handle line continuations (backslash)
            if (line.TrimEnd().EndsWith("\\"))
            {
                var withoutBackslash = line.TrimEnd()[..^1].TrimEnd();
                currentLine += (currentLine.Length > 0 ? " " : "") + withoutBackslash;
            }
            else
            {
                currentLine += (currentLine.Length > 0 ? " " : "") + line.Trim();
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            result.Add(currentLine.Trim());
        }

        var flattened = string.Join(" ", result);

        // Replace blank placeholders back
        if (_settings.PreserveBlankLines)
        {
            flattened = flattened.Replace($" {blankPlaceholder} ", "\n\n");
            flattened = flattened.Replace(blankPlaceholder, "\n");
        }

        // Normalize multiple spaces
        flattened = Regex.Replace(flattened, @"  +", " ");

        return flattened.Trim();
    }
}
