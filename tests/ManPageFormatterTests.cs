using Xunit;
using GMan;
using System.Collections.Generic;
using System.Linq;

namespace GMan.Tests;

/// <summary>
/// Mock implementation of ITextBuffer for testing
/// </summary>
public class MockTextBuffer : ITextBuffer
{
    public string Text { get; set; } = "";
    public List<(string tagName, int start, int end)> AppliedTags { get; } = new();

    public MockTextBuffer(string text)
    {
        Text = text;
    }

    public void ApplyTag(string tagName, int startOffset, int endOffset)
    {
        AppliedTags.Add((tagName, startOffset, endOffset));
    }

    public bool HasTag(string tagName, int start, int end)
    {
        return AppliedTags.Any(t => t.tagName == tagName && t.start == start && t.end == end);
    }

    public List<(int start, int end)> GetTagPositions(string tagName)
    {
        return AppliedTags
            .Where(t => t.tagName == tagName)
            .Select(t => (t.start, t.end))
            .ToList();
    }

    public string GetTaggedText(string tagName)
    {
        var positions = GetTagPositions(tagName);
        return string.Join(", ", positions.Select(p => Text.Substring(p.start, p.end - p.start)));
    }
}

public class ManPageFormatterTests
{
    [Fact]
    public void GetDefaultTagStyles_ReturnsAllRequiredTags()
    {
        var styles = ManPageFormatter.GetDefaultTagStyles();

        Assert.Contains(ManPageFormatter.HeaderTag, styles.Keys);
        Assert.Contains(ManPageFormatter.CommandTag, styles.Keys);
        Assert.Contains(ManPageFormatter.OptionTag, styles.Keys);
        Assert.Contains(ManPageFormatter.ArgumentTag, styles.Keys);
        Assert.Contains(ManPageFormatter.BoldTag, styles.Keys);
        Assert.Contains(ManPageFormatter.FilePathTag, styles.Keys);
        Assert.Contains(ManPageFormatter.UrlTag, styles.Keys);
        Assert.Contains(ManPageFormatter.ManReferenceTag, styles.Keys);
    }

    [Fact]
    public void FormatManPage_AppliesHeaderTag_ToAllCapsLines()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("NAME\nls - list directory\n\nDESCRIPTION\nSome text\n");

        formatter.FormatManPage(buffer, "ls");

        var headerPositions = buffer.GetTagPositions(ManPageFormatter.HeaderTag);
        Assert.Equal(2, headerPositions.Count);
        Assert.Equal("NAME", buffer.Text.Substring(headerPositions[0].start, headerPositions[0].end - headerPositions[0].start));
        Assert.Equal("DESCRIPTION", buffer.Text.Substring(headerPositions[1].start, headerPositions[1].end - headerPositions[1].start));
    }

    [Fact]
    public void FormatManPage_AppliesCommandTag_ToProgramName()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("The ls command lists files. Use ls often.\n");

        formatter.FormatManPage(buffer, "ls");

        var tagged = buffer.GetTaggedText(ManPageFormatter.CommandTag);
        Assert.Contains("ls", tagged);
    }

    [Fact]
    public void FormatManPage_AppliesCommandTag_ToFirstWordInNAMESection()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("NAME\ngrep - search text patterns\n");

        formatter.FormatManPage(buffer, "grep");

        var commandPositions = buffer.GetTagPositions(ManPageFormatter.CommandTag);
        // Should tag "grep" on the line after NAME
        Assert.True(commandPositions.Count >= 1);

        var firstTaggedWord = buffer.Text.Substring(commandPositions[0].start, commandPositions[0].end - commandPositions[0].start);
        Assert.Equal("grep", firstTaggedWord);
    }

    [Fact]
    public void FormatManPage_AppliesOptionTag_ToShortOptions()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("Use -h for help or -v for verbose.\n");

        formatter.FormatManPage(buffer, "test");

        var tagged = buffer.GetTaggedText(ManPageFormatter.OptionTag);
        Assert.Contains("-h", tagged);
        Assert.Contains("-v", tagged);
    }

    [Fact]
    public void FormatManPage_AppliesOptionTag_ToLongOptions()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("Use --help or --version for information.\n");

        formatter.FormatManPage(buffer, "test");

        var tagged = buffer.GetTaggedText(ManPageFormatter.OptionTag);
        Assert.Contains("--help", tagged);
        Assert.Contains("--version", tagged);
    }

    [Fact]
    public void FormatManPage_AppliesOptionTag_ToOptionsWithValues()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("Use --output=file.txt or -n=5 to specify.\n");

        formatter.FormatManPage(buffer, "test");

        var tagged = buffer.GetTaggedText(ManPageFormatter.OptionTag);
        Assert.Contains("--output=file.txt", tagged);
        Assert.Contains("-n=5", tagged);
    }

    [Fact]
    public void FormatManPage_DoesNotMatchPartialWords_InBrackets()
    {
        // This tests the regex fix for -productv[ersion] should only match -productv
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("-productv[ersion]: Product version\n");

        formatter.FormatManPage(buffer, "test");

        var optionPositions = buffer.GetTagPositions(ManPageFormatter.OptionTag);
        Assert.True(optionPositions.Count >= 1);

        var taggedOption = buffer.Text.Substring(optionPositions[0].start, optionPositions[0].end - optionPositions[0].start);
        Assert.Equal("-productv", taggedOption);
        Assert.DoesNotContain("[ersion]", taggedOption);
    }

    [Fact]
    public void FormatManPage_AppliesArgumentTag_ToAngleBracketArgs()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("Usage: command <FILE> <OUTPUT>\n");

        formatter.FormatManPage(buffer, "command");

        var tagged = buffer.GetTaggedText(ManPageFormatter.ArgumentTag);
        Assert.Contains("<FILE>", tagged);
        Assert.Contains("<OUTPUT>", tagged);
    }

    [Fact]
    public void FormatManPage_AppliesArgumentTag_ToUppercaseWords()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("Requires USERNAME and PASSWORD to authenticate.\n");

        formatter.FormatManPage(buffer, "test");

        var tagged = buffer.GetTaggedText(ManPageFormatter.ArgumentTag);
        Assert.Contains("USERNAME", tagged);
        Assert.Contains("PASSWORD", tagged);
    }

    [Fact]
    public void FormatManPage_AppliesUrlTag_ToHttpUrls()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("Visit http://example.com for more info.\n");

        formatter.FormatManPage(buffer, "test");

        var tagged = buffer.GetTaggedText(ManPageFormatter.UrlTag);
        Assert.Contains("http://example.com", tagged);
    }

    [Fact]
    public void FormatManPage_AppliesUrlTag_ToHttpsUrls()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("See https://www.example.com/docs for details.\n");

        formatter.FormatManPage(buffer, "test");

        var tagged = buffer.GetTaggedText(ManPageFormatter.UrlTag);
        Assert.Contains("https://www.example.com/docs", tagged);
    }

    [Fact]
    public void FormatManPage_AppliesFilePathTag_ToAbsolutePaths()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("Configuration file: /etc/config.conf\n");

        formatter.FormatManPage(buffer, "test");

        var tagged = buffer.GetTaggedText(ManPageFormatter.FilePathTag);
        Assert.Contains("/etc/config.conf", tagged);
    }

    [Fact]
    public void FormatManPage_AppliesFilePathTag_ToHomePaths()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("User config: ~/.config/app/settings.toml\n");

        formatter.FormatManPage(buffer, "test");

        var tagged = buffer.GetTaggedText(ManPageFormatter.FilePathTag);
        Assert.Contains("~/.config/app/settings.toml", tagged);
    }

    [Fact]
    public void FormatManPage_AppliesManReferenceTag_AndStoresReferences()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("SEE ALSO\ngrep(1), sed(1), awk(1)\n");

        var result = formatter.FormatManPage(buffer, "test");

        var tagged = buffer.GetTaggedText(ManPageFormatter.ManReferenceTag);
        Assert.Contains("grep(1)", tagged);
        Assert.Contains("sed(1)", tagged);
        Assert.Contains("awk(1)", tagged);

        // Check that references are stored without section numbers
        Assert.Contains("grep", result.ManPageReferences.Values);
        Assert.Contains("sed", result.ManPageReferences.Values);
        Assert.Contains("awk", result.ManPageReferences.Values);
    }

    [Fact]
    public void FormatManPage_HandlesComplexManReferences()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("See aa-stack(8), apparmor(7), aa_change_profile(3)\n");

        var result = formatter.FormatManPage(buffer, "test");

        Assert.Contains("aa-stack", result.ManPageReferences.Values);
        Assert.Contains("apparmor", result.ManPageReferences.Values);
        Assert.Contains("aa_change_profile", result.ManPageReferences.Values);
    }

    [Fact]
    public void FormatHelpText_AppliesHeaderTag_ToAllCapsLines()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("USAGE\n    program [options]\n\nOPTIONS\n    -h  help\n");

        formatter.FormatHelpText(buffer);

        var headerPositions = buffer.GetTagPositions(ManPageFormatter.HeaderTag);
        Assert.Equal(2, headerPositions.Count);
    }

    [Fact]
    public void FormatHelpText_AppliesOptionTag_ToKeyPatterns()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("Controls:\n    + key - add item\n    - key - remove item\n");

        formatter.FormatHelpText(buffer);

        var optionPositions = buffer.GetTagPositions(ManPageFormatter.OptionTag);
        Assert.True(optionPositions.Count >= 2);
    }

    [Fact]
    public void FormatHelpText_AppliesFilePathTag_ToConfigPaths()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("Config: ~/.config/gman/settings.conf\n");

        formatter.FormatHelpText(buffer);

        var tagged = buffer.GetTaggedText(ManPageFormatter.FilePathTag);
        Assert.Contains("~/.config/gman/settings.conf", tagged);
    }

    [Fact]
    public void FormatHelpText_AppliesUrlTag()
    {
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("Website: https://www.yourdev.net/gman\n");

        formatter.FormatHelpText(buffer);

        var tagged = buffer.GetTaggedText(ManPageFormatter.UrlTag);
        Assert.Contains("https://www.yourdev.net/gman", tagged);
    }

    [Fact]
    public void FormatManPage_DoesNotTagProgramNameInHeaderFooter()
    {
        // Man pages have headers like: LS(1)     User Commands     LS(1)
        // We should not highlight "LS" in these
        var formatter = new ManPageFormatter();
        var buffer = new MockTextBuffer("LS(1)                    User Commands                    LS(1)\n\nNAME\nls - list files\n");

        formatter.FormatManPage(buffer, "ls");

        // The ls in "NAME" section should be tagged, but not in the header
        var commandPositions = buffer.GetTagPositions(ManPageFormatter.CommandTag);

        // Should have at least one tag (in NAME section)
        Assert.True(commandPositions.Count >= 1);

        // First line should not have command tag (it's a header/footer)
        var firstLineEnd = buffer.Text.IndexOf('\n');
        var tagsInFirstLine = commandPositions.Where(p => p.start < firstLineEnd).ToList();
        Assert.Empty(tagsInFirstLine);
    }
}
