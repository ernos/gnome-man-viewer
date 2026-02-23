using System;
using System.Collections.Generic;
using System.Linq;

namespace GMan;

/// <summary>
/// Manages the list of favorite programs.
/// Provides methods to add, remove, check, and retrieve favorites.
/// </summary>
public class FavoritesManager
{
    private readonly Settings settings;

    /// <summary>
    /// Creates a new FavoritesManager instance.
    /// </summary>
    /// <param name="settings">The settings instance that stores favorites.</param>
    public FavoritesManager(Settings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Checks if a program is in the favorites list.
    /// Case-insensitive comparison.
    /// </summary>
    /// <param name="program">The program name to check.</param>
    /// <returns>True if the program is a favorite, false otherwise.</returns>
    public bool IsFavorite(string program)
    {
        if (string.IsNullOrWhiteSpace(program))
            return false;

        return settings.Favorites.Contains(program, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Adds a program to the favorites list.
    /// If the program is already a favorite, does nothing.
    /// Automatically saves settings after adding.
    /// </summary>
    /// <param name="program">The program name to add.</param>
    /// <returns>True if added, false if already in favorites.</returns>
    public bool Add(string program)
    {
        if (string.IsNullOrWhiteSpace(program))
            return false;

        if (IsFavorite(program))
            return false;

        settings.Favorites.Add(program);
        settings.Save();
        return true;
    }

    /// <summary>
    /// Removes a program from the favorites list.
    /// Case-insensitive matching.
    /// Automatically saves settings after removal.
    /// </summary>
    /// <param name="program">The program name to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    public bool Remove(string program)
    {
        if (string.IsNullOrWhiteSpace(program))
            return false;

        var removed = settings.Favorites.RemoveAll(f =>
            string.Equals(f, program, StringComparison.OrdinalIgnoreCase)) > 0;

        if (removed)
        {
            settings.Save();
        }

        return removed;
    }

    /// <summary>
    /// Gets all favorites in their current order.
    /// Returns a copy of the list to prevent external modification.
    /// </summary>
    /// <returns>A list of all favorite program names.</returns>
    public List<string> GetAll()
    {
        return new List<string>(settings.Favorites);
    }

    /// <summary>
    /// Gets all favorites sorted alphabetically (case-insensitive).
    /// </summary>
    /// <returns>A sorted list of all favorite program names.</returns>
    public List<string> GetSorted()
    {
        return settings.Favorites
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Gets the number of favorites.
    /// </summary>
    public int Count => settings.Favorites.Count;
}
