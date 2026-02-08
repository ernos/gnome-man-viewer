using Gtk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GMan;

public class MainWindow
{
    private readonly Window mainWindow;
    private readonly Entry searchEntry;
    private readonly TextView manPageView;
    private readonly Label statusLabel;
    private readonly TreeView programListView;
    private readonly Button aboutButton;
    private readonly ListStore programStore;
    private TextTag highlightTag;
    private List<string> allPrograms = new();
    private bool isManPageLoaded = false;
    private string? currentLoadedProgram;
    private string? lastSearchTerm;

    public MainWindow(string? autoLoadProgram = null, string? autoSearchTerm = null)
    {
        var builder = new Builder();
        builder.AddFromFile(GetUiPath());

        mainWindow = (Window)builder.GetObject("mainWindow");
        searchEntry = (Entry)builder.GetObject("searchEntry");
        programListView = (TreeView)builder.GetObject("programListView");
        manPageView = (TextView)builder.GetObject("manPageView");
        statusLabel = (Label)builder.GetObject("statusLabel");
        aboutButton = (Button)builder.GetObject("aboutButton");

        if (mainWindow == null || searchEntry == null || programListView == null || manPageView == null || statusLabel == null || aboutButton == null)
        {
            throw new InvalidOperationException("Failed to load UI from main_window.ui");
        }

        mainWindow.DeleteEvent += OnDeleteEvent;
        searchEntry.Changed += OnSearchTextChanged;
        aboutButton.Clicked += OnAboutClicked;

        programStore = new ListStore(typeof(string));
        programListView.Model = programStore;
        programListView.HeadersVisible = false;

        var column = new TreeViewColumn();
        var cellRenderer = new CellRendererText();
        column.PackStart(cellRenderer, true);
        column.AddAttribute(cellRenderer, "text", 0);
        programListView.AppendColumn(column);
        programListView.RowActivated += OnProgramSelected;
        programListView.Selection.Changed += OnProgramSelectionChanged;

        manPageView.Editable = false;
        manPageView.WrapMode = WrapMode.Word;

        // Create highlight tag for search matches
        highlightTag = new TextTag("highlight");
        highlightTag.Background = "yellow";
        manPageView.Buffer.TagTable.Add(highlightTag);

        LoadPrograms();

        // Handle CLI arguments
        if (!string.IsNullOrEmpty(autoLoadProgram))
        {
            LoadManPage(autoLoadProgram);
            if (!string.IsNullOrEmpty(autoSearchTerm))
            {
                searchEntry.Text = autoSearchTerm;
            }
        }
    }

    public void ShowAll()
    {
        mainWindow.ShowAll();
    }

    private void LoadPrograms()
    {
        var paths = new[] { "/bin", "/usr/bin", "/usr/local/bin", "/sbin", "/usr/sbin" };
        var programs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    var files = Directory.GetFiles(path);
                    foreach (var file in files)
                    {
                        programs.Add(System.IO.Path.GetFileName(file));
                    }
                }
                catch { }
            }
        }

        allPrograms = programs.ToList();
        RefreshProgramList("");
        statusLabel.Text = $"Ready - {allPrograms.Count} programs available";
    }

    private void OnSearchTextChanged(object? sender, EventArgs e)
    {
        string query = searchEntry.Text.Trim();

        if (isManPageLoaded)
        {
            // Man page is loaded: search within the page
            if (string.IsNullOrEmpty(query))
            {
                ClearSearchHighlights();
                statusLabel.Text = $"Displaying: {currentLoadedProgram}";
            }
            else
            {
                SearchInManPage(query);
            }
        }
        else
        {
            // No man page loaded: filter program list
            string lowerQuery = query.ToLower();
            RefreshProgramList(lowerQuery);
        }
    }

    private void RefreshProgramList(string filter)
    {
        programStore.Clear();

        var filtered = string.IsNullOrEmpty(filter)
            ? allPrograms
            : allPrograms.Where(p => p.ToLower().Contains(filter)).ToList();

        foreach (var program in filtered)
        {
            programStore.AppendValues(program);
        }

        statusLabel.Text = $"Found {filtered.Count} program(s)";
    }

    private void OnProgramSelected(object? sender, RowActivatedArgs args)
    {
        string pathStr = args.Path.ToString();
        if (programStore.GetIterFromString(out var iter, pathStr))
        {
            var program = programStore.GetValue(iter, 0)?.ToString();
            if (!string.IsNullOrEmpty(program))
            {
                LoadManPage(program);
            }
        }
    }

    private void OnProgramSelectionChanged(object? sender, EventArgs e)
    {
        if (programListView.Selection.GetSelected(out TreeIter iter))
        {
            var program = programStore.GetValue(iter, 0)?.ToString();
            if (!string.IsNullOrEmpty(program) && !string.Equals(program, currentLoadedProgram, StringComparison.OrdinalIgnoreCase))
            {
                LoadManPage(program);
            }
        }
    }

    private void LoadManPage(string pageName)
    {
        try
        {
            statusLabel.Text = $"Loading man page for '{pageName}'...";
            
            // Try to get man page content
            string manContent = GetManPageContent(pageName);

            if (string.IsNullOrEmpty(manContent))
            {
                // Man page not found, try --help
                string helpContent = GetHelpContent(pageName);

                if (string.IsNullOrEmpty(helpContent))
                {
                    // No help available either
                    statusLabel.Text = $"Error: No manual entry for '{pageName}' and no help available";
                    manPageView.Buffer.Text = $"No manual entry for {pageName}";
                    isManPageLoaded = false;
                    currentLoadedProgram = null;
                }
                else
                {
                    // Show help with warning banner
                    string warningBanner = $"⚠️  WARNING: No man page found for '{pageName}'\n" +
                                          "Showing output from 'program --help' instead.\n" +
                                          "────────────────────────────────────────────\n\n";
                    manPageView.Buffer.Text = warningBanner + helpContent;
                    statusLabel.Text = $"Displaying help for: {pageName}";
                    isManPageLoaded = true;
                    currentLoadedProgram = pageName;
                }
            }
            else
            {
                // Man page found
                manPageView.Buffer.Text = manContent;
                statusLabel.Text = $"Displaying: {pageName}";
                isManPageLoaded = true;
                currentLoadedProgram = pageName;
            }

            // Clear search state when loading a new page
            searchEntry.Text = "";
            lastSearchTerm = null;
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Error: {ex.Message}";
            manPageView.Buffer.Text = $"Error loading man page: {ex.Message}";
            isManPageLoaded = false;
            currentLoadedProgram = null;
        }
    }

    private string GetManPageContent(string pageName)
    {
        try
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "man";
            process.StartInfo.Arguments = pageName;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private string GetHelpContent(string programName)
    {
        try
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = programName;
            process.StartInfo.Arguments = "--help";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private void SearchInManPage(string searchTerm)
    {
        ClearSearchHighlights();
        lastSearchTerm = searchTerm;

        TextBuffer buffer = manPageView.Buffer;
        string searchLower = searchTerm.ToLower();

        int matchCount = 0;
        bool firstMatch = true;

        TextIter iter = buffer.StartIter;
        TextIter endIter = buffer.EndIter;
        
        while (iter.ForwardSearch(searchLower, 0, out TextIter matchStart, out TextIter matchEnd, endIter))
        {
            buffer.ApplyTag(highlightTag, matchStart, matchEnd);
            matchCount++;

            if (firstMatch)
            {
                manPageView.ScrollToIter(matchStart, 0.1, false, 0, 0);
                firstMatch = false;
            }

            iter = matchEnd;
        }

        if (matchCount == 0)
        {
            statusLabel.Text = $"No matches found for '{searchTerm}'";
        }
        else
        {
            statusLabel.Text = $"Found {matchCount} match(es) for '{searchTerm}' in {currentLoadedProgram}";
        }
    }

    private void ClearSearchHighlights()
    {
        TextBuffer buffer = manPageView.Buffer;
        buffer.RemoveTag(highlightTag, buffer.StartIter, buffer.EndIter);
    }

    private void OnDeleteEvent(object? sender, DeleteEventArgs args)
    {
        Application.Quit();
        args.RetVal = true;
    }

    private void OnAboutClicked(object? sender, EventArgs e)
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0";

        using var about = new AboutDialog
        {
            ProgramName = "GMan",
            Version = version,
            Comments = "GTK# man page viewer for X11/Linux",
            Website = "https://www.yourdev.net/gnome-man-viewer",
            Authors = new[] { "Maximilian Cornett <max@yourdev.net>",
                            "https://www.yourdev.net" }
        };

        about.TransientFor = mainWindow;
        about.Modal = true;
        about.Run();
    }

    private static string GetUiPath()
    {
        string baseDir = AppContext.BaseDirectory;
        string uiPath = System.IO.Path.Combine(baseDir, "ui", "main_window.ui");
        if (File.Exists(uiPath))
        {
            return uiPath;
        }

        string devPath = System.IO.Path.Combine(baseDir, "..", "..", "..", "ui", "main_window.ui");
        return System.IO.Path.GetFullPath(devPath);
    }
}
