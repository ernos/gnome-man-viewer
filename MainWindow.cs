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
    private readonly Button settingsButton;
    private readonly Button nextButton;
    private readonly Button previousButton;
    private readonly ListStore programStore;
    private Settings settings;
    private TextTag highlightTag;
    private TextTag currentMatchTag;
    private TextTag headerTag;
    private TextTag commandTag;
    private TextTag optionTag;
    private TextTag argumentTag;
    private TextTag boldTag;
    private TextTag filePathTag;
    private TextTag urlTag;
    private TextTag manReferenceTag;
    private List<string> allPrograms = new();
    private bool isManPageLoaded = false;
    private string? currentLoadedProgram;
    private Dictionary<(int start, int end), string> manPageReferences = new();
    private string? lastSearchTerm;
    private List<(TextIter start, TextIter end)> searchMatches = new();
    private int currentMatchIndex = -1;
    private string typeAheadBuffer = "";
    private uint? typeAheadTimeoutId = null;
    private string cachedStatusMessage = "";
    private bool isInTypeAhead = false;

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
        settingsButton = (Button)builder.GetObject("settingsButton");
        nextButton = (Button)builder.GetObject("nextButton");
        previousButton = (Button)builder.GetObject("previousButton");

        if (mainWindow == null || searchEntry == null || programListView == null || manPageView == null || statusLabel == null || aboutButton == null || settingsButton == null || nextButton == null || previousButton == null)
        {
            throw new InvalidOperationException("Failed to load UI from main_window.ui");
        }

        // Set window icon
        try
        {
            string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ui", "icon_128.png");
            if (File.Exists(iconPath))
            {
                Console.WriteLine($"Loading icon from: {iconPath}");
                mainWindow.SetIconFromFile(iconPath);
                Console.WriteLine("Icon loaded successfully");
            }
            else
            {
                Console.WriteLine($"Icon not found at: {iconPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Icon loading failed: {ex.Message}");
        }

        // Load settings
        settings = Settings.Load();

        mainWindow.DeleteEvent += OnDeleteEvent;
        searchEntry.Changed += OnSearchTextChanged;
        searchEntry.KeyPressEvent += OnSearchEntryKeyPress;
        aboutButton.Clicked += OnAboutClicked;
        settingsButton.Clicked += OnSettingsClicked;
        nextButton.Clicked += OnNextClicked;
        previousButton.Clicked += OnPreviousClicked;

        programStore = new ListStore(typeof(string));
        programListView.Model = programStore;
        programListView.HeadersVisible = false;

        var column = new TreeViewColumn();
        var cellRenderer = new CellRendererText();
        column.PackStart(cellRenderer, true);
        column.AddAttribute(cellRenderer, "text", 0);
        programListView.AppendColumn(column);
        programListView.RowActivated += OnProgramSelected;
        programListView.KeyPressEvent += OnProgramListKeyPress;

        // Wire up selection handler based on settings
        if (settings.UseSingleClick)
        {
            programListView.Selection.Changed += OnProgramSelectionChanged;
        }

        manPageView.Editable = false;
        manPageView.WrapMode = WrapMode.Word;
        manPageView.ButtonPressEvent += OnManPageViewClicked;

        // Create highlight tag for search matches
        highlightTag = new TextTag("highlight");
        highlightTag.Background = "yellow";
        highlightTag.Foreground = "black";
        manPageView.Buffer.TagTable.Add(highlightTag);

        // Create highlight tag for the current/selected match
        currentMatchTag = new TextTag("currentMatch");
        currentMatchTag.Background = "orange";
        currentMatchTag.Foreground = "black";
        manPageView.Buffer.TagTable.Add(currentMatchTag);

        // Create formatting tags for man pages
        headerTag = new TextTag("header");
        headerTag.Foreground = "#2E86AB";  // Blue
        headerTag.Weight = Pango.Weight.Bold;
        headerTag.Scale = 1.3;
        manPageView.Buffer.TagTable.Add(headerTag);

        commandTag = new TextTag("command");
        commandTag.Foreground = "#A23B72";  // Purple
        commandTag.Weight = Pango.Weight.Bold;
        manPageView.Buffer.TagTable.Add(commandTag);

        optionTag = new TextTag("option");
        optionTag.Foreground = "#F18F01";  // Orange
        optionTag.Weight = Pango.Weight.Bold;
        manPageView.Buffer.TagTable.Add(optionTag);

        argumentTag = new TextTag("argument");
        argumentTag.Foreground = "#C73E1D";  // Red
        argumentTag.Style = Pango.Style.Italic;
        manPageView.Buffer.TagTable.Add(argumentTag);

        boldTag = new TextTag("bold");
        boldTag.Weight = Pango.Weight.Bold;
        manPageView.Buffer.TagTable.Add(boldTag);

        filePathTag = new TextTag("filePath");
        filePathTag.Foreground = "#06A77D";  // Teal/Green
        filePathTag.Underline = Pango.Underline.Single;
        manPageView.Buffer.TagTable.Add(filePathTag);

        urlTag = new TextTag("url");
        urlTag.Foreground = "#0077CC";  // Blue
        urlTag.Underline = Pango.Underline.Single;
        manPageView.Buffer.TagTable.Add(urlTag);

        manReferenceTag = new TextTag("manReference");
        manReferenceTag.Foreground = "#0077CC";  // Blue
        manReferenceTag.Underline = Pango.Underline.Single;
        manPageView.Buffer.TagTable.Add(manReferenceTag);

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
        // Give focus to program list if no man page was auto-loaded
        if (!isManPageLoaded)
        {
            programListView.GrabFocus();
        }
    }

    private void LoadPrograms()
    {
        //TODO Make this configurable in settin
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

        // If EnableHelpFallback is false, only show programs with man pages
        if (!settings.EnableHelpFallback)
        {
            var manPages = GetManPageNames();
            if (manPages.Count > 0)
            {
                // Intersect: only programs that exist in both sets
                var programsWithManPages = programs.Where(p => manPages.Contains(p)).ToList();
                allPrograms = programsWithManPages;
                RefreshProgramList("");
                statusLabel.Text = $"Ready - {allPrograms.Count} programs with man pages available";
                return;
            }
            // If man -k fails, fall through to show all programs
        }

        allPrograms = programs.ToList();
        RefreshProgramList("");
        statusLabel.Text = $"Ready - {allPrograms.Count} programs available";
    }

    private HashSet<string> GetManPageNames()
    {
        var manPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
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
            var outputTask = System.Threading.Tasks.Task.Run(() => process.StandardOutput.ReadToEnd());

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

                var match = System.Text.RegularExpressions.Regex.Match(line, @"^([^\s(]+)\s*\(");
                if (match.Success)
                {
                    manPages.Add(match.Groups[1].Value);
                }
            }

            Console.WriteLine($"Parsed {manPages.Count} unique man page names");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetManPageNames: {ex.Message}");
            // If man -k fails, return empty set (caller will use all programs)
            return manPages;
        }

        return manPages;
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
        // When no man page is loaded, don't filter programs from search entry
        // The program list has focus and uses keyboard navigation instead
    }

    private void RefreshProgramList(string filter)
    {
        // Unselect all items before clearing to avoid GTK object lifecycle issues
        programListView.Selection.UnselectAll();

        // Temporarily detach model to prevent GTK from tracking changes during clear
        programListView.Model = null;

        programStore.Clear();

        var filtered = string.IsNullOrEmpty(filter)
            ? allPrograms
            : allPrograms.Where(p => p.ToLower().Contains(filter)).ToList();

        foreach (var program in filtered)
        {
            programStore.AppendValues(program);
        }

        // Reattach model after populating
        programListView.Model = programStore;

        cachedStatusMessage = $"Found {filtered.Count} program(s)";
        statusLabel.Text = cachedStatusMessage;
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
        Console.WriteLine($"DEBUG: OnProgramSelectionChanged fired, isInTypeAhead={isInTypeAhead}");

        // Don't load man page if we're just navigating with type-ahead
        if (isInTypeAhead)
        {
            Console.WriteLine("DEBUG: Skipping LoadManPage because we're in type-ahead mode");
            return;
        }

        // This handler is only attached when single-click mode is enabled
        if (settings.UseSingleClick && programListView.Selection.GetSelected(out TreeIter iter))
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
                // Man page not found, check if we should try --help
                if (!settings.EnableHelpFallback)
                {
                    // Help fallback disabled
                    statusLabel.Text = $"Error: No manual entry for '{pageName}'";
                    manPageView.Buffer.Text = $"No manual entry for {pageName}";
                    isManPageLoaded = false;
                    currentLoadedProgram = null;
                    return;
                }

                // Try --help
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
                    FormatManPage(pageName);
                    statusLabel.Text = $"Displaying help for: {pageName}";
                    isManPageLoaded = true;
                    currentLoadedProgram = pageName;
                }
            }
            else
            {
                // Man page found
                manPageView.Buffer.Text = manContent;
                FormatManPage(pageName);
                statusLabel.Text = $"Displaying: {pageName}";
                isManPageLoaded = true;
                currentLoadedProgram = pageName;
            }

            // Clear search state when loading a new page
            searchEntry.Text = "";
            lastSearchTerm = null;
            searchMatches.Clear();
            currentMatchIndex = -1;
            nextButton.Sensitive = false;
            previousButton.Sensitive = false;

            // Clear type-ahead state
            if (typeAheadTimeoutId.HasValue)
            {
                GLib.Source.Remove(typeAheadTimeoutId.Value);
            }
            ResetTypeAheadBuffer();
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

            // Remove control characters that could cause beeps or other unwanted behavior
            output = System.Text.RegularExpressions.Regex.Replace(output, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

            return output;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private void FormatManPage(string programName)
    {
        TextBuffer buffer = manPageView.Buffer;
        string text = buffer.Text;
        string[] lines = text.Split('\n');

        // Clear man page references
        manPageReferences.Clear();

        int lineStart = 0;
        bool inSeeAlsoSection = false;

        foreach (string line in lines)
        {
            int lineLength = line.Length;
            TextIter start = buffer.GetIterAtOffset(lineStart);
            TextIter end = buffer.GetIterAtOffset(lineStart + lineLength);

            // Format section headers (all caps words at start of line)
            if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^[A-Z][A-Z\s]+$") && line.Trim().Length > 0)
            {
                buffer.ApplyTag(headerTag, start, end);

                // Check if we're entering the SEE ALSO section
                string trimmedLine = line.Trim();
                if (trimmedLine == "SEE ALSO")
                {
                    inSeeAlsoSection = true;
                }
                else
                {
                    inSeeAlsoSection = false;
                }
            }
            // Format command names (program name in various contexts)
            // Skip lines that look like man page headers/footers: COMMAND(8)...COMMAND(8)
            else if (line.Contains(programName) && !System.Text.RegularExpressions.Regex.IsMatch(line, @"^\S+\(\d+\).*\S+\(\d+\)\s*$"))
            {
                int index = 0;
                while ((index = line.IndexOf(programName, index)) != -1)
                {
                    // Check if this is a whole word match (word boundaries before and after)
                    bool isWordBoundaryBefore = index == 0 || char.IsWhiteSpace(line[index - 1]) || char.IsPunctuation(line[index - 1]);
                    bool isWordBoundaryAfter = (index + programName.Length >= line.Length) ||
                    char.IsWhiteSpace(line[index + programName.Length]) ||
                    char.IsPunctuation(line[index + programName.Length]);

                    if (isWordBoundaryBefore && isWordBoundaryAfter)
                    {
                        TextIter cmdStart = buffer.GetIterAtOffset(lineStart + index);
                        TextIter cmdEnd = buffer.GetIterAtOffset(lineStart + index + programName.Length);
                        buffer.ApplyTag(commandTag, cmdStart, cmdEnd);
                    }
                    index += programName.Length;
                }
            }

            // Format options and arguments using regex
            // Match options: -x, -?, -x=value, --option, --option=value, -arj, -box, etc.
            // Can be inside brackets [] or braces {}, separated by pipes |
            // Values can contain brackets/braces/pipes for syntax (e.g., --opt=[val1|val2])
            var optionMatches = System.Text.RegularExpressions.Regex.Matches(line,
            @"(?<=^|\s|\[|\{|\||,)(-[-a-zA-Z0-9?]+(?:=\[[^\]]+\]|=[^\s,\[\]]+)?|--[a-zA-Z][-a-zA-Z0-9\u2010]*(?:=\[[^\]]+\]|=[^\s,\[\]]+|\[[^\]]+\])?)(?=[\s,\[\]\{\}\|]|$)");
            // ...existing code...
            foreach (System.Text.RegularExpressions.Match match in optionMatches)
            {
                TextIter optStart = buffer.GetIterAtOffset(lineStart + match.Index);
                TextIter optEnd = buffer.GetIterAtOffset(lineStart + match.Index + match.Length);
                buffer.ApplyTag(optionTag, optStart, optEnd);
            }

            // Format argument placeholders
            // Match: <WORD>, UPPERCASE_WORDS, single lowercase letter before comma, 
            // lowercase_with_underscores_or-dashes, lowercase words after dash options with special chars
            var argMatches = System.Text.RegularExpressions.Regex.Matches(line,
            @"<[A-Z_][A-Z_0-9]*>|(?<![a-zA-Z])[A-Z][A-Z_0-9]+(?![a-zA-Z])|(?<=^|\s)[a-z](?=,\s)|(?<=^|\s)[a-z][a-z0-9]*[_\-][a-z0-9_\-:<>\[\]]*|(?<=-[a-zA-Z0-9?]+\s)[a-z][a-z0-9\-:<>\[\]]*");
            foreach (System.Text.RegularExpressions.Match match in argMatches)
            {
                TextIter argStart = buffer.GetIterAtOffset(lineStart + match.Index);
                TextIter argEnd = buffer.GetIterAtOffset(lineStart + match.Index + match.Length);
                buffer.ApplyTag(argumentTag, argStart, argEnd);
            }

            // Format URLs (http:// or https://)
            var urlMatches = System.Text.RegularExpressions.Regex.Matches(line, @"https?://[^\s<>\[\]]+");
            foreach (System.Text.RegularExpressions.Match match in urlMatches)
            {
                TextIter urlStart = buffer.GetIterAtOffset(lineStart + match.Index);
                TextIter urlEnd = buffer.GetIterAtOffset(lineStart + match.Index + match.Length);
                buffer.ApplyTag(urlTag, urlStart, urlEnd);
            }

            // Format file paths (starting with / or ~/)
            var filePathMatches = System.Text.RegularExpressions.Regex.Matches(line, @"(?:^|\s)(~?/[/\w\-\.]+)");
            foreach (System.Text.RegularExpressions.Match match in filePathMatches)
            {
                // Use Group 1 to skip the leading whitespace
                if (match.Groups.Count > 1)
                {
                    var pathGroup = match.Groups[1];
                    TextIter pathStart = buffer.GetIterAtOffset(lineStart + pathGroup.Index);
                    TextIter pathEnd = buffer.GetIterAtOffset(lineStart + pathGroup.Index + pathGroup.Length);
                    buffer.ApplyTag(filePathTag, pathStart, pathEnd);
                }
            }

            // Format man page references in SEE ALSO section (e.g., program(1), command(8))
            if (inSeeAlsoSection)
            {
                // Match pattern: word-characters followed by (number)
                // This matches: aa-stack(8), apparmor(7), aa_change_profile(3), etc.
                var manRefMatches = System.Text.RegularExpressions.Regex.Matches(line, @"([a-zA-Z0-9_\-\.]+)\(\d+\)");
                foreach (System.Text.RegularExpressions.Match match in manRefMatches)
                {
                    // Extract just the program name (without the section number)
                    string fullMatch = match.Value;  // e.g., "aa-stack(8)"
                    string progName = match.Groups[1].Value;  // e.g., "aa-stack"

                    int matchStart = lineStart + match.Index;
                    int matchEnd = matchStart + match.Length;

                    TextIter refStart = buffer.GetIterAtOffset(matchStart);
                    TextIter refEnd = buffer.GetIterAtOffset(matchEnd);
                    buffer.ApplyTag(manReferenceTag, refStart, refEnd);

                    // Store the reference for click handling
                    manPageReferences[(matchStart, matchEnd)] = progName;
                }
            }

            lineStart += lineLength + 1; // +1 for newline character
        }
    }

    private string GetHelpContent(string programName)
    {
        try
        {
            using var process = new System.Diagnostics.Process();
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
            var outputTask = System.Threading.Tasks.Task.Run(() => process.StandardOutput.ReadToEnd());

            // Wait for either output or timeout (3 seconds)
            if (!outputTask.Wait(3000))
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

            // Remove control characters that could cause beeps or other unwanted behavior
            output = System.Text.RegularExpressions.Regex.Replace(output, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

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
        searchMatches.Clear();
        currentMatchIndex = -1;

        TextBuffer buffer = manPageView.Buffer;
        string searchLower = searchTerm.ToLower();

        TextIter iter = buffer.StartIter;
        TextIter endIter = buffer.EndIter;

        while (iter.ForwardSearch(searchLower, 0, out TextIter matchStart, out TextIter matchEnd, endIter))
        {
            searchMatches.Add((matchStart, matchEnd));
            iter = matchEnd;
        }

        int matchCount = searchMatches.Count;

        if (matchCount == 0)
        {
            statusLabel.Text = $"No matches found for '{searchTerm}'";
            nextButton.Sensitive = false;
            previousButton.Sensitive = false;
        }
        else
        {
            // Highlight all matches and navigate to first
            foreach (var (start, end) in searchMatches)
            {
                buffer.ApplyTag(highlightTag, start, end);
            }
            NavigateToMatch(0);
            statusLabel.Text = $"Match 1 of {matchCount} for '{searchTerm}' in {currentLoadedProgram}";
            nextButton.Sensitive = true;
            previousButton.Sensitive = true;
        }
    }

    private void NavigateToMatch(int index)
    {
        if (searchMatches.Count == 0 || index < 0 || index >= searchMatches.Count)
            return;

        // Remove currentMatchTag from previous match if any
        if (currentMatchIndex >= 0 && currentMatchIndex < searchMatches.Count)
        {
            var (oldStart, oldEnd) = searchMatches[currentMatchIndex];
            manPageView.Buffer.RemoveTag(currentMatchTag, oldStart, oldEnd);
        }

        currentMatchIndex = index;
        var (start, end) = searchMatches[index];
        manPageView.Buffer.ApplyTag(currentMatchTag, start, end);
        manPageView.ScrollToIter(start, 0.1, false, 0, 0);

        string searchTerm = lastSearchTerm ?? "";
        statusLabel.Text = $"Match {index + 1} of {searchMatches.Count} for '{searchTerm}' in {currentLoadedProgram}";
    }

    private void OnNextClicked(object? sender, EventArgs e)
    {
        if (searchMatches.Count == 0)
            return;

        int nextIndex = (currentMatchIndex + 1) % searchMatches.Count;
        NavigateToMatch(nextIndex);
    }

    private void OnPreviousClicked(object? sender, EventArgs e)
    {
        if (searchMatches.Count == 0)
            return;

        int prevIndex = currentMatchIndex - 1;
        if (prevIndex < 0)
            prevIndex = searchMatches.Count - 1;
        NavigateToMatch(prevIndex);
    }

    [GLib.ConnectBefore]
    private void OnSearchEntryKeyPress(object? sender, KeyPressEventArgs args)
    {
        if (args.Event.Key == Gdk.Key.Return)
        {
            OnNextClicked(null, EventArgs.Empty);
            args.RetVal = true;
        }
    }

    [GLib.ConnectBefore]
    private void OnProgramListKeyPress(object? sender, KeyPressEventArgs args)
    {
        Console.WriteLine($"DEBUG: OnProgramListKeyPress fired! Key={args.Event.Key}, KeyValue={args.Event.KeyValue}");

        // Handle Enter/Return key to load selected program
        if (args.Event.Key == Gdk.Key.Return)
        {
            Console.WriteLine("DEBUG: Enter key pressed");
            // Clear type-ahead state
            ResetTypeAheadBuffer();

            if (programListView.Selection.GetSelected(out TreeIter selectedIter))
            {
                var program = programStore.GetValue(selectedIter, 0)?.ToString();
                if (!string.IsNullOrEmpty(program))
                {
                    LoadManPage(program);
                    args.RetVal = true;
                    return;
                }
            }
        }

        // Get the typed character from the key
        uint keyval = args.Event.KeyValue;
        char typedChar = (char)Gdk.Keyval.ToUnicode(keyval);

        Console.WriteLine($"DEBUG: Typed char='{typedChar}' ({(int)typedChar}), IsLetterOrDigit={char.IsLetterOrDigit(typedChar)}");

        // Only process alphabetic and numeric characters
        if (!char.IsLetterOrDigit(typedChar))
        {
            Console.WriteLine("DEBUG: Not alphanumeric, returning");
            return;
        }

        Console.WriteLine($"DEBUG: Processing character. Buffer before: '{typeAheadBuffer}'");

        // Set flag to prevent OnProgramSelectionChanged from loading man pages
        isInTypeAhead = true;

        // Cancel existing timeout if any
        if (typeAheadTimeoutId.HasValue)
        {
            Console.WriteLine($"DEBUG: Cancelling existing timeout {typeAheadTimeoutId.Value}");
            try
            {
                GLib.Source.Remove(typeAheadTimeoutId.Value);
            }
            catch
            {
                // Timeout may have already fired, ignore
            }
            typeAheadTimeoutId = null;
        }

        // Append character to buffer (limit to 5 characters)
        typeAheadBuffer += char.ToLower(typedChar);
        if (typeAheadBuffer.Length > 5)
        {
            typeAheadBuffer = typeAheadBuffer.Substring(typeAheadBuffer.Length - 5);
        }

        Console.WriteLine($"DEBUG: Buffer after append: '{typeAheadBuffer}'");

        // Always show what user has typed so far
        statusLabel.Text = $"Type-ahead: {typeAheadBuffer}";
        Console.WriteLine($"DEBUG: Status label set to: '{statusLabel.Text}'");

        // IMPORTANT: Start new timeout IMMEDIATELY before any GTK operations
        // that might process the event loop and fire the old timeout
        typeAheadTimeoutId = GLib.Timeout.Add(1000, () =>
        {
            Console.WriteLine("DEBUG: Timeout fired, resetting buffer");
            ResetTypeAheadBuffer();
            return false; // Don't repeat
        });
        Console.WriteLine($"DEBUG: New timeout created: {typeAheadTimeoutId}");

        // Find the first program starting with the buffer in the displayed list
        // Use manual iteration instead of Foreach to avoid GTK event processing
        bool found = false;
        TreeIter iter;

        if (programStore.GetIterFirst(out iter))
        {
            do
            {
                var value = programStore.GetValue(iter, 0)?.ToString();
                if (value != null && value.ToLower().StartsWith(typeAheadBuffer))
                {
                    // Found a match - select and scroll
                    TreePath path = programStore.GetPath(iter);
                    programListView.Selection.SelectPath(path);
                    // Use idle callback for scrolling to avoid immediate event processing
                    GLib.Idle.Add(() =>
                    {
                        programListView.ScrollToCell(path, null, false, 0, 0);
                        return false;
                    });
                    found = true;
                    break;
                }
            } while (programStore.IterNext(ref iter));
        }

        if (found)
        {
            args.RetVal = true;
        }
    }

    private void ResetTypeAheadBuffer()
    {
        Console.WriteLine($"DEBUG: ResetTypeAheadBuffer called! Buffer was: '{typeAheadBuffer}'");
        Console.WriteLine($"DEBUG: Stack trace: {Environment.StackTrace}");
        typeAheadBuffer = "";
        typeAheadTimeoutId = null;
        isInTypeAhead = false;  // Clear the flag

        // Restore status label to cached message (avoid GTK event processing)
        statusLabel.Text = cachedStatusMessage;
        Console.WriteLine($"DEBUG: ResetTypeAheadBuffer complete. Buffer now: '{typeAheadBuffer}'");
    }

    private void ClearSearchHighlights()
    {
        TextBuffer buffer = manPageView.Buffer;
        buffer.RemoveTag(highlightTag, buffer.StartIter, buffer.EndIter);
        buffer.RemoveTag(currentMatchTag, buffer.StartIter, buffer.EndIter);
    }

    private void OnManPageViewClicked(object? sender, ButtonPressEventArgs args)
    {
        // Check if this is a left-click
        if (args.Event.Button != 1)
        {
            return;
        }

        // Get the clicked position
        int x = (int)args.Event.X;
        int y = (int)args.Event.Y;

        manPageView.WindowToBufferCoords(Gtk.TextWindowType.Widget, x, y, out int bufferX, out int bufferY);
        manPageView.GetIterAtLocation(out TextIter clickIter, bufferX, bufferY);
        int clickOffset = clickIter.Offset;

        // Check if the click is on a man page reference
        foreach (var kvp in manPageReferences)
        {
            var (startOffset, endOffset) = kvp.Key;
            string programName = kvp.Value;

            if (clickOffset >= startOffset && clickOffset < endOffset)
            {
                // Clicked on a man page reference - load that page
                LoadManPageAndSelect(programName);
                args.RetVal = true;
                return;
            }
        }

        // When user clicks on the man page view (not on a reference), give focus to search entry
        // so they can immediately start searching
        if (isManPageLoaded)
        {
            searchEntry.GrabFocus();
        }
    }

    private void LoadManPageAndSelect(string programName)
    {
        // First, try to find and select the program in the list
        bool foundInList = false;
        programStore.Foreach((model, path, iter) =>
        {
            var value = programStore.GetValue(iter, 0)?.ToString();
            if (string.Equals(value, programName, StringComparison.OrdinalIgnoreCase))
            {
                programListView.Selection.SelectPath(path);
                programListView.ScrollToCell(path, null, false, 0, 0);
                foundInList = true;
                return true; // Stop iteration
            }
            return false; // Continue iteration
        });

        // Load the man page (even if not found in list, it might still have a man page)
        LoadManPage(programName);

        // If not found in the filtered list, it might be filtered out
        if (!foundInList)
        {
            statusLabel.Text = $"Displaying: {programName} (not in executable list)";
        }
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

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        try
        {
            // Reload settings before showing dialog
            var oldUseSingleClick = settings.UseSingleClick;
            var oldEnableHelpFallback = settings.EnableHelpFallback;

            var (response, newEnableHelpFallback, newUseSingleClick) = SettingsDialog.ShowDialog(mainWindow);

            if (response == ResponseType.Ok)
            {
                // Reload settings from disk
                settings = Settings.Load();

                // Update event handlers if click mode changed
                if (oldUseSingleClick != newUseSingleClick)
                {
                    if (newUseSingleClick)
                    {
                        // Enable single-click
                        programListView.Selection.Changed += OnProgramSelectionChanged;
                    }
                    else
                    {
                        // Disable single-click
                        programListView.Selection.Changed -= OnProgramSelectionChanged;
                    }
                }

                // Reload program list if EnableHelpFallback changed
                if (oldEnableHelpFallback != newEnableHelpFallback)
                {
                    Console.WriteLine($"Settings changed: EnableHelpFallback from {oldEnableHelpFallback} to {newEnableHelpFallback}");
                    statusLabel.Text = "Updating program list...";

                    // Defer the reload slightly to let GTK finish cleaning up the dialog
                    GLib.Timeout.Add(50, () =>
                    {
                        LoadPrograms();
                        Console.WriteLine("Successfully reloaded programs");
                        return false; // Don't repeat
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnSettingsClicked: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            statusLabel.Text = $"Error updating settings: {ex.Message}";
        }
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
