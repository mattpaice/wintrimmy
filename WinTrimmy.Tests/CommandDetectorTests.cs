using System.Linq;
using WinTrimmy;
using Xunit;

namespace WinTrimmy.Tests;

public class CommandDetectorTests
{
    [Fact]
    public void DetectsMultiLineCommand()
    {
        var settings = new TestTrimSettings();
        var detector = new CommandDetector(settings);
        var text = "echo hi\\\n  && ls -la\n";

        var result = detector.TransformIfCommand(text);

        Assert.Equal("echo hi && ls -la", result);
    }

    [Fact]
    public void SkipsSingleLine()
    {
        var detector = new CommandDetector(new TestTrimSettings());

        Assert.Null(detector.TransformIfCommand("ls -la"));
    }

    [Fact]
    public void SkipsLongCopies()
    {
        var detector = new CommandDetector(new TestTrimSettings());
        var blob = string.Join("\n", Enumerable.Repeat("echo hi", 11));

        Assert.Null(detector.TransformIfCommand(blob));
    }

    [Fact]
    public void PreservesBlankLinesWhenEnabled()
    {
        var settings = new TestTrimSettings { PreserveBlankLines = true };
        var detector = new CommandDetector(settings);
        var text = "echo hi\n\necho bye\n";

        var result = detector.TransformIfCommand(text);

        Assert.Equal("echo hi\n\necho bye", result);
    }

    [Fact]
    public void FlattensBackslashContinuations()
    {
        var detector = new CommandDetector(new TestTrimSettings());
        var text = "python script.py \\\n  --flag yes \\\n  --count 2";

        var result = detector.TransformIfCommand(text);

        Assert.Equal("python script.py --flag yes --count 2", result);
    }

    [Fact]
    public void RepairsBrokenUppercaseTokens()
    {
        var detector = new CommandDetector(new TestTrimSettings { Aggressiveness = Aggressiveness.High });
        var text = "N\nODE_PATH=/usr/bin\nls";

        var result = detector.TransformIfCommand(text);

        Assert.Equal("NODE_PATH=/usr/bin ls", result);
    }

    [Theory]
    [InlineData(Aggressiveness.Low)]
    [InlineData(Aggressiveness.Normal)]
    [InlineData(Aggressiveness.High)]
    public void AggressivenessThresholds(Aggressiveness level)
    {
        var settings = new TestTrimSettings { Aggressiveness = level };
        var detector = new CommandDetector(settings);
        var text = "echo hi\\\n --flag yes";

        var result = detector.TransformIfCommand(text);

        Assert.Equal("echo hi --flag yes", result);
    }

    [Fact]
    public void CleanBoxDrawingCharactersDoesNothingWhenDisabled()
    {
        var settings = new TestTrimSettings { RemoveBoxDrawing = false };
        var detector = new CommandDetector(settings);

        Assert.Null(detector.CleanBoxDrawingCharacters("hello │ │ world"));
    }

    [Fact]
    public void CleanBoxDrawingCharactersCollapsesWhitespace()
    {
        var detector = new CommandDetector(new TestTrimSettings());
        var cleaned = detector.CleanBoxDrawingCharacters("│ │ echo   │ │ hi");

        Assert.Equal("echo hi", cleaned);
    }

    [Fact]
    public void CleanBoxDrawingReturnsNullWhenNoGlyphs()
    {
        var detector = new CommandDetector(new TestTrimSettings());

        Assert.Null(detector.CleanBoxDrawingCharacters("echo hi"));
    }

    [Fact]
    public void DetectsCommonCommandsWithFlags()
    {
        var detector = new CommandDetector(new TestTrimSettings());
        var text = """
        docker run
          --rm
        """;

        var result = detector.TransformIfCommand(text);

        Assert.Equal("docker run --rm", result);
    }

    [Fact]
    public void LowAggressivenessSkipsMarkdownList()
    {
        var settings = new TestTrimSettings { Aggressiveness = Aggressiveness.Low };
        var detector = new CommandDetector(settings);
        var text = """
        Shopping list:
        - apples.
        - oranges.
        """;

        var result = detector.TransformIfCommand(text);

        Assert.Null(result);
    }

    private sealed class TestTrimSettings : ITrimSettings
    {
        public Aggressiveness Aggressiveness { get; set; } = Aggressiveness.Normal;
        public bool PreserveBlankLines { get; set; }
        public bool AutoTrimEnabled { get; set; } = true;
        public bool RemoveBoxDrawing { get; set; } = true;
    }
}
