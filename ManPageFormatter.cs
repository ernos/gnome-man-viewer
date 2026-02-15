using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GMan;

/// <summary>
/// Abstraction over GTK TextBuffer to decouple formatting logic from GTK
/// </summary>
public interface ITextBuffer
{
    string Text { get; }
    void ApplyTag(string tagName, int startOffset, int endOffset);
}

/// <summary>
/// Represents styling information for text tags
/// </summary>
public class TextTagStyle
{
    public string Name { get; set; } = "";
    public string? Foreground { get; set; }
    public string? Background { get; set; }
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public double Scale { get; set; } = 1.0;
}

/// <summary>
/// Result of formatting operation containing references to other man pages
/// </summary>
public class FormatResult
{
    public Dictionary<(int start, int end), string> ManPageReferences { get; set; } = new();
}

/// <summary>
/// Pure-logic formatter for man pages and help text, independent of GTK
/// </summary>
public class ManPageFormatter
{
    // Tag name constants
    public const string HeaderTag = "header";
    public const string CommandTag = "command";
    public const string OptionTag = "option";
    public const string ArgumentTag = "argument";
    public const string BoldTag = "bold";
    public const string FilePathTag = "filePath";
    public const string UrlTag = "url";
    public const string ManReferenceTag = "manReference";

    /// <summary>
    /// Get default tag styles for man page formatting
    /// </summary>
    public static Dictionary<string, TextTagStyle> GetDefaultTagStyles()
    {
        return new Dictionary<string, TextTagStyle>
        {
            [HeaderTag] = new TextTagStyle
            {
                Name = HeaderTag,
                Foreground = "#2E86AB",  // Blue
                Bold = true,
                Scale = 1.3
            },
            [CommandTag] = new TextTagStyle
            {
                Name = CommandTag,
                Foreground = "#A23B72",  // Purple
                Bold = true
            },
            [OptionTag] = new TextTagStyle
            {
                Name = OptionTag,
                Foreground = "#F18F01",  // Orange
                Bold = true
            },
            [ArgumentTag] = new TextTagStyle
            {
                Name = ArgumentTag,
                Foreground = "#C73E1D",  // Red
                Italic = true
            },
            [BoldTag] = new TextTagStyle
            {
                Name = BoldTag,
                Bold = true
            },
            [FilePathTag] = new TextTagStyle
            {
                Name = FilePathTag,
                Foreground = "#06A77D",  // Teal/Green
                Underline = true
            },
            [UrlTag] = new TextTagStyle
            {
                Name = UrlTag,
                Foreground = "#0077CC",  // Blue
                Underline = true
            },
            [ManReferenceTag] = new TextTagStyle
            {
                Name = ManReferenceTag,
                Foreground = "#0077CC",  // Blue
                Underline = true
            }
        };
    }

    /// <summary>
    /// Format a man page with syntax highlighting
    /// </summary>
    public FormatResult FormatManPage(ITextBuffer buffer, string programName)
    {
        var result = new FormatResult();
        string text = buffer.Text;
        string[] lines = text.Split('\n');

        int lineStart = 0;
        bool inSeeAlsoSection = false;
        bool inNameSection = false;
        bool inSynopsisSection = false;

        foreach (string line in lines)
        {
            int lineLength = line.Length;

            // Format section headers (all caps words at start of line)
            if (Regex.IsMatch(line, @"^[A-Z][A-Z\s]+$") && line.Trim().Length > 0)
            {
                buffer.ApplyTag(HeaderTag, lineStart, lineStart + lineLength);

                // Check which section we're in
                string trimmedLine = line.Trim();
                inSeeAlsoSection = (trimmedLine == "SEE ALSO");
                inNameSection = (trimmedLine == "NAME");
                inSynopsisSection = (trimmedLine == "SYNOPSIS");
            }
            // Highlight first word on line in NAME and SYNOPSIS sections
            else if ((inNameSection || inSynopsisSection) && !string.IsNullOrWhiteSpace(line))
            {
                // Extract first word (sequence of non-whitespace characters at start of line after optional whitespace)
                var firstWordMatch = Regex.Match(line, @"^\s*([\S]+)");
                if (firstWordMatch.Success)
                {
                    var wordGroup = firstWordMatch.Groups[1];
                    int wordStart = lineStart + wordGroup.Index;
                    int wordEnd = wordStart + wordGroup.Length;
                    buffer.ApplyTag(CommandTag, wordStart, wordEnd);
                }
            }
            // Format command names (program name in various contexts)
            // Skip lines that look like man page headers/footers: COMMAND(8)...COMMAND(8)
            else if (line.Contains(programName) && !Regex.IsMatch(line, @"^\S+\(\d+\).*\S+\(\d+\)\s*$"))
            {
                int index = 0;
                while ((index = line.IndexOf(programName, index)) != -1)
                {
                    // Check if this is a whole word match (word boundaries before and after)
                    bool isWordBoundaryBefore = index == 0 || char.IsWhiteSpace(line[index - 1]) || char.IsPunctuation(line[index - 1]);
                    bool isWordBoundaryAfter = (index + programName.Length >= line.Length) ||
                                              char.IsWhiteSpace(line[index + programName.Length]) ||
                                              char.IsPunctuation(line[index + programName.Length]);

                    if (isWordBoundaryBefore && isWordBoundaryAfter)
                    {
                        int cmdStart = lineStart + index;
                        int cmdEnd = cmdStart + programName.Length;
                        buffer.ApplyTag(CommandTag, cmdStart, cmdEnd);
                    }
                    index += programName.Length;
                }
            }

            // Format options and arguments using regex
            // Match options: -x, -?, -x=value, --option, --option=value, -arj, -box, etc.
            // Can be inside brackets [] or braces {}, separated by pipes |
            // Values can contain brackets/braces/pipes for syntax (e.g., --opt=[val1|val2])
            // Note: Brackets without '=' are documentation (e.g., -product[version]), not syntax
            var optionMatches = Regex.Matches(line,
                @"(?<=^|\s|\[|\{|\||,)(-[-a-zA-Z0-9?]+(?:=\[[^\]]+\]|=[^\s,\[\]]+)?|--[a-zA-Z][-a-zA-Z0-9\u2010]*(?:=\[[^\]]+\]|=[^\s,\[\]]+)?)(?=[\s,\[\]\{\}\|]|$)");

            foreach (Match match in optionMatches)
            {
                int optStart = lineStart + match.Index;
                int optEnd = optStart + match.Length;
                buffer.ApplyTag(OptionTag, optStart, optEnd);
            }

            // Format argument placeholders
            // Match: <WORD>, UPPERCASE_WORDS, single lowercase letter before comma, 
            // lowercase_with_underscores_or-dashes, lowercase words after dash options with special chars
            var argMatches = Regex.Matches(line,
                @"<[A-Z_][A-Z_0-9]*>|(?<![a-zA-Z])[A-Z][A-Z_0-9]+(?![a-zA-Z])|(?<=^|\s)[a-z](?=,\s)|(?<=^|\s)[a-z][a-z0-9]*[_\-][a-z0-9_\-:<>\[\]]*|(?<=-[a-zA-Z0-9?]+\s)[a-z][a-z0-9\-:<>\[\]]*");

            foreach (Match match in argMatches)
            {
                int argStart = lineStart + match.Index;
                int argEnd = argStart + match.Length;
                buffer.ApplyTag(ArgumentTag, argStart, argEnd);
            }

            // Format URLs (http:// or https://)
            var urlMatches = Regex.Matches(line, @"https?://[^\s<>\[\]]+");
            foreach (Match match in urlMatches)
            {
                int urlStart = lineStart + match.Index;
                int urlEnd = urlStart + match.Length;
                buffer.ApplyTag(UrlTag, urlStart, urlEnd);
            }

            // Format file paths (starting with / or ~/)
            var filePathMatches = Regex.Matches(line, @"(?:^|\s)(~?/[/\w\-\.]+)");
            foreach (Match match in filePathMatches)
            {
                // Use Group 1 to skip the leading whitespace
                if (match.Groups.Count > 1)
                {
                    var pathGroup = match.Groups[1];
                    int pathStart = lineStart + pathGroup.Index;
                    int pathEnd = pathStart + pathGroup.Length;
                    buffer.ApplyTag(FilePathTag, pathStart, pathEnd);
                }
            }

            // Format man page references (e.g., program(1), command(8))
            // Match pattern: word-characters followed by (number)
            // This matches: aa-stack(8), apparmor(7), aa_change_profile(3), etc.
            var manRefMatches = Regex.Matches(line, @"([a-zA-Z0-9_\-\.]+)\(\d+\)");
            foreach (Match match in manRefMatches)
            {
                // Extract just the program name (without the section number)
                string progName = match.Groups[1].Value;  // e.g., "aa-stack"

                int matchStart = lineStart + match.Index;
                int matchEnd = matchStart + match.Length;

                buffer.ApplyTag(ManReferenceTag, matchStart, matchEnd);

                // Store the reference for click handling
                result.ManPageReferences[(matchStart, matchEnd)] = progName;
            }

            lineStart += lineLength + 1; // +1 for newline character
        }

        return result;
    }

    /// <summary>
    /// Format help text (from --help output) with basic syntax highlighting
    /// </summary>
    public void FormatHelpText(ITextBuffer buffer)
    {
        string text = buffer.Text;
        string[] lines = text.Split('\n');

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Calculate line offset
            int lineStart = 0;
            for (int i = 0; i < lineIndex; i++)
                lineStart += lines[i].Length + 1; // +1 for newline

            // Section headers (all caps, standalone lines)
            // Must not be indented and must be all uppercase
            if (line == line.ToUpper() && line.Length > 0 &&
                !line.StartsWith("    ") && !line.StartsWith("\t"))
            {
                buffer.ApplyTag(HeaderTag, lineStart, lineStart + line.Length);
                continue;
            }

            // Options and commands indented with spaces
            if (line.StartsWith("    ") && line.Trim().Length > 0)
            {
                // Check for option patterns like "+ key", "- key", "Enter", etc.
                var optionMatch = Regex.Match(line, @"^\s+([+\-]|Enter|Return|Letters)\s+(key|-)\s");
                if (optionMatch.Success)
                {
                    int matchStart = lineStart + optionMatch.Index;
                    int matchEnd = matchStart + optionMatch.Length;
                    buffer.ApplyTag(OptionTag, matchStart, matchEnd);
                    continue;
                }
            }

            // File paths
            if (line.Contains("~/.config/gman") || line.Contains("/home/"))
            {
                var pathMatches = Regex.Matches(line, @"(/[^\s]+|~/[^\s]+)");
                foreach (Match match in pathMatches)
                {
                    int matchStart = lineStart + match.Index;
                    int matchEnd = matchStart + match.Length;
                    buffer.ApplyTag(FilePathTag, matchStart, matchEnd);
                }
            }

            // URLs
            if (line.Contains("http://") || line.Contains("https://"))
            {
                var urlMatches = Regex.Matches(line, @"https?://[^\s]+");
                foreach (Match match in urlMatches)
                {
                    int matchStart = lineStart + match.Index;
                    int matchEnd = matchStart + match.Length;
                    buffer.ApplyTag(UrlTag, matchStart, matchEnd);
                }
            }
        }
    }
}
