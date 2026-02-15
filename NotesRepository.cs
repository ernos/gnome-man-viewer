using System;
using System.IO;

namespace GMan;

/// <summary>
/// Manages storage and retrieval of notes for programs.
/// Notes are stored in ~/.config/gman/notes/{program-name}.txt
/// Pure C# with no GTK dependencies - fully unit testable.
/// </summary>
public class NotesRepository
{
    private readonly string notesDirectory;

    /// <summary>
    /// Event fired when a notes file is created or deleted.
    /// This signals that the UI should refresh to show/hide the notes icon.
    /// </summary>
    public event EventHandler<string>? NotesStatusChanged;

    /// <summary>
    /// Creates a new NotesRepository with the default notes directory.
    /// Default: ~/.config/gman/notes
    /// </summary>
    public NotesRepository()
        : this(GetDefaultNotesDirectory())
    {
    }

    /// <summary>
    /// Creates a new NotesRepository with a custom notes directory.
    /// Used for testing with temporary directories.
    /// </summary>
    /// <param name="notesDirectory">The directory where notes files are stored</param>
    public NotesRepository(string notesDirectory)
    {
        this.notesDirectory = notesDirectory;
    }

    /// <summary>
    /// Gets the default notes directory path.
    /// </summary>
    private static string GetDefaultNotesDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "gman",
            "notes"
        );
    }

    /// <summary>
    /// Gets the full path to the notes file for a given program.
    /// </summary>
    /// <param name="programName">The name of the program</param>
    /// <returns>Full path to the notes file</returns>
    public string GetNotesPath(string programName)
    {
        return Path.Combine(notesDirectory, $"{programName}.txt");
    }

    /// <summary>
    /// Loads notes for the given program.
    /// </summary>
    /// <param name="programName">The name of the program</param>
    /// <returns>The notes content, or empty string if no notes exist</returns>
    public string Load(string programName)
    {
        try
        {
            var notesPath = GetNotesPath(programName);

            if (File.Exists(notesPath))
            {
                return File.ReadAllText(notesPath);
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading notes for {programName}: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Saves notes for the given program.
    /// If content is empty, deletes the notes file (if it exists).
    /// Creates the notes directory if it doesn't exist.
    /// </summary>
    /// <param name="programName">The name of the program</param>
    /// <param name="content">The notes content to save</param>
    public void Save(string programName, string content)
    {
        try
        {
            var notesPath = GetNotesPath(programName);
            bool hadNotesBefore = File.Exists(notesPath);

            // Only save if content is not empty
            if (!string.IsNullOrWhiteSpace(content))
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(notesDirectory))
                {
                    Directory.CreateDirectory(notesDirectory);
                }

                File.WriteAllText(notesPath, content);

                // If this is the first time notes are being saved, notify listeners
                if (!hadNotesBefore)
                {
                    NotesStatusChanged?.Invoke(this, programName);
                }
            }
            else if (hadNotesBefore)
            {
                // If content is empty but file exists, delete the file
                File.Delete(notesPath);
                NotesStatusChanged?.Invoke(this, programName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving notes for {programName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if notes exist for the given program.
    /// </summary>
    /// <param name="programName">The name of the program</param>
    /// <returns>True if a notes file exists, false otherwise</returns>
    public bool HasNotes(string programName)
    {
        return File.Exists(GetNotesPath(programName));
    }

    /// <summary>
    /// Deletes notes for the given program if they exist.
    /// </summary>
    /// <param name="programName">The name of the program</param>
    /// <returns>True if notes were deleted, false if no notes existed</returns>
    public bool Delete(string programName)
    {
        try
        {
            var notesPath = GetNotesPath(programName);
            if (File.Exists(notesPath))
            {
                File.Delete(notesPath);
                NotesStatusChanged?.Invoke(this, programName);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting notes for {programName}: {ex.Message}");
            return false;
        }
    }
}
