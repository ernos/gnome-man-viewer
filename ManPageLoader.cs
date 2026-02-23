using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GMan;

/// <summary>
/// Service for loading man page and help content from the system.
/// Handles process execution, timeouts, and content sanitization.
/// </summary>
public class ManPageLoader
{
    private const int HelpCommandTimeoutMs = 3000;

    /// <summary>
    /// Result of attempting to load content for a program.
    /// </summary>
    public class LoadResult
    {
        /// <summary>
        /// The loaded content, or empty string if loading failed.
        /// </summary>
        public string Content { get; init; } = "";

        /// <summary>
        /// The source of the content.
        /// </summary>
        public ContentSource Source { get; init; }

        /// <summary>
        /// Whether content was successfully loaded.
        /// </summary>
        public bool Success => !string.IsNullOrEmpty(Content);
    }

    /// <summary>
    /// Indicates where the content came from.
    /// </summary>
    public enum ContentSource
    {
        /// <summary>No content was loaded.</summary>
        None,
        /// <summary>Content from man page.</summary>
        ManPage,
        /// <summary>Content from program --help.</summary>
        HelpFallback
    }

    /// <summary>
    /// Attempts to load content for a program, trying man page first, then --help if enabled.
    /// </summary>
    /// <param name="programName">The program name to load content for.</param>
    /// <param name="width">The character width for formatting the man page (default: 80).</param>
    /// <param name="enableHelpFallback">Whether to try --help if man page is not found.</param>
    /// <returns>A LoadResult containing the content and its source.</returns>
    public LoadResult LoadContent(string programName, int width = 80, bool enableHelpFallback = false)
    {
        if (string.IsNullOrWhiteSpace(programName))
        {
            return new LoadResult { Source = ContentSource.None };
        }

        // Try man page first
        string manContent = GetManPageContent(programName, width);
        if (!string.IsNullOrEmpty(manContent))
        {
            return new LoadResult
            {
                Content = manContent,
                Source = ContentSource.ManPage
            };
        }

        // If man page not found and help fallback is enabled, try --help
        if (enableHelpFallback)
        {
            string helpContent = GetHelpContent(programName);
            if (!string.IsNullOrEmpty(helpContent))
            {
                return new LoadResult
                {
                    Content = helpContent,
                    Source = ContentSource.HelpFallback
                };
            }
        }

        return new LoadResult { Source = ContentSource.None };
    }

    /// <summary>
    /// Gets the man page content for a program.
    /// </summary>
    /// <param name="pageName">The name of the man page.</param>
    /// <param name="width">The character width for formatting (default: 80).</param>
    /// <returns>The man page content, or empty string if not found or error occurred.</returns>
    public string GetManPageContent(string pageName, int width = 80)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "man";
            process.StartInfo.Arguments = pageName;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // Set MANWIDTH environment variable to format for the correct width
            process.StartInfo.Environment["MANWIDTH"] = width.ToString();

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Return empty if man command failed
            if (process.ExitCode != 0)
            {
                return string.Empty;
            }

            // Remove control characters that could cause beeps or other unwanted behavior
            output = Regex.Replace(output, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

            return output;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets help content by running a program with --help flag.
    /// Includes timeout protection (3 seconds).
    /// </summary>
    /// <param name="programName">The program to run with --help.</param>
    /// <returns>The help output, or empty string if not available or timeout occurred.</returns>
    public string GetHelpContent(string programName)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = programName;
            process.StartInfo.Arguments = "--help";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;  // Prevent waiting for input
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.StandardInput.Close();  // Close stdin immediately

            // Use a task with timeout to prevent blocking
            var outputTask = Task.Run(() => process.StandardOutput.ReadToEnd());

            // Wait for either output or timeout
            if (!outputTask.Wait(HelpCommandTimeoutMs))
            {
                // Timeout - kill the process
                try { process.Kill(); } catch { }
                return string.Empty;
            }

            string output = outputTask.Result;

            // Ensure process has exited
            if (!process.HasExited)
            {
                try { process.Kill(); } catch { }
            }

            // Remove control characters
            output = Regex.Replace(output, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

            return output;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
