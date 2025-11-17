using System.Linq;
using WinTrimmy;
using Xunit;

namespace WinTrimmy.Tests;

public class ClipboardMonitorTests
{
    [Fact]
    public void ShouldForceFlattenRequiresMultiline()
    {
        Assert.False(ClipboardMonitor.ShouldForceFlatten("single line"));
    }

    [Fact]
    public void ShouldForceFlattenRejectsLongCopies()
    {
        var text = string.Join("\n", Enumerable.Repeat("echo hi", 11));
        Assert.False(ClipboardMonitor.ShouldForceFlatten(text));
    }

    [Fact]
    public void ShouldForceFlattenAcceptsReasonableMultiline()
    {
        var text = "echo hi\nls -la\n";
        Assert.True(ClipboardMonitor.ShouldForceFlatten(text));
    }
}
