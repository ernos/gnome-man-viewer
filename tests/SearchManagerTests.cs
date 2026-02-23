using System;
using Xunit;

namespace GMan.Tests;

public class SearchManagerTests
{
    [Fact]
    public void FindMatches_EmptyText_ReturnsZero()
    {
        var manager = new SearchManager();

        int matchCount = manager.FindMatches("", "test");

        Assert.Equal(0, matchCount);
        Assert.Equal(0, manager.MatchCount);
        Assert.False(manager.HasMatches);
    }

    [Fact]
    public void FindMatches_EmptySearchTerm_ReturnsZero()
    {
        var manager = new SearchManager();

        int matchCount = manager.FindMatches("some text", "");

        Assert.Equal(0, matchCount);
        Assert.Equal(0, manager.MatchCount);
        Assert.False(manager.HasMatches);
    }

    [Fact]
    public void FindMatches_NullSearchTerm_ReturnsZero()
    {
        var manager = new SearchManager();

        int matchCount = manager.FindMatches("some text", null!);

        Assert.Equal(0, matchCount);
        Assert.Equal(0, manager.MatchCount);
        Assert.False(manager.HasMatches);
    }

    [Fact]
    public void FindMatches_SingleMatch_ReturnsOne()
    {
        var manager = new SearchManager();

        int matchCount = manager.FindMatches("hello world", "world");

        Assert.Equal(1, matchCount);
        Assert.Equal(1, manager.MatchCount);
        Assert.True(manager.HasMatches);
        Assert.Equal("world", manager.SearchTerm);
    }

    [Fact]
    public void FindMatches_MultipleMatches_ReturnsCorrectCount()
    {
        var manager = new SearchManager();

        int matchCount = manager.FindMatches("the cat in the hat", "the");

        Assert.Equal(2, matchCount);
        Assert.Equal(2, manager.MatchCount);
        Assert.True(manager.HasMatches);
    }

    [Fact]
    public void FindMatches_CaseInsensitive_FindsMatches()
    {
        var manager = new SearchManager();

        int matchCount = manager.FindMatches("Hello WORLD world", "world");

        Assert.Equal(2, matchCount);
    }

    [Fact]
    public void FindMatches_OverlappingMatches_FindsAll()
    {
        var manager = new SearchManager();

        int matchCount = manager.FindMatches("aaa", "aa");

        // "aa" appears at positions 0 and 1 in "aaa"
        Assert.Equal(2, matchCount);
    }

    [Fact]
    public void FindMatches_NoMatches_ReturnsZero()
    {
        var manager = new SearchManager();

        int matchCount = manager.FindMatches("hello world", "foo");

        Assert.Equal(0, matchCount);
        Assert.False(manager.HasMatches);
    }

    [Fact]
    public void FindMatches_SetsCurrentIndexToZero()
    {
        var manager = new SearchManager();

        manager.FindMatches("one two three", "o");

        Assert.Equal(0, manager.CurrentIndex);
    }

    [Fact]
    public void GetMatches_ReturnsCorrectPositions()
    {
        var manager = new SearchManager();
        manager.FindMatches("hello world", "o");

        var matches = manager.GetMatches();

        Assert.Equal(2, matches.Count);
        Assert.Equal((4, 5), matches[0]); // "o" in "hello"
        Assert.Equal((7, 8), matches[1]); // "o" in "world"
    }

    [Fact]
    public void GetMatches_ReturnsCopy_NotReference()
    {
        var manager = new SearchManager();
        manager.FindMatches("test test", "test");

        var matches1 = manager.GetMatches();
        matches1.Add((99, 100));
        var matches2 = manager.GetMatches();

        Assert.Equal(2, matches2.Count); // Original count unchanged
    }

    [Fact]
    public void GetCurrentMatch_WithMatches_ReturnsFirstMatch()
    {
        var manager = new SearchManager();
        manager.FindMatches("one two one", "one");

        var currentMatch = manager.GetCurrentMatch();

        Assert.NotNull(currentMatch);
        Assert.Equal((0, 3), currentMatch);
    }

    [Fact]
    public void GetCurrentMatch_NoMatches_ReturnsNull()
    {
        var manager = new SearchManager();
        manager.FindMatches("hello world", "foo");

        var currentMatch = manager.GetCurrentMatch();

        Assert.Null(currentMatch);
    }

    [Fact]
    public void NavigateToNext_WithMatches_AdvancesIndex()
    {
        var manager = new SearchManager();
        manager.FindMatches("one two one", "one");

        int newIndex = manager.NavigateToNext();

        Assert.Equal(1, newIndex);
        Assert.Equal(1, manager.CurrentIndex);
    }

    [Fact]
    public void NavigateToNext_AtLastMatch_WrapsToFirst()
    {
        var manager = new SearchManager();
        manager.FindMatches("a b a", "a");
        manager.NavigateToNext(); // Move to index 1 (last match)

        int newIndex = manager.NavigateToNext(); // Should wrap to 0

        Assert.Equal(0, newIndex);
        Assert.Equal(0, manager.CurrentIndex);
    }

    [Fact]
    public void NavigateToNext_NoMatches_ReturnsMinusOne()
    {
        var manager = new SearchManager();
        manager.FindMatches("hello", "foo");

        int newIndex = manager.NavigateToNext();

        Assert.Equal(-1, newIndex);
    }

    [Fact]
    public void NavigateToPrevious_WithMatches_DecreasesIndex()
    {
        var manager = new SearchManager();
        manager.FindMatches("one two one", "one");
        manager.NavigateToNext(); // Move to index 1

        int newIndex = manager.NavigateToPrevious();

        Assert.Equal(0, newIndex);
        Assert.Equal(0, manager.CurrentIndex);
    }

    [Fact]
    public void NavigateToPrevious_AtFirstMatch_WrapsToLast()
    {
        var manager = new SearchManager();
        manager.FindMatches("a b a", "a");

        int newIndex = manager.NavigateToPrevious(); // Should wrap to index 1 (last)

        Assert.Equal(1, newIndex);
        Assert.Equal(1, manager.CurrentIndex);
    }

    [Fact]
    public void NavigateToPrevious_NoMatches_ReturnsMinusOne()
    {
        var manager = new SearchManager();
        manager.FindMatches("hello", "foo");

        int newIndex = manager.NavigateToPrevious();

        Assert.Equal(-1, newIndex);
    }

    [Fact]
    public void Clear_RemovesAllMatches()
    {
        var manager = new SearchManager();
        manager.FindMatches("test test", "test");

        manager.Clear();

        Assert.Equal(0, manager.MatchCount);
        Assert.Equal(-1, manager.CurrentIndex);
        Assert.Equal("", manager.SearchTerm);
        Assert.False(manager.HasMatches);
    }

    [Fact]
    public void FindMatches_ClearsExistingMatches()
    {
        var manager = new SearchManager();
        manager.FindMatches("test test", "test");

        manager.FindMatches("new search", "new");

        Assert.Equal(1, manager.MatchCount);
        Assert.Equal(0, manager.CurrentIndex);
        Assert.Equal("new", manager.SearchTerm);
    }

    [Fact]
    public void NavigateToNext_MultipleCallsWrapCorrectly()
    {
        var manager = new SearchManager();
        manager.FindMatches("a b a b a", "a"); // 3 matches

        manager.NavigateToNext(); // Index 1
        manager.NavigateToNext(); // Index 2
        int finalIndex = manager.NavigateToNext(); // Wrap to 0

        Assert.Equal(0, finalIndex);
    }

    [Fact]
    public void NavigateToPrevious_MultipleCallsWrapCorrectly()
    {
        var manager = new SearchManager();
        manager.FindMatches("a b a b a", "a"); // 3 matches at 0, 1, 2

        manager.NavigateToPrevious(); // Wrap to index 2
        manager.NavigateToPrevious(); // Index 1
        int finalIndex = manager.NavigateToPrevious(); // Index 0

        Assert.Equal(0, finalIndex);
    }

    [Fact]
    public void GetCurrentMatch_AfterNavigation_ReturnsCorrectMatch()
    {
        var manager = new SearchManager();
        manager.FindMatches("one two one", "one");
        manager.NavigateToNext();

        var currentMatch = manager.GetCurrentMatch();

        Assert.NotNull(currentMatch);
        Assert.Equal((8, 11), currentMatch); // Second "one"
    }

    [Fact]
    public void FindMatches_WithNewlineAndSpecialChars_WorksCorrectly()
    {
        var manager = new SearchManager();
        string text = "line1\nline2\nline3";

        int matchCount = manager.FindMatches(text, "line");

        Assert.Equal(3, matchCount);
    }

    [Fact]
    public void FindMatches_LargeText_HandlesEfficiently()
    {
        var manager = new SearchManager();
        string text = string.Join(" ", System.Linq.Enumerable.Repeat("word", 1000));

        int matchCount = manager.FindMatches(text, "word");

        Assert.Equal(1000, matchCount);
    }
}
