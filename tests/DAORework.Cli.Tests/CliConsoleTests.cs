using DAORework.Cli.Composition;
using Spectre.Console;
using Xunit;

namespace DAORework.Cli.Tests;

/// <summary>Found by the owner's verify script: a long support-bundle path was folded across lines and could not be read back.</summary>
public sealed class CliConsoleTests
{
    [Fact]
    public void Redirected_LongLine_IsWrittenOnOneLine()
    {
        using var output = new StringWriter();
        var path = @"C:\Users\someone\AppData\Local\DAORework\reports\" + new string('x', 200) + ".zip";

        CliConsole.Create(output, outputRedirected: true).WriteLine($"Support bundle written to: {path}");

        Assert.Equal($"Support bundle written to: {path}", output.ToString().TrimEnd('\r', '\n'));
    }

    [Fact]
    public void Redirected_WritesNoEscapeCodes()
    {
        using var output = new StringWriter();

        CliConsole.Create(output, outputRedirected: true).WriteLine("plain");

        Assert.DoesNotContain("\u001b", output.ToString(), StringComparison.Ordinal);
    }
}
