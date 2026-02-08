using Gtk;
using System;

namespace GMan;

class Program
{
    static void Main(string[] args)
    {
        Application.Init();
        
        var (programName, searchTerm) = ParseArguments(args);
        var window = new MainWindow(programName, searchTerm);
        window.ShowAll();
        Application.Run();
    }

    /// <summary>
    /// Parse command-line arguments.
    /// Format: [program-name] [-s|--search search-term]
    /// Examples:
    ///   gman
    ///   gman ls
    ///   gman ls -s malloc
    ///   gman -s malloc (invalid, program name required for search)
    /// </summary>
    static (string? programName, string? searchTerm) ParseArguments(string[] args)
    {
        string? programName = null;
        string? searchTerm = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg == "-s" || arg == "--search")
            {
                if (i + 1 < args.Length)
                {
                    searchTerm = args[i + 1];
                    i++; // Skip next arg since we consumed it
                }
            }
            else if (!arg.StartsWith("-"))
            {
                // First non-option argument is the program name
                if (programName == null)
                {
                    programName = arg;
                }
            }
        }

        return (programName, searchTerm);
    }
}
