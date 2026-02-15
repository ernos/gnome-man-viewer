using System;
using System.Collections.Generic;
using System.Linq;

namespace GMan;

/// <summary>
/// Handles type-ahead navigation logic for list widgets.
/// Manages a character buffer and finds matching items in lists.
/// Pure C# with no GTK dependencies - fully unit testable.
/// </summary>
public class TypeAheadNavigator
{
    private string buffer = "";
    private const int MaxBufferLength = 10;

    /// <summary>
    /// Gets the current type-ahead buffer contents.
    /// </summary>
    public string Buffer => buffer;

    /// <summary>
    /// Gets whether the buffer has any content.
    /// </summary>
    public bool IsActive => !string.IsNullOrEmpty(buffer);

    /// <summary>
    /// Appends a character to the type-ahead buffer.
    /// The buffer is automatically limited to MaxBufferLength characters.
    /// </summary>
    /// <param name="c">The character to append (will be converted to lowercase)</param>
    public void AppendChar(char c)
    {
        buffer += char.ToLower(c);
        if (buffer.Length > MaxBufferLength)
        {
            buffer = buffer.Substring(buffer.Length - MaxBufferLength);
        }
    }

    /// <summary>
    /// Resets the type-ahead buffer to empty.
    /// </summary>
    public void Reset()
    {
        buffer = "";
    }

    /// <summary>
    /// Finds the index of the first item that starts with the current buffer.
    /// </summary>
    /// <param name="items">The list of items to search</param>
    /// <returns>The zero-based index of the first match, or null if no match found</returns>
    public int? FindMatch(IEnumerable<string> items)
    {
        if (string.IsNullOrEmpty(buffer))
            return null;

        int index = 0;
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item) && item.ToLower().StartsWith(buffer))
                return index;
            index++;
        }
        return null;
    }

    /// <summary>
    /// Finds the index of the first item that starts with the current buffer.
    /// This overload works with a function that extracts the searchable text from each item.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    /// <param name="items">The list of items to search</param>
    /// <param name="textSelector">Function to extract searchable text from each item</param>
    /// <returns>The zero-based index of the first match, or null if no match found</returns>
    public int? FindMatch<T>(IEnumerable<T> items, Func<T, string?> textSelector)
    {
        if (string.IsNullOrEmpty(buffer))
            return null;

        int index = 0;
        foreach (var item in items)
        {
            var text = textSelector(item);
            if (!string.IsNullOrEmpty(text) && text.ToLower().StartsWith(buffer))
                return index;
            index++;
        }
        return null;
    }

    /// <summary>
    /// Gets a status message describing the current type-ahead state.
    /// </summary>
    /// <returns>A user-friendly message showing the current buffer</returns>
    public string GetStatusMessage()
    {
        if (string.IsNullOrEmpty(buffer))
            return "";
        return $"Type-ahead: {buffer}";
    }

    /// <summary>
    /// Gets a markup message for timeout expiration.
    /// </summary>
    /// <returns>An orange bold message indicating timeout</returns>
    public string GetTimeoutMessage()
    {
        return "<span foreground='orange' weight='bold'>⏱ Type-ahead timeout - cleared</span>";
    }
}
