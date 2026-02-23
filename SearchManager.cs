using System;
using System.Collections.Generic;

namespace GMan;

/// <summary>
/// Manages search functionality within text content.
/// Stores match positions and handles navigation between matches.
/// </summary>
public class SearchManager
{
    private readonly List<(int start, int end)> matches = new();
    private int currentIndex = -1;
    private string currentSearchTerm = "";

    /// <summary>
    /// Finds all matches of the search term in the text (case-insensitive).
    /// Returns the number of matches found.
    /// </summary>
    /// <param name="text">The text to search in.</param>
    /// <param name="searchTerm">The term to search for.</param>
    /// <returns>The number of matches found.</returns>
    public int FindMatches(string text, string searchTerm)
    {
        Clear();

        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchTerm))
            return 0;

        currentSearchTerm = searchTerm;
        string textLower = text.ToLower();
        string searchLower = searchTerm.ToLower();

        int searchIndex = 0;
        while (searchIndex < textLower.Length)
        {
            int foundIndex = textLower.IndexOf(searchLower, searchIndex, StringComparison.Ordinal);
            if (foundIndex < 0)
                break;

            matches.Add((foundIndex, foundIndex + searchLower.Length));
            searchIndex = foundIndex + 1; // Move past current match to find overlapping matches
        }

        // If we found matches, set current index to 0 (first match)
        if (matches.Count > 0)
        {
            currentIndex = 0;
        }

        return matches.Count;
    }

    /// <summary>
    /// Gets all match positions.
    /// </summary>
    /// <returns>A list of (start, end) tuples representing match positions.</returns>
    public List<(int start, int end)> GetMatches()
    {
        return new List<(int start, int end)>(matches);
    }

    /// <summary>
    /// Gets the current match position.
    /// </summary>
    /// <returns>The (start, end) tuple of the current match, or null if no matches.</returns>
    public (int start, int end)? GetCurrentMatch()
    {
        if (currentIndex < 0 || currentIndex >= matches.Count)
            return null;

        return matches[currentIndex];
    }

    /// <summary>
    /// Navigates to the next match (wraps around to first if at end).
    /// </summary>
    /// <returns>The new current index, or -1 if no matches.</returns>
    public int NavigateToNext()
    {
        if (matches.Count == 0)
            return -1;

        currentIndex = (currentIndex + 1) % matches.Count;
        return currentIndex;
    }

    /// <summary>
    /// Navigates to the previous match (wraps around to last if at beginning).
    /// </summary>
    /// <returns>The new current index, or -1 if no matches.</returns>
    public int NavigateToPrevious()
    {
        if (matches.Count == 0)
            return -1;

        currentIndex = (currentIndex - 1 + matches.Count) % matches.Count;
        return currentIndex;
    }

    /// <summary>
    /// Clears all search state.
    /// </summary>
    public void Clear()
    {
        matches.Clear();
        currentIndex = -1;
        currentSearchTerm = "";
    }

    /// <summary>
    /// Gets the total number of matches.
    /// </summary>
    public int MatchCount => matches.Count;

    /// <summary>
    /// Gets the current match index (0-based).
    /// </summary>
    public int CurrentIndex => currentIndex;

    /// <summary>
    /// Gets the current search term.
    /// </summary>
    public string SearchTerm => currentSearchTerm;

    /// <summary>
    /// Checks if there are any matches.
    /// </summary>
    public bool HasMatches => matches.Count > 0;
}
