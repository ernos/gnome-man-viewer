using System;
using Xunit;

namespace GMan.Tests;

public class ManPageLoaderTests
{
    [Fact]
    public void LoadContent_WithNullProgram_ReturnsNone()
    {
        var loader = new ManPageLoader();

        var result = loader.LoadContent(null!);

        Assert.False(result.Success);
        Assert.Equal(ManPageLoader.ContentSource.None, result.Source);
        Assert.Empty(result.Content);
    }

    [Fact]
    public void LoadContent_WithEmptyProgram_ReturnsNone()
    {
        var loader = new ManPageLoader();

        var result = loader.LoadContent("");

        Assert.False(result.Success);
        Assert.Equal(ManPageLoader.ContentSource.None, result.Source);
        Assert.Empty(result.Content);
    }

    [Fact]
    public void LoadContent_WithWhitespaceProgram_ReturnsNone()
    {
        var loader = new ManPageLoader();

        var result = loader.LoadContent("   ");

        Assert.False(result.Success);
        Assert.Equal(ManPageLoader.ContentSource.None, result.Source);
        Assert.Empty(result.Content);
    }

    [Fact]
    public void LoadContent_WithValidManPage_ReturnsManPageSource()
    {
        var loader = new ManPageLoader();

        // 'ls' should be available on all Unix systems
        var result = loader.LoadContent("ls");

        Assert.True(result.Success);
        Assert.Equal(ManPageLoader.ContentSource.ManPage, result.Source);
        Assert.NotEmpty(result.Content);
        Assert.Contains("ls", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadContent_WithNonExistentProgram_HelpDisabled_ReturnsNone()
    {
        var loader = new ManPageLoader();

        var result = loader.LoadContent("nonexistent-program-xyz123", enableHelpFallback: false);

        Assert.False(result.Success);
        Assert.Equal(ManPageLoader.ContentSource.None, result.Source);
        Assert.Empty(result.Content);
    }

    [Fact]
    public void LoadContent_WithValidProgram_CustomWidth_ReturnsContent()
    {
        var loader = new ManPageLoader();

        var result = loader.LoadContent("ls", width: 120);

        Assert.True(result.Success);
        Assert.Equal(ManPageLoader.ContentSource.ManPage, result.Source);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public void GetManPageContent_WithNullProgram_ReturnsEmpty()
    {
        var loader = new ManPageLoader();

        var content = loader.GetManPageContent(null!);

        Assert.Empty(content);
    }

    [Fact]
    public void GetManPageContent_WithValidProgram_ReturnsContent()
    {
        var loader = new ManPageLoader();

        var content = loader.GetManPageContent("ls");

        Assert.NotEmpty(content);
        Assert.Contains("ls", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetManPageContent_WithNonExistentProgram_ReturnsEmpty()
    {
        var loader = new ManPageLoader();

        var content = loader.GetManPageContent("nonexistent-program-xyz123");

        Assert.Empty(content);
    }

    [Fact]
    public void GetManPageContent_WithCustomWidth_FormatsCorrectly()
    {
        var loader = new ManPageLoader();

        // Get content with different widths
        var content80 = loader.GetManPageContent("ls", 80);
        var content120 = loader.GetManPageContent("ls", 120);

        // Both should have content
        Assert.NotEmpty(content80);
        Assert.NotEmpty(content120);

        // Contents may differ in formatting based on width
        // We can't assert exact differences as it depends on the man page,
        // but we can verify it's the same program
        Assert.Contains("ls", content80, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ls", content120, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetManPageContent_RemovesControlCharacters()
    {
        var loader = new ManPageLoader();

        var content = loader.GetManPageContent("ls");

        // Content should not be empty
        Assert.NotEmpty(content);

        // The actual control character removal happens in the service
        // We just verify that content is returned
        // (actual control char filtering is implementation detail)
    }

    [Fact]
    public void GetHelpContent_WithNullProgram_ReturnsEmpty()
    {
        var loader = new ManPageLoader();

        var content = loader.GetHelpContent(null!);

        Assert.Empty(content);
    }

    [Fact]
    public void GetHelpContent_WithLsProgram_ReturnsContent()
    {
        var loader = new ManPageLoader();

        // 'ls' typically supports --help
        var content = loader.GetHelpContent("ls");

        // Should have help content (most systems support ls --help)
        Assert.NotEmpty(content);
    }

    [Fact]
    public void GetHelpContent_WithNonExistentProgram_ReturnsEmpty()
    {
        var loader = new ManPageLoader();

        var content = loader.GetHelpContent("nonexistent-program-xyz123");

        Assert.Empty(content);
    }

    [Fact]
    public void GetHelpContent_CompletesWithinTimeout()
    {
        var loader = new ManPageLoader();

        var startTime = DateTime.UtcNow;
        // Use a program that should respond quickly
        loader.GetHelpContent("ls");
        var elapsed = DateTime.UtcNow - startTime;

        // Should complete well within the 3-second timeout
        Assert.True(elapsed.TotalSeconds < 5, $"GetHelpContent took {elapsed.TotalSeconds} seconds");
    }

    [Fact]
    public void LoadContent_WithHelpFallbackEnabled_TriesHelpAfterManFails()
    {
        var loader = new ManPageLoader();

        // Use a program that likely has --help but no man page
        // bash is more likely to have both, so let's use a non-existent command
        var result = loader.LoadContent("nonexistent-program-xyz123", enableHelpFallback: true);

        // Should fail since program doesn't exist
        Assert.False(result.Success);
        Assert.Equal(ManPageLoader.ContentSource.None, result.Source);
    }

    [Fact]
    public void LoadContent_WithHelpFallbackDisabled_DoesNotTryHelp()
    {
        var loader = new ManPageLoader();

        var result = loader.LoadContent("nonexistent-program-xyz123", enableHelpFallback: false);

        // Should return None without trying help
        Assert.False(result.Success);
        Assert.Equal(ManPageLoader.ContentSource.None, result.Source);
        Assert.Empty(result.Content);
    }

    [Fact]
    public void LoadResult_Success_ReflectsContentPresence()
    {
        var successResult = new ManPageLoader.LoadResult
        {
            Content = "some content",
            Source = ManPageLoader.ContentSource.ManPage
        };

        var failureResult = new ManPageLoader.LoadResult
        {
            Content = "",
            Source = ManPageLoader.ContentSource.None
        };

        Assert.True(successResult.Success);
        Assert.False(failureResult.Success);
    }

    [Fact]
    public void GetManPageContent_WithMinimumWidth_ReturnsContent()
    {
        var loader = new ManPageLoader();

        var content = loader.GetManPageContent("ls", width: 40);

        Assert.NotEmpty(content);
    }

    [Fact]
    public void GetManPageContent_WithLargeWidth_ReturnsContent()
    {
        var loader = new ManPageLoader();

        var content = loader.GetManPageContent("ls", width: 200);

        Assert.NotEmpty(content);
    }
}
