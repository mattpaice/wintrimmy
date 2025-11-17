using System.Linq;
using System.Text.RegularExpressions;

namespace WinTrimmy;

public class CommandDetector
{
    private static readonly Regex BoxDrawingCharacters = new(@"[│┃┆┇┊┋]", RegexOptions.Compiled);
    private static readonly Regex DoubleSpaces = new(@" {2,}", RegexOptions.Compiled);
    private static readonly Regex BoxCleanupPattern = new(@"(?<!\n)([A-Z0-9_.-])\s*\n\s*([A-Z0-9_.-])(?!\n)", RegexOptions.Compiled);
    private static readonly Regex CommandPreamble = new(@"(?m)^\s*(sudo\s+)?[A-Za-z0-9./~_-]+", RegexOptions.Compiled);
    private static readonly Regex PipePattern = new(@"\|", RegexOptions.Compiled);
    private static readonly Regex ChainOperatorsPattern = new(@"&&|;|\|\||&", RegexOptions.Compiled);
    private static readonly Regex PromptPattern = new(@"(^|\n)\s*\$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex WordCharacterAfterSpace = new(@"[-/]", RegexOptions.Compiled);
    private static readonly Regex WhitespaceCollapse = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex LineCollapse = new(@"\n+", RegexOptions.Compiled);
    private static readonly Regex BackslashContinuation = new(@"\\\s*\n", RegexOptions.Compiled);
    private static readonly Regex CommonCommandsPattern = new(@"(?mi)^\s*(sudo|apt|yum|brew|npm|yarn|pip|cargo|dotnet|git|docker|kubectl|helm|az|gcloud|terraform)\b", RegexOptions.Compiled);
    private static readonly Regex LongFlagPattern = new(@"--[A-Za-z0-9-]+", RegexOptions.Compiled);
    private static readonly Regex ShortFlagPattern = new(@"(?<!-)-[A-Za-z0-9]", RegexOptions.Compiled);
    private static readonly Regex DirectoryCommandPattern = new(@"(?mi)^\s*(cd|export|set|source)\b", RegexOptions.Compiled);
    private static readonly Regex ScriptInvocationPattern = new(@"(?mi)^\s*\.[\\/]", RegexOptions.Compiled);

    private readonly ITrimSettings _settings;

    public CommandDetector(ITrimSettings settings)
    {
        _settings = settings;
    }

    public string? CleanBoxDrawingCharacters(string text)
    {
        if (!_settings.RemoveBoxDrawing) return null;
        if (!BoxDrawingCharacters.IsMatch(text)) return null;

        var cleaned = BoxDrawingCharacters.Replace(text, " ");
        cleaned = DoubleSpaces.Replace(cleaned, " ");
        cleaned = cleaned.Trim();

        return cleaned == text ? null : cleaned;
    }

    public string? TransformIfCommand(string text)
    {
        if (!text.Contains('\n')) return null;

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToArray();
        if (lines.Length < 2 || lines.Length > 10) return null;

        var score = CalculateCommandScore(text, lines);
        if (score < _settings.Aggressiveness.ScoreThreshold()) return null;

        var flattened = Flatten(text);
        return flattened == text ? null : flattened;
    }

    private int CalculateCommandScore(string text, string[] lines)
    {
        var score = 0;
        if (text.Contains("\\\n")) score += 1;
        if (PipePattern.IsMatch(text)) score += 1;
        if (ChainOperatorsPattern.IsMatch(text)) score += 1;
        if (PromptPattern.IsMatch(text)) score += 1;
        if (lines.All(IsLikelyCommandLine)) score += 1;
        if (CommandPreamble.IsMatch(text)) score += 1;
        if (WordCharacterAfterSpace.IsMatch(text)) score += 1;
        if (CommonCommandsPattern.IsMatch(text)) score += 1;
        if (LongFlagPattern.IsMatch(text) || ShortFlagPattern.IsMatch(text)) score += 1;
        if (DirectoryCommandPattern.IsMatch(text)) score += 1;
        if (ScriptInvocationPattern.IsMatch(text)) score += 1;

        if (lines.Any(IsProseLine)) score -= 1;
        return score;
    }

    private bool IsLikelyCommandLine(string line)
    {
        var trimmed = line.Trim();

        if (trimmed.Length == 0) return false;
        if (trimmed.EndsWith(".") && !trimmed.EndsWith("..")) return false;

        var pattern = @"^(sudo\s+)?[A-Za-z0-9./~_-]+(?:\s+|\z)";
        return Regex.IsMatch(trimmed, pattern);
    }

    private static bool IsProseLine(string line)
    {
        var trimmed = line.TrimEnd();
        return trimmed.EndsWith(".") && !trimmed.EndsWith("..");
    }

    public string Flatten(string text)
    {
        const string BlankPlaceholder = "__WINTRIMMY_BLANK__";

        var result = text;
        if (_settings.PreserveBlankLines)
        {
            result = Regex.Replace(result, @"\n\s*\n", BlankPlaceholder, RegexOptions.Multiline);
        }

        result = BoxCleanupPattern.Replace(result, "$1$2");
        result = BackslashContinuation.Replace(result, " ");
        result = LineCollapse.Replace(result, " ");
        result = WhitespaceCollapse.Replace(result, " ");

        if (_settings.PreserveBlankLines)
        {
            result = result.Replace(BlankPlaceholder, "\n\n");
        }

        return result.Trim();
    }
}
