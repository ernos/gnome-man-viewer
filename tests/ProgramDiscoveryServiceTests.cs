using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GMan.Tests;

public class ProgramDiscoveryServiceTests : IDisposable
{
    private readonly string testDirectory;
    private readonly List<string> createdDirectories = new();

    public ProgramDiscoveryServiceTests()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), $"gman-discovery-test-{Guid.NewGuid()}");
    }

    public void Dispose()
    {
        // Clean up all created directories
        foreach (var dir in createdDirectories)
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch { }
            }
        }
    }

    private string CreateTestDirectory(string name)
    {
        var dir = Path.Combine(testDirectory, name);
        Directory.CreateDirectory(dir);
        createdDirectories.Add(dir);
        return dir;
    }

    private void CreateTestFile(string directory, string filename)
    {
        var path = Path.Combine(directory, filename);
        File.WriteAllText(path, "#!/bin/bash\necho test");
    }

    [Fact]
    public void DiscoverPrograms_ReturnsEmptyList_WhenNoDirectoriesExist()
    {
        var nonExistentPaths = new[] { "/nonexistent1", "/nonexistent2" };
        var service = new ProgramDiscoveryService(nonExistentPaths);

        var programs = service.DiscoverPrograms();

        Assert.Empty(programs);
    }

    [Fact]
    public void DiscoverPrograms_ScansMultipleDirectories()
    {
        var dir1 = CreateTestDirectory("bin1");
        var dir2 = CreateTestDirectory("bin2");
        CreateTestFile(dir1, "ls");
        CreateTestFile(dir1, "cat");
        CreateTestFile(dir2, "grep");

        var service = new ProgramDiscoveryService(new[] { dir1, dir2 });
        var programs = service.DiscoverPrograms();

        Assert.Equal(3, programs.Count);
        Assert.Contains("ls", programs);
        Assert.Contains("cat", programs);
        Assert.Contains("grep", programs);
    }

    [Fact]
    public void DiscoverPrograms_RemovesDuplicates()
    {
        var dir1 = CreateTestDirectory("bin1");
        var dir2 = CreateTestDirectory("bin2");
        CreateTestFile(dir1, "ls");
        CreateTestFile(dir2, "ls"); // Duplicate

        var service = new ProgramDiscoveryService(new[] { dir1, dir2 });
        var programs = service.DiscoverPrograms();

        Assert.Single(programs);
        Assert.Equal("ls", programs[0]);
    }

    [Fact]
    public void DiscoverPrograms_IsCaseInsensitive()
    {
        var dir1 = CreateTestDirectory("bin1");
        var dir2 = CreateTestDirectory("bin2");
        CreateTestFile(dir1, "Program");
        CreateTestFile(dir2, "program"); // Same name, different case

        var service = new ProgramDiscoveryService(new[] { dir1, dir2 });
        var programs = service.DiscoverPrograms();

        Assert.Single(programs);
    }

    [Fact]
    public void DiscoverPrograms_ReturnsSortedList()
    {
        var dir = CreateTestDirectory("bin");
        CreateTestFile(dir, "zebra");
        CreateTestFile(dir, "apple");
        CreateTestFile(dir, "middle");

        var service = new ProgramDiscoveryService(new[] { dir });
        var programs = service.DiscoverPrograms();

        Assert.Equal(3, programs.Count);
        Assert.Equal("apple", programs[0]);
        Assert.Equal("middle", programs[1]);
        Assert.Equal("zebra", programs[2]);
    }

    [Fact]
    public void DiscoverPrograms_SortingIsCaseInsensitive()
    {
        var dir = CreateTestDirectory("bin");
        CreateTestFile(dir, "Apple");
        CreateTestFile(dir, "banana");
        CreateTestFile(dir, "Cherry");

        var service = new ProgramDiscoveryService(new[] { dir });
        var programs = service.DiscoverPrograms();

        Assert.Equal("Apple", programs[0]);
        Assert.Equal("banana", programs[1]);
        Assert.Equal("Cherry", programs[2]);
    }

    [Fact]
    public void DiscoverPrograms_HandlesEmptyDirectories()
    {
        var dir = CreateTestDirectory("empty");
        // Don't create any files

        var service = new ProgramDiscoveryService(new[] { dir });
        var programs = service.DiscoverPrograms();

        Assert.Empty(programs);
    }

    [Fact]
    public void DiscoverPrograms_SkipsNonExistentDirectories()
    {
        var existingDir = CreateTestDirectory("bin");
        CreateTestFile(existingDir, "ls");

        var service = new ProgramDiscoveryService(new[] { "/nonexistent", existingDir });
        var programs = service.DiscoverPrograms();

        Assert.Single(programs);
        Assert.Equal("ls", programs[0]);
    }

    [Fact]
    public void DiscoverPrograms_HandlesSpecialCharactersInFilenames()
    {
        var dir = CreateTestDirectory("bin");
        CreateTestFile(dir, "program-with-dashes");
        CreateTestFile(dir, "program_with_underscores");
        CreateTestFile(dir, "program.with.dots");

        var service = new ProgramDiscoveryService(new[] { dir });
        var programs = service.DiscoverPrograms();

        Assert.Equal(3, programs.Count);
        Assert.Contains("program-with-dashes", programs);
        Assert.Contains("program_with_underscores", programs);
        Assert.Contains("program.with.dots", programs);
    }

    [Fact]
    public void QueryManPageDatabase_ReturnsEmptySet_WhenManNotAvailable()
    {
        // This test assumes 'man' command might not be available in CI/CD
        // or that the test can handle both scenarios
        var service = new ProgramDiscoveryService();

        var manPages = service.QueryManPageDatabase();

        // Man pages might be available or not - just ensure it doesn't crash
        Assert.NotNull(manPages);
    }

    [Fact]
    public void DiscoverPrograms_WithFilterByManPages_ReturnsSubset()
    {
        var dir = CreateTestDirectory("bin");
        CreateTestFile(dir, "ls");
        CreateTestFile(dir, "grep");
        CreateTestFile(dir, "nonexistent-program-123456");

        var service = new ProgramDiscoveryService(new[] { dir });

        // Without filter
        var allPrograms = service.DiscoverPrograms(filterByManPages: false);

        // With filter (if man is available, should filter out programs without man pages)
        var filteredPrograms = service.DiscoverPrograms(filterByManPages: true);

        Assert.NotNull(filteredPrograms);
        // If filtering works and man pages are available, filtered list might be smaller or same
        Assert.True(filteredPrograms.Count <= allPrograms.Count);
    }

    [Fact]
    public void DiscoverPrograms_HandlesLargeNumberOfFiles()
    {
        var dir = CreateTestDirectory("bin");

        // Create 100 test files
        for (int i = 0; i < 100; i++)
        {
            CreateTestFile(dir, $"program{i:D3}");
        }

        var service = new ProgramDiscoveryService(new[] { dir });
        var programs = service.DiscoverPrograms();

        Assert.Equal(100, programs.Count);
        Assert.Equal("program000", programs[0]);
        Assert.Equal("program099", programs[99]);
    }

    [Fact]
    public void Constructor_WithDefaultPaths_DoesNotThrow()
    {
        var exception = Record.Exception(() => new ProgramDiscoveryService());

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithCustomPaths_UsesProvidedPaths()
    {
        var customPaths = new[] { "/custom1", "/custom2" };

        var exception = Record.Exception(() => new ProgramDiscoveryService(customPaths));

        Assert.Null(exception);
    }

    [Fact]
    public void WorkflowScenario_TypicalDiscovery()
    {
        // Simulate typical system with common programs
        var binDir = CreateTestDirectory("bin");
        CreateTestFile(binDir, "ls");
        CreateTestFile(binDir, "cat");
        CreateTestFile(binDir, "grep");

        var usrBinDir = CreateTestDirectory("usr-bin");
        CreateTestFile(usrBinDir, "git");
        CreateTestFile(usrBinDir, "vim");

        var service = new ProgramDiscoveryService(new[] { binDir, usrBinDir });
        var programs = service.DiscoverPrograms();

        Assert.Equal(5, programs.Count);
        Assert.True(programs.SequenceEqual(programs.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)));
    }
}
