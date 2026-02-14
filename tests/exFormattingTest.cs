using System.Text.RegularExpressions;
using Xunit;

namespace GManTests;

public class RegexFormattingTests
{
    // Option regex from MainWindow.cs
    private const string OptionPattern = @"(?<=^|\s|\[|\{|\||,)(-[a-zA-Z0-9?]+(?:=\[[^\]]+\]|=[^\s,\[\]]+)?|--[a-zA-Z][-a-zA-Z0-9\u2010]*(?:=\[[^\]]+\]|=[^\s,\[\]]+|\[[^\]]+\])?)(?=[\s,\[\]\{\}\|]|$)";
    
    // Man page reference regex from MainWindow.cs
    private const string ManReferencePattern = @"([a-zA-Z0-9_\-\.]+)\(\d+\)";

    [Theory]
    [InlineData("aa-stack(8)", "aa-stack")]
    [InlineData("aa-namespace(8)", "aa-namespace")]
    [InlineData("apparmor(7)", "apparmor")]
    [InlineData("apparmor.d(5)", "apparmor.d")]
    [InlineData("aa_change_profile(3)", "aa_change_profile")]
    [InlineData("aa_change_onexec(3)", "aa_change_onexec")]
    [InlineData("systemd-networkd(8)", "systemd-networkd")]
    [InlineData("apt-get(8)", "apt-get")]
    [InlineData("gcc(1)", "gcc")]
    public void ManPageReference_ShouldMatchAndExtractProgramName(string input, string expectedProgramName)
    {
        var match = Regex.Match(input, ManReferencePattern);
        Assert.True(match.Success, $"Pattern should match '{input}'");
        Assert.Equal(input, match.Value); // Full match includes (number)
        Assert.Equal(expectedProgramName, match.Groups[1].Value); // Group 1 is just the program name
    }

    [Theory]
    [InlineData("SEE ALSO", new string[] { })] // No references in header
    [InlineData("Just some text", new string[] { })] // No references in plain text
    [InlineData("program", new string[] { })] // Program name without section number
    [InlineData("(8)", new string[] { })] // Section number without program name
    public void ManPageReference_ShouldNotMatch_InvalidFormats(string input, string[] expected)
    {
        var matches = Regex.Matches(input, ManReferencePattern);
        Assert.Equal(expected.Length, matches.Count);
    }

    [Fact]
    public void RealSeeAlsoSection_ShouldMatchAllReferences()
    {
        string seeAlsoLine = "aa-stack(8), aa-namespace(8), apparmor(7), apparmor.d(5), aa_change_profile(3), aa_change_onexec(3) and <https://wiki.apparmor.net>.";
        var matches = Regex.Matches(seeAlsoLine, ManReferencePattern);
        
        Assert.Equal(6, matches.Count);
        Assert.Equal("aa-stack", matches[0].Groups[1].Value);
        Assert.Equal("aa-namespace", matches[1].Groups[1].Value);
        Assert.Equal("apparmor", matches[2].Groups[1].Value);
        Assert.Equal("apparmor.d", matches[3].Groups[1].Value);
        Assert.Equal("aa_change_profile", matches[4].Groups[1].Value);
        Assert.Equal("aa_change_onexec", matches[5].Groups[1].Value);
    }


    [Theory]
    [InlineData("-x", "-x")]
    [InlineData("-arj", "-arj")]
    [InlineData("-arj32", "-arj32")]
    [InlineData("-box", "-box")]
    [InlineData("-?", "-?")]
    [InlineData("--help", "--help")]
    [InlineData("--no-pager", "--no-pager")]
    [InlineData("--list-opts", "--list-opts")]
    [InlineData("[--un‐conditional]", "--un‐conditional")]

    
    public void SingleOption_ShouldMatch(string input, string expected)
    {
        var match = Regex.Match(input, OptionPattern);
        Assert.True(match.Success, $"Pattern should match '{input}'");
        Assert.Equal(expected, match.Value);
    }

    [Theory]
    [InlineData("-x=foo", "-x=foo")]
    [InlineData("-output=file.txt", "-output=file.txt")]
    [InlineData("--format=json", "--format=json")]
    [InlineData("--listing-cont-lines=NUM", "--listing-cont-lines=NUM")]
    [InlineData("--multibyte-handling=[allow|warn|warn-sym-only]", "--multibyte-handling=[allow|warn|warn-sym-only]")]
    public void OptionWithValue_ShouldMatch(string input, string expected)
    {
        var match = Regex.Match(input, OptionPattern);
        Assert.True(match.Success, $"Pattern should match '{input}'");
        Assert.Equal(expected, match.Value);
    }

    [Theory]
    [InlineData("-h,--help", new[] { "-h", "--help" })]
    [InlineData("-h, --help", new[] { "-h", "--help" })]
    [InlineData("[-a|--addresses]", new[] { "-a", "--addresses" })]
    [InlineData("{-v | --version}", new[] { "-v", "--version" })]
    public void MultipleOptions_ShouldMatchAll(string input, string[] expected)
    {
        var matches = Regex.Matches(input, OptionPattern);
        Assert.Equal(expected.Length, matches.Count);
        
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], matches[i].Value);
        }
    }

    [Theory]
    [InlineData("[-b bfdname|--target=bfdname]", new[] { "-b", "--target=bfdname" })]
    [InlineData("[-e filename|--exe=filename]", new[] { "-e", "--exe=filename" })]
    [InlineData("[-C|--demangle[=style]]", new[] { "-C", "--demangle[=style]" })]
    public void ComplexSynopsisLine_ShouldMatchOptions(string input, string[] expected)
    {
        var matches = Regex.Matches(input, OptionPattern);
        Assert.Equal(expected.Length, matches.Count);
        
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], matches[i].Value);
        }
    }

    [Theory]
    [InlineData("something-verbose")]
    [InlineData("aa-features-abi")]
    [InlineData("APT-MARK(8)")]
    [InlineData("apt-mark(8)")]
    [InlineData("prefix-x")]
    public void DashInMiddleOfWord_ShouldNotMatch(string input)
    {
        var matches = Regex.Matches(input, OptionPattern);
        Assert.Empty(matches);
    }

    [Theory]
    [InlineData("       -arj   Registers all ARJ", "-arj")]
    [InlineData("       --no-pager", "--no-pager")]
    [InlineData("[-x FILE]", "-x")]
    public void OptionWithWhitespace_ShouldMatch(string input, string expected)
    {
        var match = Regex.Match(input, OptionPattern);
        Assert.True(match.Success);
        Assert.Equal(expected, match.Value);
    }

    [Fact]
    public void RealManPageSnippet_ShouldMatchAllOptions()
    {
        string snippet = @"
       addr2line [-a|--addresses]
                 [-b bfdname|--target=bfdname]
                 [-C|--demangle[=style]]
                 [-e filename|--exe=filename]
                 [-H|--help] [-V|--version]";

        var matches = Regex.Matches(snippet, OptionPattern);
        
        var expectedOptions = new[] {
            "-a", "--addresses",
            "-b", "--target=bfdname",
            "-C", "--demangle[=style]",
            "-e", "--exe=filename",
            "-H", "--help",
            "-V", "--version"
        };

        Assert.Equal(expectedOptions.Length, matches.Count);
        
        for (int i = 0; i < expectedOptions.Length; i++)
        {
            Assert.Equal(expectedOptions[i], matches[i].Value);
        }
    }

    [Theory]
    [InlineData("-A auth-username:password", "-A")]
    [InlineData("-X proxy[:port]", "-X")]
    public void OptionFollowedByArgument_ShouldMatchOption(string input, string expected)
    {
        var match = Regex.Match(input, OptionPattern);
        Assert.True(match.Success);
        Assert.Equal(expected, match.Value);
    }
}