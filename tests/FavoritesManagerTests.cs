using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GMan.Tests;

public class FavoritesManagerTests : IDisposable
{
    private readonly string testSettingsDir;
    private readonly string testSettingsFile;

    public FavoritesManagerTests()
    {
        // Create a temporary settings directory for testing
        testSettingsDir = Path.Combine(Path.GetTempPath(), $"gman_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testSettingsDir);
        testSettingsFile = Path.Combine(testSettingsDir, "settings.conf");
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(testSettingsDir))
        {
            try
            {
                Directory.Delete(testSettingsDir, true);
            }
            catch { }
        }
    }

    private Settings CreateTestSettings()
    {
        // Create a settings instance with test favorites
        return new Settings
        {
            Favorites = new List<string>()
        };
    }

    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new FavoritesManager(null!));
    }

    [Fact]
    public void IsFavorite_WithEmpty_ReturnsFalse()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        Assert.False(manager.IsFavorite("ls"));
    }

    [Fact]
    public void IsFavorite_WithNull_ReturnsFalse()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        Assert.False(manager.IsFavorite(null!));
    }

    [Fact]
    public void IsFavorite_WithWhitespace_ReturnsFalse()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        Assert.False(manager.IsFavorite("  "));
    }

    [Fact]
    public void IsFavorite_WithExisting_ReturnsTrue()
    {
        var settings = CreateTestSettings();
        settings.Favorites.Add("ls");
        var manager = new FavoritesManager(settings);

        Assert.True(manager.IsFavorite("ls"));
    }

    [Fact]
    public void IsFavorite_IsCaseInsensitive()
    {
        var settings = CreateTestSettings();
        settings.Favorites.Add("ls");
        var manager = new FavoritesManager(settings);

        Assert.True(manager.IsFavorite("LS"));
        Assert.True(manager.IsFavorite("Ls"));
        Assert.True(manager.IsFavorite("ls"));
    }

    [Fact]
    public void Add_NewProgram_ReturnsTrue()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        bool added = manager.Add("grep");

        Assert.True(added);
        Assert.True(manager.IsFavorite("grep"));
        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void Add_Duplicate_ReturnsFalse()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        manager.Add("grep");
        bool addedAgain = manager.Add("grep");

        Assert.False(addedAgain);
        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void Add_DuplicateDifferentCase_ReturnsFalse()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        manager.Add("grep");
        bool addedAgain = manager.Add("GREP");

        Assert.False(addedAgain);
        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void Add_Null_ReturnsFalse()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        bool added = manager.Add(null!);

        Assert.False(added);
        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void Add_Whitespace_ReturnsFalse()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        bool added = manager.Add("  ");

        Assert.False(added);
        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void Add_MultiplePrograms_AllAdded()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        manager.Add("ls");
        manager.Add("grep");
        manager.Add("find");

        Assert.Equal(3, manager.Count);
        Assert.True(manager.IsFavorite("ls"));
        Assert.True(manager.IsFavorite("grep"));
        Assert.True(manager.IsFavorite("find"));
    }

    [Fact]
    public void Remove_ExistingProgram_ReturnsTrue()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);
        manager.Add("grep");

        bool removed = manager.Remove("grep");

        Assert.True(removed);
        Assert.False(manager.IsFavorite("grep"));
        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void Remove_NonExistingProgram_ReturnsFalse()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        bool removed = manager.Remove("grep");

        Assert.False(removed);
    }

    [Fact]
    public void Remove_IsCaseInsensitive()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);
        manager.Add("ls");

        bool removed = manager.Remove("LS");

        Assert.True(removed);
        Assert.False(manager.IsFavorite("ls"));
    }

    [Fact]
    public void Remove_Null_ReturnsFalse()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);
        manager.Add("ls");

        bool removed = manager.Remove(null!);

        Assert.False(removed);
        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void Remove_Whitespace_ReturnsFalse()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);
        manager.Add("ls");

        bool removed = manager.Remove("  ");

        Assert.False(removed);
        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void GetAll_Empty_ReturnsEmptyList()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        var all = manager.GetAll();

        Assert.Empty(all);
    }

    [Fact]
    public void GetAll_WithFavorites_ReturnsAllFavorites()
    {
        var settings = CreateTestSettings();
        settings.Favorites.AddRange(new[] { "ls", "grep", "find" });
        var manager = new FavoritesManager(settings);

        var all = manager.GetAll();

        Assert.Equal(3, all.Count);
        Assert.Contains("ls", all);
        Assert.Contains("grep", all);
        Assert.Contains("find", all);
    }

    [Fact]
    public void GetAll_ReturnsCopy_NotReference()
    {
        var settings = CreateTestSettings();
        settings.Favorites.Add("ls");
        var manager = new FavoritesManager(settings);

        var all = manager.GetAll();
        all.Add("grep");

        // Original settings should not be affected
        Assert.Equal(1, manager.Count);
        Assert.False(manager.IsFavorite("grep"));
    }

    [Fact]
    public void GetSorted_Empty_ReturnsEmptyList()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        var sorted = manager.GetSorted();

        Assert.Empty(sorted);
    }

    [Fact]
    public void GetSorted_WithFavorites_ReturnsSortedList()
    {
        var settings = CreateTestSettings();
        settings.Favorites.AddRange(new[] { "zsh", "bash", "ls", "find" });
        var manager = new FavoritesManager(settings);

        var sorted = manager.GetSorted();

        Assert.Equal(new[] { "bash", "find", "ls", "zsh" }, sorted.ToArray());
    }

    [Fact]
    public void GetSorted_IsCaseInsensitive()
    {
        var settings = CreateTestSettings();
        settings.Favorites.AddRange(new[] { "Zsh", "bash", "LS", "Find" });
        var manager = new FavoritesManager(settings);

        var sorted = manager.GetSorted();

        Assert.Equal(new[] { "bash", "Find", "LS", "Zsh" }, sorted.ToArray());
    }

    [Fact]
    public void Count_Empty_ReturnsZero()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void Count_WithFavorites_ReturnsCorrectCount()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        manager.Add("ls");
        manager.Add("grep");
        manager.Add("find");

        Assert.Equal(3, manager.Count);
    }

    [Fact]
    public void Count_AfterRemoval_ReturnsUpdatedCount()
    {
        var settings = CreateTestSettings();
        var manager = new FavoritesManager(settings);

        manager.Add("ls");
        manager.Add("grep");
        manager.Add("find");
        manager.Remove("grep");

        Assert.Equal(2, manager.Count);
    }
}
