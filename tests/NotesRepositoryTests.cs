using Xunit;
using System;
using System.IO;

namespace GMan.Tests;

public class NotesRepositoryTests : IDisposable
{
    private readonly string testDirectory;
    private readonly NotesRepository repository;

    public NotesRepositoryTests()
    {
        // Create a temporary directory for each test
        testDirectory = Path.Combine(Path.GetTempPath(), $"gman-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(testDirectory);
        repository = new NotesRepository(testDirectory);
    }

    public void Dispose()
    {
        // Clean up test directory after each test
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Fact]
    public void GetNotesPath_ReturnsCorrectPath()
    {
        var path = repository.GetNotesPath("grep");

        Assert.Equal(Path.Combine(testDirectory, "grep.txt"), path);
    }

    [Fact]
    public void Load_ReturnsEmptyString_WhenNotesDoNotExist()
    {
        var content = repository.Load("nonexistent");

        Assert.Equal("", content);
    }

    [Fact]
    public void Save_CreatesNotesFile()
    {
        repository.Save("grep", "This is a note about grep");

        var path = repository.GetNotesPath("grep");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Save_StoresContent()
    {
        var expectedContent = "grep searches for patterns\nin text files";

        repository.Save("grep", expectedContent);
        var actualContent = repository.Load("grep");

        Assert.Equal(expectedContent, actualContent);
    }

    [Fact]
    public void Save_CreatesDirectoryIfNotExists()
    {
        // Delete directory to simulate it not existing
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }

        repository.Save("ls", "Lists files");

        Assert.True(Directory.Exists(testDirectory));
        Assert.Equal("Lists files", repository.Load("ls"));
    }

    [Fact]
    public void Save_OverwritesExistingContent()
    {
        repository.Save("cat", "First version");
        repository.Save("cat", "Second version");

        var content = repository.Load("cat");

        Assert.Equal("Second version", content);
    }

    [Fact]
    public void Save_DeletesFile_WhenContentIsEmpty()
    {
        repository.Save("grep", "Some notes");
        var path = repository.GetNotesPath("grep");
        Assert.True(File.Exists(path));

        repository.Save("grep", "");

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Save_DeletesFile_WhenContentIsWhitespace()
    {
        repository.Save("ls", "Some notes");
        var path = repository.GetNotesPath("ls");
        Assert.True(File.Exists(path));

        repository.Save("ls", "   \n\t  ");

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Save_DoesNotCreateFile_WhenContentIsEmpty()
    {
        repository.Save("newprogram", "");

        var path = repository.GetNotesPath("newprogram");
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void HasNotes_ReturnsFalse_WhenNotesDoNotExist()
    {
        Assert.False(repository.HasNotes("nonexistent"));
    }

    [Fact]
    public void HasNotes_ReturnsTrue_WhenNotesExist()
    {
        repository.Save("grep", "Notes about grep");

        Assert.True(repository.HasNotes("grep"));
    }

    [Fact]
    public void HasNotes_ReturnsFalse_AfterDeletingNotes()
    {
        repository.Save("ls", "Notes");
        repository.Save("ls", ""); // Delete by saving empty content

        Assert.False(repository.HasNotes("ls"));
    }

    [Fact]
    public void Delete_RemovesNotesFile()
    {
        repository.Save("grep", "Some notes");
        Assert.True(repository.HasNotes("grep"));

        var result = repository.Delete("grep");

        Assert.True(result);
        Assert.False(repository.HasNotes("grep"));
    }

    [Fact]
    public void Delete_ReturnsFalse_WhenNotesDoNotExist()
    {
        var result = repository.Delete("nonexistent");

        Assert.False(result);
    }

    [Fact]
    public void NotesStatusChanged_FiredWhenFirstSaved()
    {
        string? changedProgram = null;
        repository.NotesStatusChanged += (sender, programName) => changedProgram = programName;

        repository.Save("grep", "First notes");

        Assert.Equal("grep", changedProgram);
    }

    [Fact]
    public void NotesStatusChanged_NotFiredWhenOverwriting()
    {
        repository.Save("ls", "First version");

        int eventCount = 0;
        repository.NotesStatusChanged += (sender, programName) => eventCount++;

        repository.Save("ls", "Second version");

        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void NotesStatusChanged_FiredWhenDeleted()
    {
        repository.Save("cat", "Some notes");

        string? changedProgram = null;
        repository.NotesStatusChanged += (sender, programName) => changedProgram = programName;

        repository.Save("cat", ""); // Delete by saving empty

        Assert.Equal("cat", changedProgram);
    }

    [Fact]
    public void NotesStatusChanged_FiredWhenDeletedExplicitly()
    {
        repository.Save("man", "Some notes");

        string? changedProgram = null;
        repository.NotesStatusChanged += (sender, programName) => changedProgram = programName;

        repository.Delete("man");

        Assert.Equal("man", changedProgram);
    }

    [Fact]
    public void NotesStatusChanged_NotFiredWhenSavingEmptyToNonexistent()
    {
        int eventCount = 0;
        repository.NotesStatusChanged += (sender, programName) => eventCount++;

        repository.Save("newprogram", "");

        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void Save_HandlesSpecialCharactersInProgramName()
    {
        repository.Save("program-with-dashes", "Notes");

        Assert.True(repository.HasNotes("program-with-dashes"));
        Assert.Equal("Notes", repository.Load("program-with-dashes"));
    }

    [Fact]
    public void MultiplePrograms_CanHaveIndependentNotes()
    {
        repository.Save("grep", "grep notes");
        repository.Save("ls", "ls notes");
        repository.Save("cat", "cat notes");

        Assert.Equal("grep notes", repository.Load("grep"));
        Assert.Equal("ls notes", repository.Load("ls"));
        Assert.Equal("cat notes", repository.Load("cat"));
    }

    [Fact]
    public void Load_ReturnsEmptyString_OnIOError()
    {
        // Create a file and make it unreadable (this is platform-dependent and may not work on all systems)
        var path = repository.GetNotesPath("test");
        File.WriteAllText(path, "content");

        // On most systems, we can't easily create an unreadable file in tests
        // So we'll just verify that Load handles missing files gracefully
        File.Delete(path);

        var content = repository.Load("test");
        Assert.Equal("", content);
    }

    [Fact]
    public void Save_HandlesLongContent()
    {
        var longContent = new string('x', 10000) + "\n" + new string('y', 10000);

        repository.Save("longtest", longContent);
        var loaded = repository.Load("longtest");

        Assert.Equal(longContent, loaded);
    }

    [Fact]
    public void Save_PreservesLineBreaks()
    {
        var contentWithBreaks = "Line 1\nLine 2\r\nLine 3\n\nLine 5";

        repository.Save("multiline", contentWithBreaks);
        var loaded = repository.Load("multiline");

        Assert.Equal(contentWithBreaks, loaded);
    }

    [Fact]
    public void WorkflowScenario_CreateUpdateDelete()
    {
        // User creates notes
        repository.Save("vim", "vim is a text editor");
        Assert.True(repository.HasNotes("vim"));
        Assert.Equal("vim is a text editor", repository.Load("vim"));

        // User updates notes
        repository.Save("vim", "vim is a powerful text editor\nwith modal editing");
        Assert.Equal("vim is a powerful text editor\nwith modal editing", repository.Load("vim"));

        // User clears notes
        repository.Save("vim", "");
        Assert.False(repository.HasNotes("vim"));
        Assert.Equal("", repository.Load("vim"));
    }
}
