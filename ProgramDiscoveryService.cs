using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GMan;

/// <summary>
/// Discovers executable programs on the system by scanning directories
/// and optionally filtering by man page availability.
/// Pure C# with no GTK dependencies - fully unit testable.
/// </summary>
public class ProgramDiscoveryService
{
    private readonly string[] scanPaths;

    /// <summary>
    /// Creates a new ProgramDiscoveryService with default scan paths.
    /// Default paths: /bin, /usr/bin, /usr/local/bin, /sbin, /usr/sbin
    /// </summary>
    public ProgramDiscoveryService()
        : this(new[] { "/bin", "/usr/bin", "/usr/local/bin", "/sbin", "/usr/sbin" })
    {
    }

    /// <summary>
    /// Creates a new ProgramDiscoveryService with custom scan paths.
    /// Used for testing with temporary directories.
    /// </summary>
    /// <param name="scanPaths">Array of directory paths to scan for executables</param>
    public ProgramDiscoveryService(string[] scanPaths)
    {
        this.scanPaths = scanPaths;
    }

    /// <summary>
    /// Discovers all executable programs in the configured directories.
    /// </summary>
    /// <param name="filterByManPages">If true, only returns programs with man pages</param>
    /// <returns>Sorted list of program names (case-insensitive)</returns>
    public List<string> DiscoverPrograms(bool filterByManPages = false)
    {
        // Scan directories for executables
        var programs = ScanDirectories();

        // Filter by man pages if requested
        if (filterByManPages)
        {
            var manPages = QueryManPageDatabase();
            if (manPages.Count > 0)
            {
                // Intersect: only programs that exist in both sets
                programs = programs.Where(p => manPages.Contains(p)).ToList();
            }
            // If man -k fails (returns empty set), fall through to return all programs
        }

        return programs;
    }

    public List<string> GetExecutablePrograms()
    {
        // Scan directories for executables
        var programs = ScanDirectories();


        var manPages = QueryManPageDatabase();
        if (manPages.Count > 0)
        {
            // Intersect: only programs that does not exist in man database
            programs = programs.Where(p => !manPages.Contains(p)).ToList();
        }

        return programs;
    }

    /// <summary>
    /// Scans configured directories for executable files.
    /// </summary>
    /// <returns>Sorted list of unique program names</returns>
    private List<string> ScanDirectories()
    {
        var programs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in scanPaths)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    var files = Directory.GetFiles(path);
                    foreach (var file in files)
                    {
                        programs.Add(Path.GetFileName(file));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning directory {path}: {ex.Message}");
                }
            }
        }

        return programs.ToList();
    }

    /// <summary>
    /// Queries the system man page database using 'man -k .'.
    /// Returns an empty set if the command fails or times out.
    /// </summary>
    /// <returns>Case-insensitive set of program names with man pages</returns>
    public HashSet<string> QueryManPageDatabase()
    {
        var manPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "man",
                    Arguments = "-k .",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            Console.WriteLine("Starting man -k . query...");
            process.Start();

            // Read output with timeout
            var outputTask = Task.Run(() => process.StandardOutput.ReadToEnd());

            // Wait up to 5 seconds for man -k to complete
            if (!outputTask.Wait(5000))
            {
                Console.WriteLine("man -k . timed out after 5 seconds");
                try { process.Kill(); } catch { }
                return manPages; // Return empty set on timeout
            }

            string output = outputTask.Result;

            if (!process.HasExited)
            {
                try { process.Kill(); } catch { }
            }
            else if (process.ExitCode != 0)
            {
                Console.WriteLine($"man -k . failed with exit code {process.ExitCode}");
                return manPages; // Return empty set on failure
            }

            Console.WriteLine($"man -k . returned {output.Split('\n').Length} lines");

            // Parse output: "program_name (section) - description"
            // Extract everything before the first space and opening parenthesis
            foreach (var line in output.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var match = Regex.Match(line, @"^([^\s(]+)\s*\(");
                if (match.Success)
                {
                    manPages.Add(match.Groups[1].Value);
                }
            }

            Console.WriteLine($"Parsed {manPages.Count} unique man page names");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in QueryManPageDatabase: {ex.Message}");
            // If man -k fails, return empty set (caller will use all programs)
            return manPages;
        }

        return manPages;
    }
}
