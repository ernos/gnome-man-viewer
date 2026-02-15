using Xunit;
using System.Collections.Generic;

namespace GMan.Tests;

public class TypeAheadNavigatorTests
{
    [Fact]
    public void Constructor_InitializesEmptyBuffer()
    {
        var navigator = new TypeAheadNavigator();

        Assert.Equal("", navigator.Buffer);
        Assert.False(navigator.IsActive);
    }

    [Fact]
    public void AppendChar_AddsCharacterToBuffer()
    {
        var navigator = new TypeAheadNavigator();

        navigator.AppendChar('a');

        Assert.Equal("a", navigator.Buffer);
        Assert.True(navigator.IsActive);
    }

    [Fact]
    public void AppendChar_ConvertsToLowercase()
    {
        var navigator = new TypeAheadNavigator();

        navigator.AppendChar('A');
        navigator.AppendChar('B');

        Assert.Equal("ab", navigator.Buffer);
    }

    [Fact]
    public void AppendChar_LimitsBufferTo10Characters()
    {
        var navigator = new TypeAheadNavigator();

        // Add 15 characters
        for (int i = 0; i < 15; i++)
        {
            navigator.AppendChar('a');
        }

        Assert.Equal(10, navigator.Buffer.Length);
        Assert.Equal("aaaaaaaaaa", navigator.Buffer);
    }

    [Fact]
    public void AppendChar_TrimsOldestCharactersWhenExceedingLimit()
    {
        var navigator = new TypeAheadNavigator();

        // Type "abcdefghijklmno" (15 chars)
        foreach (char c in "abcdefghijklmno")
        {
            navigator.AppendChar(c);
        }

        // Should keep only the last 10: "fghijklmno"
        Assert.Equal("fghijklmno", navigator.Buffer);
    }

    [Fact]
    public void Reset_ClearsBuffer()
    {
        var navigator = new TypeAheadNavigator();
        navigator.AppendChar('a');
        navigator.AppendChar('b');

        navigator.Reset();

        Assert.Equal("", navigator.Buffer);
        Assert.False(navigator.IsActive);
    }

    [Fact]
    public void FindMatch_ReturnsNull_WhenBufferEmpty()
    {
        var navigator = new TypeAheadNavigator();
        var items = new List<string> { "apple", "banana", "cherry" };

        var result = navigator.FindMatch(items);

        Assert.Null(result);
    }

    [Fact]
    public void FindMatch_ReturnsFirstMatchingIndex()
    {
        var navigator = new TypeAheadNavigator();
        var items = new List<string> { "apple", "apricot", "banana", "cherry" };

        navigator.AppendChar('a');
        var result = navigator.FindMatch(items);

        Assert.Equal(0, result);
    }

    [Fact]
    public void FindMatch_IsCaseInsensitive()
    {
        var navigator = new TypeAheadNavigator();
        var items = new List<string> { "Apple", "Banana", "Cherry" };

        navigator.AppendChar('b');
        var result = navigator.FindMatch(items);

        Assert.Equal(1, result);
    }

    [Fact]
    public void FindMatch_HandlesMultipleCharacters()
    {
        var navigator = new TypeAheadNavigator();
        var items = new List<string> { "grep", "groff", "gunzip", "gzip" };

        navigator.AppendChar('g');
        navigator.AppendChar('r');
        var result = navigator.FindMatch(items);

        Assert.Equal(0, result); // "grep"
    }

    [Fact]
    public void FindMatch_ReturnsNull_WhenNoMatch()
    {
        var navigator = new TypeAheadNavigator();
        var items = new List<string> { "apple", "banana", "cherry" };

        navigator.AppendChar('z');
        var result = navigator.FindMatch(items);

        Assert.Null(result);
    }

    [Fact]
    public void FindMatch_SkipsNullItems()
    {
        var navigator = new TypeAheadNavigator();
        var items = new List<string?> { null, "apple", "banana" };

        navigator.AppendChar('a');
        var result = navigator.FindMatch(items!);

        Assert.Equal(1, result);
    }

    [Fact]
    public void FindMatch_SkipsEmptyItems()
    {
        var navigator = new TypeAheadNavigator();
        var items = new List<string> { "", "apple", "banana" };

        navigator.AppendChar('a');
        var result = navigator.FindMatch(items);

        Assert.Equal(1, result);
    }

    [Fact]
    public void FindMatchWithSelector_WorksWithComplexTypes()
    {
        var navigator = new TypeAheadNavigator();
        var items = new List<(int id, string name)>
        {
            (1, "grep"),
            (2, "ls"),
            (3, "cat")
        };

        navigator.AppendChar('l');
        var result = navigator.FindMatch(items, item => item.name);

        Assert.Equal(1, result);
    }

    [Fact]
    public void FindMatchWithSelector_HandlesNullReturnFromSelector()
    {
        var navigator = new TypeAheadNavigator();
        var items = new List<string?> { null, "apple" };

        navigator.AppendChar('a');
        var result = navigator.FindMatch(items, item => item);

        Assert.Equal(1, result);
    }

    [Fact]
    public void GetStatusMessage_ReturnsEmptyWhenBufferEmpty()
    {
        var navigator = new TypeAheadNavigator();

        Assert.Equal("", navigator.GetStatusMessage());
    }

    [Fact]
    public void GetStatusMessage_ReturnsFormattedMessage()
    {
        var navigator = new TypeAheadNavigator();
        navigator.AppendChar('g');
        navigator.AppendChar('r');

        Assert.Equal("Type-ahead: gr", navigator.GetStatusMessage());
    }

    [Fact]
    public void GetTimeoutMessage_ReturnsMarkupString()
    {
        var navigator = new TypeAheadNavigator();

        var message = navigator.GetTimeoutMessage();

        Assert.Contains("Type-ahead timeout", message);
        Assert.Contains("<span", message);
    }

    [Fact]
    public void TypeAheadScenario_UserTypesAndFindsMatch()
    {
        // Realistic scenario: User has a list and types "gre" to find "grep"
        var navigator = new TypeAheadNavigator();
        var programs = new List<string> { "cat", "grep", "groff", "ls", "man" };

        // User types 'g'
        navigator.AppendChar('g');
        Assert.Equal("Type-ahead: g", navigator.GetStatusMessage());
        var match1 = navigator.FindMatch(programs);
        Assert.Equal(1, match1); // "grep"

        // User types 'r'
        navigator.AppendChar('r');
        Assert.Equal("Type-ahead: gr", navigator.GetStatusMessage());
        var match2 = navigator.FindMatch(programs);
        Assert.Equal(1, match2); // Still "grep"

        // User types 'e'
        navigator.AppendChar('e');
        Assert.Equal("Type-ahead: gre", navigator.GetStatusMessage());
        var match3 = navigator.FindMatch(programs);
        Assert.Equal(1, match3); // Still "grep"

        // Timeout occurs - reset
        navigator.Reset();
        Assert.False(navigator.IsActive);
    }

    [Fact]
    public void TypeAheadScenario_UserCorrectsMistake()
    {
        // Scenario: User types wrong character, buffer adjusts
        var navigator = new TypeAheadNavigator();
        var programs = new List<string> { "cat", "grep", "ls" };

        navigator.AppendChar('x'); // Wrong character
        var match1 = navigator.FindMatch(programs);
        Assert.Null(match1);

        // User types more, old 'x' moves through buffer
        navigator.AppendChar('g');
        navigator.AppendChar('r');
        navigator.AppendChar('e');
        navigator.AppendChar('p');
        Assert.Equal("xgrep", navigator.Buffer);

        // Still no match because buffer is "xgrep"
        var match2 = navigator.FindMatch(programs);
        Assert.Null(match2);
    }
}
