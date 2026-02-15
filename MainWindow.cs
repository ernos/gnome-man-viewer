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
    private readonly TextView notesView;
    private readonly Label statusLabel;
    private readonly TreeView programListView;
    private readonly TreeView favoritesListView;
    private readonly CheckButton showFavoritesCheck;
    private readonly CheckButton showProgramsCheck;
    private readonly CheckButton showNotesCheck;
    private readonly Paned leftPaned;
    private readonly Paned rightPaned;
    private readonly Box favoritesBox;
    private readonly Box programsBox;
    private readonly Box notesBox;
    private readonly Box notesContainerBox;
    private readonly ScrolledWindow favoritesListScroll;
    private readonly ScrolledWindow programListScroll;
    private readonly ScrolledWindow notesScroll;
    private readonly Label favoritesLabel;
    private readonly Label programListLabel;
    private readonly Label notesLabel;
    private readonly Label manLabel;
    private readonly EventBox favoritesLabelEventBox;
    private readonly EventBox programListLabelEventBox;
    private readonly EventBox notesLabelEventBox;
    private readonly Button aboutButton;
    private readonly Button settingsButton;
    private readonly Button helpButton;
    private readonly Button nextButton;
    private readonly Button previousButton;
    private readonly Button addToFavoritesButton;
    private readonly CheckButton manNotesCheck;
    private readonly Box manNotesCheckBox;
    private readonly ListStore programStore;
    private readonly ListStore favoritesStore;
    private List<string> favorites = new();
    private Settings settings;
    private readonly ManPageFormatter formatter = new();
    private readonly NotesRepository notesRepository = new();
    private const string FAVORITE_ICON = "starred";
    private const string NOTES_ICON = "text-x-generic";
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
    private readonly TypeAheadNavigator typeAheadNavigator = new();
    private uint? typeAheadTimeoutId = null;
    private string cachedStatusMessage = "";
    private int lastPanedPosition = 200; // Store the last user-set paned position

    public MainWindow(string? autoLoadProgram = null, string? autoSearchTerm = null)
    {
        var builder = new Builder();
        builder.AddFromFile(GetUiPath());

        mainWindow = (Window)builder.GetObject("mainWindow");
        searchEntry = (Entry)builder.GetObject("searchEntry");
        programListView = (TreeView)builder.GetObject("programListView");
        favoritesListView = (TreeView)builder.GetObject("favoritesListView");
        manPageView = (TextView)builder.GetObject("manPageView");
        notesView = (TextView)builder.GetObject("notesView");
        statusLabel = (Label)builder.GetObject("statusLabel");
        aboutButton = (Button)builder.GetObject("aboutButton");
        settingsButton = (Button)builder.GetObject("settingsButton");
        helpButton = (Button)builder.GetObject("helpButton");
        nextButton = (Button)builder.GetObject("nextButton");
        previousButton = (Button)builder.GetObject("previousButton");
        addToFavoritesButton = (Button)builder.GetObject("addToFavoritesButton");
        manNotesCheck = (CheckButton)builder.GetObject("manNotesCheck");
        manNotesCheckBox = (Box)builder.GetObject("manNotesCheckBox");
        showFavoritesCheck = (CheckButton)builder.GetObject("showFavoritesCheck");
        showProgramsCheck = (CheckButton)builder.GetObject("showProgramsCheck");
        showNotesCheck = (CheckButton)builder.GetObject("showNotesCheck");
        leftPaned = (Paned)builder.GetObject("leftPaned");
        rightPaned = (Paned)builder.GetObject("rightPaned");
        favoritesBox = (Box)builder.GetObject("favoritesBox");
        programsBox = (Box)builder.GetObject("programsBox");
        notesBox = (Box)builder.GetObject("notesBox");
        notesContainerBox = (Box)builder.GetObject("notesContainerBox");
        favoritesListScroll = (ScrolledWindow)builder.GetObject("favoritesListScroll");
        programListScroll = (ScrolledWindow)builder.GetObject("programListScroll");
        notesScroll = (ScrolledWindow)builder.GetObject("notesScroll");
        favoritesLabel = (Label)builder.GetObject("favoritesLabel");
        programListLabel = (Label)builder.GetObject("programListLabel");
        notesLabel = (Label)builder.GetObject("notesLabel");
        manLabel = (Label)builder.GetObject("manLabel");
        favoritesLabelEventBox = (EventBox)builder.GetObject("favoritesLabelEventBox");
        programListLabelEventBox = (EventBox)builder.GetObject("programListLabelEventBox");
        notesLabelEventBox = (EventBox)builder.GetObject("notesLabelEventBox");

        if (mainWindow == null || searchEntry == null || programListView == null || favoritesListView == null ||
            manPageView == null || notesView == null || statusLabel == null || aboutButton == null || settingsButton == null ||
            helpButton == null || nextButton == null || previousButton == null || addToFavoritesButton == null ||
            manNotesCheck == null || manNotesCheckBox == null || showFavoritesCheck == null ||
            showProgramsCheck == null || showNotesCheck == null || leftPaned == null || rightPaned == null ||
            favoritesBox == null || programsBox == null || notesBox == null || notesContainerBox == null ||
            favoritesListScroll == null || programListScroll == null || notesScroll == null ||
            favoritesLabel == null || programListLabel == null ||
            notesLabel == null || manLabel == null || favoritesLabelEventBox == null || programListLabelEventBox == null || notesLabelEventBox == null)
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

        // Load favorites from settings
        favorites = new List<string>(settings.Favorites);

        mainWindow.DeleteEvent += OnDeleteEvent;
        searchEntry.Changed += OnSearchTextChanged;
        searchEntry.KeyPressEvent += OnSearchEntryKeyPress;
        aboutButton.Clicked += OnAboutClicked;
        settingsButton.Clicked += OnSettingsClicked;
        helpButton.Clicked += OnHelpClicked;
        nextButton.Clicked += OnNextClicked;
        previousButton.Clicked += OnPreviousClicked;
        addToFavoritesButton.Clicked += OnAddToFavoritesClicked;
        manNotesCheck.Toggled += OnManNotesCheckToggled;
        showFavoritesCheck.Toggled += OnShowFavoritesToggled;
        showProgramsCheck.Toggled += OnShowProgramsToggled;
        showNotesCheck.Toggled += OnShowNotesToggled;

        // Make labels clickable to toggle checkboxes
        favoritesLabelEventBox.ButtonPressEvent += OnFavoritesLabelClicked;
        programListLabelEventBox.ButtonPressEvent += OnProgramListLabelClicked;
        notesLabelEventBox.ButtonPressEvent += OnNotesLabelClicked;

        // Setup favorites list (single column: text only)
        favoritesStore = new ListStore(typeof(string));
        favoritesListView.Model = favoritesStore;
        favoritesListView.HeadersVisible = false;
        favoritesListView.HasTooltip = true;
        favoritesListView.QueryTooltip += OnFavoritesListQueryTooltip;

        var favoritesColumn = new TreeViewColumn();
        var favoritesCellRenderer = new CellRendererText();
        favoritesColumn.PackStart(favoritesCellRenderer, true);
        favoritesColumn.AddAttribute(favoritesCellRenderer, "text", 0);
        favoritesListView.AppendColumn(favoritesColumn);
        favoritesListView.RowActivated += OnFavoritesRowActivated;
        favoritesListView.KeyPressEvent += OnFavoritesKeyPress;
        favoritesListView.FocusInEvent += OnFavoritesListFocusIn;
        favoritesListView.Selection.Changed += OnFavoritesSelectionChangedForHint;

        // Wire up selection handler for favorites based on settings
        if (settings.UseSingleClick)
        {
            favoritesListView.Selection.Changed += OnFavoritesSelectionChanged;
        }

        // Setup programs list (three columns: favorite icon + notes icon + text)
        programStore = new ListStore(typeof(string), typeof(string), typeof(string));
        programListView.Model = programStore;
        programListView.HeadersVisible = false;
        programListView.HasTooltip = true;
        programListView.QueryTooltip += OnProgramListQueryTooltip;

        // Column 0: Favorite Icon
        var favoriteIconColumn = new TreeViewColumn();
        var favoriteIconRenderer = new CellRendererPixbuf();
        favoriteIconRenderer.StockSize = (uint)IconSize.Menu;
        favoriteIconColumn.PackStart(favoriteIconRenderer, false);
        favoriteIconColumn.AddAttribute(favoriteIconRenderer, "icon-name", 0);
        programListView.AppendColumn(favoriteIconColumn);

        // Column 1: Notes Icon
        var notesIconColumn = new TreeViewColumn();
        var notesIconRenderer = new CellRendererPixbuf();
        notesIconRenderer.StockSize = (uint)IconSize.Menu;
        notesIconColumn.PackStart(notesIconRenderer, false);
        notesIconColumn.AddAttribute(notesIconRenderer, "icon-name", 1);
        programListView.AppendColumn(notesIconColumn);

        // Column 2: Text
        var column = new TreeViewColumn();
        var cellRenderer = new CellRendererText();
        column.PackStart(cellRenderer, true);
        column.AddAttribute(cellRenderer, "text", 2);
        programListView.AppendColumn(column);
        programListView.RowActivated += OnProgramSelected;
        programListView.KeyPressEvent += OnProgramListKeyPress;
        programListView.FocusInEvent += OnProgramListFocusIn;
        programListView.Selection.Changed += OnProgramListSelectionChangedForHint;

        // Wire up selection handler based on settings
        if (settings.UseSingleClick)
        {
            programListView.Selection.Changed += OnProgramSelectionChanged;
        }

        manPageView.Editable = false;
        manPageView.WrapMode = WrapMode.Word;
        manPageView.ButtonPressEvent += OnManPageViewClicked;

        // Wire up button release handler for auto-copy feature (copy when user finishes selecting)
        manPageView.ButtonReleaseEvent += OnManPageViewButtonRelease;

        // Set monospace font for man page display
        var cssProvider = new CssProvider();
        cssProvider.LoadFromData("textview { font-family: monospace; font-size: 10pt; }");
        manPageView.StyleContext.AddProvider(cssProvider, StyleProviderPriority.Application);

        // Setup notes view
        notesView.WrapMode = WrapMode.Word;
        notesView.StyleContext.AddProvider(cssProvider, StyleProviderPriority.Application);
        notesView.Buffer.Changed += OnNotesChanged;

        // Wire up notes repository event to refresh program list when notes status changes
        notesRepository.NotesStatusChanged += (sender, programName) => RefreshProgramList("");

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

        // Create formatting tags for man pages using ManPageFormatter styles
        var tagStyles = ManPageFormatter.GetDefaultTagStyles();

        headerTag = new TextTag(ManPageFormatter.HeaderTag);
        headerTag.Foreground = tagStyles[ManPageFormatter.HeaderTag].Foreground;
        headerTag.Weight = Pango.Weight.Bold;
        headerTag.Scale = tagStyles[ManPageFormatter.HeaderTag].Scale;
        manPageView.Buffer.TagTable.Add(headerTag);

        commandTag = new TextTag(ManPageFormatter.CommandTag);
        commandTag.Foreground = tagStyles[ManPageFormatter.CommandTag].Foreground;
        commandTag.Weight = Pango.Weight.Bold;
        manPageView.Buffer.TagTable.Add(commandTag);

        optionTag = new TextTag(ManPageFormatter.OptionTag);
        optionTag.Foreground = tagStyles[ManPageFormatter.OptionTag].Foreground;
        optionTag.Weight = Pango.Weight.Bold;
        manPageView.Buffer.TagTable.Add(optionTag);

        argumentTag = new TextTag(ManPageFormatter.ArgumentTag);
        argumentTag.Foreground = tagStyles[ManPageFormatter.ArgumentTag].Foreground;
        argumentTag.Style = Pango.Style.Italic;
        manPageView.Buffer.TagTable.Add(argumentTag);

        boldTag = new TextTag(ManPageFormatter.BoldTag);
        boldTag.Weight = Pango.Weight.Bold;
        manPageView.Buffer.TagTable.Add(boldTag);

        filePathTag = new TextTag(ManPageFormatter.FilePathTag);
        filePathTag.Foreground = tagStyles[ManPageFormatter.FilePathTag].Foreground;
        filePathTag.Underline = Pango.Underline.Single;
        manPageView.Buffer.TagTable.Add(filePathTag);

        urlTag = new TextTag(ManPageFormatter.UrlTag);
        urlTag.Foreground = tagStyles[ManPageFormatter.UrlTag].Foreground;
        urlTag.Underline = Pango.Underline.Single;
        manPageView.Buffer.TagTable.Add(urlTag);

        manReferenceTag = new TextTag(ManPageFormatter.ManReferenceTag);
        manReferenceTag.Foreground = tagStyles[ManPageFormatter.ManReferenceTag].Foreground;
        manReferenceTag.Underline = Pango.Underline.Single;
        manPageView.Buffer.TagTable.Add(manReferenceTag);

        LoadPrograms();

        // Set initial notes visibility from settings (defer until window is shown)
        showNotesCheck.Active = settings.ShowNotes;
        mainWindow.Shown += (sender, args) =>
        {
            UpdateNotesVisibility();
        };

        // Add window key press handler for global shortcuts (e.g., 'n' for notes)
        mainWindow.KeyPressEvent += OnWindowKeyPress;

        // Handle CLI arguments
        if (!string.IsNullOrEmpty(autoLoadProgram))
        {
            // Special handling for --help argument
            if (autoLoadProgram.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                autoLoadProgram.Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                LoadHelp();
            }
            else
            {
                LoadManPage(autoLoadProgram);
                if (!string.IsNullOrEmpty(autoSearchTerm))
                {
                    searchEntry.Text = autoSearchTerm;
                }
            }
        }
        else
        {
            // No arguments provided - show help by default
            LoadHelp();
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
                RefreshFavoritesList();
                ApplyFavoritesPosition();
                statusLabel.Text = $"Ready - {allPrograms.Count} programs with man pages available";
                return;
            }
            // If man -k fails, fall through to show all programs
        }

        allPrograms = programs.ToList();
        RefreshProgramList("");
        RefreshFavoritesList();
        ApplyFavoritesPosition();
        statusLabel.Text = $"Ready - {allPrograms.Count} programs available";
    }

    private void CleanupFavorites()
    {
        // Keep all favorites - don't filter out subcommands or programs not in scan
        // Favorites can include man pages not discoverable through directory scan
        // (e.g., subcommands like "nvme-device-smart-scan")
    }

    private void ApplyFavoritesPosition()
    {
        // Reorder children if favorites should be at bottom
        if (!settings.FavoritesAtTop)
        {
            // In GTK Paned, child1 is top/left, child2 is bottom/right
            // We need to swap them by removing and re-adding
            var child1 = leftPaned.Child1;
            var child2 = leftPaned.Child2;

            if (child1 == favoritesBox && child2 == programsBox)
            {
                // Currently favorites at top, need to swap
                leftPaned.Remove(child1);
                leftPaned.Remove(child2);
                leftPaned.Pack1(programsBox, false, true);
                leftPaned.Pack2(favoritesBox, true, true);
            }
        }
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
            // Column 0: favorite icon (show star if favorited)
            string favoriteIcon = favorites.Contains(program, StringComparer.OrdinalIgnoreCase) ? FAVORITE_ICON : "";
            // Column 1: notes icon (show document if has notes)
            string notesIcon = HasNotes(program) ? NOTES_ICON : "";
            // Column 2: program name
            programStore.AppendValues(favoriteIcon, notesIcon, program);
        }

        // Reattach model after populating
        programListView.Model = programStore;

        cachedStatusMessage = $"Found {filtered.Count} program(s)";
        statusLabel.Text = cachedStatusMessage;
    }

    private void RefreshFavoritesList()
    {
        // Unselect all items before clearing
        favoritesListView.Selection.UnselectAll();

        // Temporarily detach model
        favoritesListView.Model = null;

        favoritesStore.Clear();

        // Show all favorites (including subcommands not in program list), sorted alphabetically
        var sortedFavorites = favorites.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var favorite in sortedFavorites)
        {
            favoritesStore.AppendValues(favorite);
        }

        // Reattach model after populating
        favoritesListView.Model = favoritesStore;
    }

    private void SaveFavorites()
    {
        settings.Favorites = new List<string>(favorites);
        settings.Save();
    }

    private void AddToFavorites(string program)
    {
        if (!favorites.Contains(program, StringComparer.OrdinalIgnoreCase))
        {
            favorites.Add(program);
            SaveFavorites();
            RefreshFavoritesList();
            RefreshProgramList(""); // Refresh to show star icon
            statusLabel.Text = $"Added '{program}' to favorites";
        }
        else
        {
            statusLabel.Text = $"'{program}' is already in favorites";
        }
    }

    private void RemoveFromFavorites(string program)
    {
        var removed = favorites.RemoveAll(f => string.Equals(f, program, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed)
        {
            SaveFavorites();
            RefreshFavoritesList();
            RefreshProgramList(""); // Refresh to remove star icon
            statusLabel.Text = $"Removed '{program}' from favorites";
        }
    }

    private void OnProgramSelected(object? sender, RowActivatedArgs args)
    {
        string pathStr = args.Path.ToString();
        if (programStore.GetIterFromString(out var iter, pathStr))
        {
            var program = programStore.GetValue(iter, 2)?.ToString(); // Column 2 is text
            if (!string.IsNullOrEmpty(program))
            {
                LoadManPage(program);
            }
        }
    }

    private void OnProgramSelectionChanged(object? sender, EventArgs e)
    {
        Console.WriteLine($"DEBUG: OnProgramSelectionChanged fired, isInTypeAhead={typeAheadNavigator.IsActive}");

        // Don't load man page if we're just navigating with type-ahead
        if (typeAheadNavigator.IsActive)
        {
            Console.WriteLine("DEBUG: Skipping LoadManPage because we're in type-ahead mode");
            return;
        }

        // This handler is only attached when single-click mode is enabled
        if (settings.UseSingleClick && programListView.Selection.GetSelected(out TreeIter iter))
        {
            var program = programStore.GetValue(iter, 2)?.ToString(); // Column 2 is text
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
            // Save current notes before switching pages
            if (isManPageLoaded && currentLoadedProgram != null && currentLoadedProgram != pageName)
            {
                SaveNotes(currentLoadedProgram);
            }

            statusLabel.Text = $"Loading man page for '{pageName}'...";

            // Calculate the appropriate width for formatting the man page
            int manWidth = CalculateTextViewCharacterWidth();

            // Try to get man page content
            string manContent = GetManPageContent(pageName, manWidth);

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
                    addToFavoritesButton.Sensitive = false;
                }
                else
                {
                    // Show help with warning banner
                    string warningBanner = $"⚠️  WARNING: No man page found for '{pageName}'\n" +
                                          "Showing output from 'program --help' instead.\n" +
                                          "────────────────────────────────────────────\n\n";
                    manPageView.Buffer.Text = warningBanner + helpContent;
                    FormatManPage(pageName);
                    manLabel.Text = $"Help for {pageName}";
                    mainWindow.Title = $"GMan - {pageName} --help";
                    statusLabel.Text = $"Displaying help for: {pageName}";
                    isManPageLoaded = true;
                    currentLoadedProgram = pageName;
                    addToFavoritesButton.Sensitive = true;
                    LoadNotes(pageName);
                }
            }
            else
            {
                // Man page found
                manPageView.Buffer.Text = manContent;
                FormatManPage(pageName);
                UpdateManPageHeader(manContent, pageName);
                statusLabel.Text = $"Displaying: {pageName}";
                isManPageLoaded = true;
                currentLoadedProgram = pageName;
                addToFavoritesButton.Sensitive = true;
                LoadNotes(pageName);
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
            addToFavoritesButton.Sensitive = false;
        }
    }

    private string GetManPageContent(string pageName, int width = 80)
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

            // Set MANWIDTH environment variable to format for the correct width
            process.StartInfo.Environment["MANWIDTH"] = width.ToString();

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

    private int CalculateTextViewCharacterWidth()
    {
        try
        {
            // Get the allocated width of the TextView
            int widthInPixels = manPageView.AllocatedWidth;

            // Account for scrollbar and padding (approximate)
            widthInPixels -= 20;

            if (widthInPixels <= 0)
            {
                return 80; // Default fallback
            }

            // Get the monospace font description that's set on the TextView
            var fontDesc = Pango.FontDescription.FromString("Monospace 10");

            // Create a Pango layout to measure character width
            var layout = manPageView.CreatePangoLayout("M");
            layout.FontDescription = fontDesc;
            layout.GetPixelSize(out int charWidth, out _);

            if (charWidth <= 0)
            {
                return 80; // Default fallback
            }

            // Calculate how many characters fit in the width
            int characterWidth = widthInPixels / charWidth;
            Console.WriteLine($"DEBUG: TextView width in pixels: {widthInPixels}, character width in pixels: {charWidth}, calculated character width: {characterWidth}");

            // Ensure reasonable bounds (minimum 40, maximum 200)
            characterWidth = Math.Max(40, Math.Min(200, characterWidth));

            return characterWidth;
        }
        catch (Exception)
        {
            // If anything goes wrong, return a safe default
            return 80;
        }
    }

    private void UpdateManPageHeader(string manContent, string pageName)
    {
        // Extract first line for label
        string firstLine = manContent.Split('\n').FirstOrDefault()?.Trim() ?? "Manual Page";
        if (!string.IsNullOrEmpty(firstLine))
        {
            manLabel.Text = firstLine;
        }
        else
        {
            manLabel.Text = "Manual Page";
        }

        // Update window title
        mainWindow.Title = $"GMan - {pageName.ToUpper()} Manual";
    }

    private void FormatManPage(string programName)
    {
        // Clear man page references
        manPageReferences.Clear();

        // Create adapter and tag lookup
        var tagLookup = new Dictionary<string, TextTag>
        {
            [ManPageFormatter.HeaderTag] = headerTag,
            [ManPageFormatter.CommandTag] = commandTag,
            [ManPageFormatter.OptionTag] = optionTag,
            [ManPageFormatter.ArgumentTag] = argumentTag,
            [ManPageFormatter.BoldTag] = boldTag,
            [ManPageFormatter.FilePathTag] = filePathTag,
            [ManPageFormatter.UrlTag] = urlTag,
            [ManPageFormatter.ManReferenceTag] = manReferenceTag
        };

        var adapter = new GtkTextBufferAdapter(manPageView.Buffer, tagLookup);

        // Use formatter to apply tags
        var result = formatter.FormatManPage(adapter, programName);

        // Store man page references for click handling
        manPageReferences = result.ManPageReferences;
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

    private void OnAddToFavoritesClicked(object? sender, EventArgs e)
    {
        if (isManPageLoaded && currentLoadedProgram != null)
        {
            AddToFavorites(currentLoadedProgram);
        }
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
                var program = programStore.GetValue(selectedIter, 2)?.ToString(); // Column 2 is text
                if (!string.IsNullOrEmpty(program))
                {
                    LoadManPage(program);
                    args.RetVal = true;
                    return;
                }
            }
        }

        // Handle '+' key to add to favorites
        if (args.Event.Key == Gdk.Key.plus || args.Event.Key == Gdk.Key.KP_Add)
        {
            if (programListView.Selection.GetSelected(out TreeIter selectedIter))
            {
                var program = programStore.GetValue(selectedIter, 2)?.ToString();
                if (!string.IsNullOrEmpty(program))
                {
                    AddToFavorites(program);
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

        Console.WriteLine($"DEBUG: Processing character. Buffer before: '{typeAheadNavigator.Buffer}'");

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

        // Append character to navigator
        typeAheadNavigator.AppendChar(typedChar);

        Console.WriteLine($"DEBUG: Buffer after append: '{typeAheadNavigator.Buffer}'");

        // Always show what user has typed so far
        statusLabel.Text = typeAheadNavigator.GetStatusMessage();
        Console.WriteLine($"DEBUG: Status label set to: '{statusLabel.Text}'");

        // IMPORTANT: Start new timeout IMMEDIATELY before any GTK operations
        // that might process the event loop and fire the old timeout
        typeAheadTimeoutId = GLib.Timeout.Add(3000, () =>
        {
            Console.WriteLine("DEBUG: Timeout fired, resetting buffer");
            // Show clear visual feedback that timeout expired
            statusLabel.Markup = typeAheadNavigator.GetTimeoutMessage();
            GLib.Timeout.Add(2000, () =>
            {
                ResetTypeAheadBuffer();
                return false;
            });
            return false; // Don't repeat
        });
        Console.WriteLine($"DEBUG: New timeout created: {typeAheadTimeoutId}");

        // Find the first program starting with the buffer in the displayed list
        // Collect items from TreeView
        var items = new List<string>();
        TreeIter iter;
        if (programStore.GetIterFirst(out iter))
        {
            do
            {
                var value = programStore.GetValue(iter, 2)?.ToString(); // Column 2 is text
                items.Add(value ?? "");
            } while (programStore.IterNext(ref iter));
        }

        // Use navigator to find match
        var matchIndex = typeAheadNavigator.FindMatch(items);
        if (matchIndex.HasValue)
        {
            // Navigate to the match
            if (programStore.GetIterFirst(out iter))
            {
                int currentIndex = 0;
                do
                {
                    if (currentIndex == matchIndex.Value)
                    {
                        TreePath path = programStore.GetPath(iter);
                        programListView.Selection.SelectPath(path);
                        // Use idle callback for scrolling to avoid immediate event processing
                        GLib.Idle.Add(() =>
                        {
                            programListView.ScrollToCell(path, null, false, 0, 0);
                            return false;
                        });
                        args.RetVal = true;
                        break;
                    }
                    currentIndex++;
                } while (programStore.IterNext(ref iter));
            }
        }
    }

    [GLib.ConnectBefore]
    private void OnFavoritesKeyPress(object? sender, KeyPressEventArgs args)
    {
        // Handle Enter key: load man page
        if (args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.KP_Enter)
        {
            if (favoritesListView.Selection.GetSelected(out TreeIter selectedIter))
            {
                var program = favoritesStore.GetValue(selectedIter, 0)?.ToString();
                if (!string.IsNullOrEmpty(program))
                {
                    LoadManPage(program);
                    args.RetVal = true;
                    return;
                }
            }
        }

        // Handle '-' key: remove from favorites
        if (args.Event.Key == Gdk.Key.minus || args.Event.Key == Gdk.Key.KP_Subtract)
        {
            if (favoritesListView.Selection.GetSelected(out TreeIter selectedIter))
            {
                var program = favoritesStore.GetValue(selectedIter, 0)?.ToString();
                if (!string.IsNullOrEmpty(program))
                {
                    RemoveFromFavorites(program);
                    args.RetVal = true;
                    return;
                }
            }
        }

        // Type-ahead navigation for favorites (same logic as programs list)
        uint keyval = args.Event.KeyValue;
        char typedChar = (char)Gdk.Keyval.ToUnicode(keyval);

        if (!char.IsLetterOrDigit(typedChar))
        {
            return;
        }

        if (typeAheadTimeoutId.HasValue)
        {
            try
            {
                GLib.Source.Remove(typeAheadTimeoutId.Value);
            }
            catch { }
            typeAheadTimeoutId = null;
        }

        typeAheadNavigator.AppendChar(typedChar);

        statusLabel.Text = typeAheadNavigator.GetStatusMessage();

        typeAheadTimeoutId = GLib.Timeout.Add(5000, () =>
        {
            // Show clear visual feedback that timeout expired
            statusLabel.Markup = typeAheadNavigator.GetTimeoutMessage();
            GLib.Timeout.Add(2000, () =>
            {
                ResetTypeAheadBuffer();
                return false;
            });
            return false;
        });

        // Collect items from TreeView
        var items = new List<string>();
        TreeIter iter;
        if (favoritesStore.GetIterFirst(out iter))
        {
            do
            {
                var value = favoritesStore.GetValue(iter, 0)?.ToString();
                items.Add(value ?? "");
            } while (favoritesStore.IterNext(ref iter));
        }

        // Use navigator to find match
        var matchIndex = typeAheadNavigator.FindMatch(items);
        if (matchIndex.HasValue)
        {
            // Navigate to the match
            if (favoritesStore.GetIterFirst(out iter))
            {
                int currentIndex = 0;
                do
                {
                    if (currentIndex == matchIndex.Value)
                    {
                        TreePath path = favoritesStore.GetPath(iter);
                        favoritesListView.Selection.SelectPath(path);
                        GLib.Idle.Add(() =>
                        {
                            favoritesListView.ScrollToCell(path, null, false, 0, 0);
                            return false;
                        });
                        args.RetVal = true;
                        break;
                    }
                    currentIndex++;
                } while (favoritesStore.IterNext(ref iter));
            }
        }
    }

    private void OnFavoritesRowActivated(object? sender, RowActivatedArgs args)
    {
        string pathStr = args.Path.ToString();
        if (favoritesStore.GetIterFromString(out var iter, pathStr))
        {
            var program = favoritesStore.GetValue(iter, 0)?.ToString();
            if (!string.IsNullOrEmpty(program))
            {
                LoadManPage(program);
            }
        }
    }

    private void OnFavoritesSelectionChanged(object? sender, EventArgs e)
    {
        // Don't load man page if we're just navigating with type-ahead
        if (typeAheadNavigator.IsActive)
        {
            return;
        }

        // This handler is only attached when single-click mode is enabled
        if (settings.UseSingleClick && favoritesListView.Selection.GetSelected(out TreeIter iter))
        {
            var program = favoritesStore.GetValue(iter, 0)?.ToString();
            if (!string.IsNullOrEmpty(program) && !string.Equals(program, currentLoadedProgram, StringComparison.OrdinalIgnoreCase))
            {
                LoadManPage(program);
            }
        }
    }

    private void OnShowFavoritesToggled(object? sender, EventArgs e)
    {
        if (showFavoritesCheck.Active)
        {
            // Show favorites list
            favoritesListScroll.Visible = true;
            favoritesBox.SetSizeRequest(-1, -1); // Reset size constraint
            // Restore previous position if programs list is also visible
            if (showProgramsCheck.Active)
            {
                leftPaned.Position = lastPanedPosition;
            }
        }
        else
        {
            // Hide favorites list
            if (showProgramsCheck.Active)
            {
                lastPanedPosition = leftPaned.Position; // Save current position
            }
            favoritesListScroll.Visible = false;
            favoritesBox.SetSizeRequest(-1, 20); // Collapse to minimal height (just header)
            leftPaned.Position = 20; // Give most space to programs list
        }
    }

    private void OnShowProgramsToggled(object? sender, EventArgs e)
    {
        if (showProgramsCheck.Active)
        {
            // Show programs list
            programListScroll.Visible = true;
            programsBox.SetSizeRequest(-1, -1); // Reset size constraint
            // Restore previous position if favorites list is also visible
            if (showFavoritesCheck.Active)
            {
                leftPaned.Position = lastPanedPosition;
            }
        }
        else
        {
            // Hide programs list
            if (showFavoritesCheck.Active)
            {
                lastPanedPosition = leftPaned.Position; // Save current position
            }
            programListScroll.Visible = false;
            programsBox.SetSizeRequest(-1, 30); // Collapse to minimal height (just header)
            // Position paned to give most space to favorites
            leftPaned.Position = leftPaned.Parent.AllocatedHeight - 30;
        }
    }

    private void OnFavoritesLabelClicked(object? sender, ButtonPressEventArgs args)
    {
        Console.WriteLine("DEBUG: Favorites label clicked");
        showFavoritesCheck.Active = !showFavoritesCheck.Active;
        args.RetVal = true;
    }

    private void OnProgramListLabelClicked(object? sender, ButtonPressEventArgs args)
    {
        Console.WriteLine("DEBUG: Programs label clicked");
        showProgramsCheck.Active = !showProgramsCheck.Active;
        args.RetVal = true;
    }

    private void OnNotesLabelClicked(object? sender, ButtonPressEventArgs args)
    {
        Console.WriteLine("DEBUG: Notes label clicked");
        showNotesCheck.Active = !showNotesCheck.Active;
        args.RetVal = true;
    }

    private void OnShowNotesToggled(object? sender, EventArgs e)
    {
        UpdateNotesVisibility();

        // Save the notes visibility preference
        settings.ShowNotes = showNotesCheck.Active;
        settings.Save();
    }

    private void UpdateNotesVisibility()
    {
        if (showNotesCheck.Active)
        {
            // Show entire notes container
            notesContainerBox.Visible = true;
            notesBox.Visible = true;
            manNotesCheckBox.Visible = false;

            // Sync the man page header checkbox
            manNotesCheck.Toggled -= OnManNotesCheckToggled;
            manNotesCheck.Active = true;
            manNotesCheck.Toggled += OnManNotesCheckToggled;

            // Use idle callback to ensure window is properly sized
            GLib.Idle.Add(() =>
            {
                int availableWidth = rightPaned.AllocatedWidth;
                if (availableWidth > 300)
                {
                    rightPaned.Position = availableWidth - 300;
                }
                return false;
            });
        }
        else
        {
            // Hide entire notes container and show checkbox in man page header
            notesContainerBox.Visible = false;
            manNotesCheckBox.Visible = true;

            // Sync the man page header checkbox
            manNotesCheck.Toggled -= OnManNotesCheckToggled;
            manNotesCheck.Active = false;
            manNotesCheck.Toggled += OnManNotesCheckToggled;

            // Use idle callback to ensure window is properly sized
            GLib.Idle.Add(() =>
            {
                // Give all space to man page
                rightPaned.Position = rightPaned.AllocatedWidth;
                return false;
            });
        }
    }

    private void OnManNotesCheckToggled(object? sender, EventArgs e)
    {
        // Sync with the main notes checkbox
        showNotesCheck.Active = manNotesCheck.Active;
    }

    private void OnNotesChanged(object? sender, EventArgs e)
    {
        // Auto-save notes when they change
        if (isManPageLoaded && currentLoadedProgram != null)
        {
            SaveNotes(currentLoadedProgram);
        }
    }

    private void OnWindowKeyPress(object? sender, KeyPressEventArgs args)
    {
        // Check for 'n' key to toggle notes
        if (args.Event.Key == Gdk.Key.n || args.Event.Key == Gdk.Key.N)
        {
            // Only toggle if no text entry has focus
            if (mainWindow.Focus != searchEntry && mainWindow.Focus != notesView)
            {
                showNotesCheck.Active = !showNotesCheck.Active;
                args.RetVal = true;
            }
        }
    }

    private void LoadNotes(string programName)
    {
        try
        {
            var content = notesRepository.Load(programName);
            notesView.Buffer.Text = content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading notes: {ex.Message}");
            notesView.Buffer.Text = "";
        }
    }

    private void SaveNotes(string programName)
    {
        try
        {
            var content = notesView.Buffer.Text;
            notesRepository.Save(programName, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving notes: {ex.Message}");
        }
    }

    private bool HasNotes(string programName)
    {
        return notesRepository.HasNotes(programName);
    }

    private void OnProgramListFocusIn(object? sender, FocusInEventArgs args)
    {
        if (programListView.Selection.GetSelected(out _))
        {
            UpdateStatusWithHint(" - Press + key to add this program to your favorites list");
        }
    }

    private void OnProgramListSelectionChangedForHint(object? sender, EventArgs e)
    {
        if (programListView.HasFocus && programListView.Selection.GetSelected(out _))
        {
            UpdateStatusWithHint(" - Press + key to add this program to your favorites list");
        }
    }

    private void OnFavoritesListFocusIn(object? sender, FocusInEventArgs args)
    {
        if (favoritesListView.Selection.GetSelected(out _))
        {
            UpdateStatusWithHint(" - Press - key to remove this program from your favorites");
        }
    }

    private void OnFavoritesSelectionChangedForHint(object? sender, EventArgs e)
    {
        if (favoritesListView.HasFocus && favoritesListView.Selection.GetSelected(out _))
        {
            UpdateStatusWithHint(" - Press - key to remove this program from your favorites");
        }
    }

    private void UpdateStatusWithHint(string hint)
    {
        var currentText = statusLabel.Text;
        // Remove any existing hints first
        if (currentText.Contains(" - Press + key"))
        {
            var index = currentText.IndexOf(" - Press + key");
            currentText = currentText.Substring(0, index);
        }
        if (currentText.Contains(" - Press - key"))
        {
            var index = currentText.IndexOf(" - Press - key");
            currentText = currentText.Substring(0, index);
        }
        statusLabel.Text = currentText + hint;
    }

    private void OnProgramListQueryTooltip(object o, QueryTooltipArgs args)
    {
        if (programListView.GetPathAtPos(args.X, args.Y, out TreePath path))
        {
            args.Tooltip.Text = "Press + key to add this program to your favorites list";
            args.RetVal = true;
        }
        else
        {
            args.RetVal = false;
        }
    }

    private void OnFavoritesListQueryTooltip(object o, QueryTooltipArgs args)
    {
        if (favoritesListView.GetPathAtPos(args.X, args.Y, out TreePath path))
        {
            args.Tooltip.Text = "Press - key to remove this program from your favorites";
            args.RetVal = true;
        }
        else
        {
            args.RetVal = false;
        }
    }

    private void ResetTypeAheadBuffer()
    {
        Console.WriteLine($"DEBUG: ResetTypeAheadBuffer called! Buffer was: '{typeAheadNavigator.Buffer}'");
        Console.WriteLine($"DEBUG: Stack trace: {Environment.StackTrace}");
        typeAheadNavigator.Reset();
        typeAheadTimeoutId = null;

        // Restore status label to cached message (avoid GTK event processing)
        statusLabel.Text = cachedStatusMessage;
        Console.WriteLine($"DEBUG: ResetTypeAheadBuffer complete. Buffer now: '{typeAheadNavigator.Buffer}'");
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

    private void OnManPageViewButtonRelease(object? sender, ButtonReleaseEventArgs args)
    {
        // Only auto-copy if the setting is enabled
        if (!settings.AutoCopySelection)
            return;

        // Only handle left mouse button release
        if (args.Event.Button != 1)
            return;

        Console.WriteLine("DEBUG: OnManPageViewButtonRelease fired, attempting to copy selection to clipboard");
        try
        {
            // Get the selected text
            TextIter start, end;
            if (manPageView.Buffer.GetSelectionBounds(out start, out end))
            {
                string selectedText = manPageView.Buffer.GetText(start, end, false);

                // Only copy non-empty selections
                if (!string.IsNullOrWhiteSpace(selectedText))
                {
                    // Copy to clipboard
                    var clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
                    clipboard.Text = selectedText;

                    string statusText = $"'{selectedText}'";
                    if (statusText.Length > 200)
                    {
                        statusText = statusText.Substring(0, 200);
                    }
                    statusText = $"Selection was copied to clipboard! - {statusText}";
                    statusLabel.Text = statusText;

                    Console.WriteLine($"DEBUG: Copied to clipboard: '{selectedText}'");
                }
            }
        }
        catch (Exception ex)
        {
            // Silently ignore clipboard errors to avoid disrupting the user
            Console.WriteLine($"Error copying to clipboard: {ex.Message}");
        }
    }

    private void LoadManPageAndSelect(string programName)
    {
        // First, try to find and select the program in the list
        bool foundInList = false;
        programStore.Foreach((model, path, iter) =>
        {
            var value = programStore.GetValue(iter, 2)?.ToString(); // Column 2 is text
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
        // Save notes before quitting
        if (isManPageLoaded && currentLoadedProgram != null)
        {
            SaveNotes(currentLoadedProgram);
        }

        Application.Quit();
        args.RetVal = true;
    }

    private void OnAboutClicked(object? sender, EventArgs e)
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.1";

        using var about = new AboutDialog
        {
            ProgramName = "GMan",
            Version = version,
            Comments = "A graphical man page viewer for Linux/X11 environments, written in C# using GTK#. Features favorites list, smart filtering, advanced search, keyboard navigation, clickable man references, nicely colored and formatted text in the manuals and command-line integration.",
            Website = "https://www.yourdev.net/gnome-man-viewer",
            Authors = new[] { "Maximilian Cornett <max@yourdev.net>",
                            "https://www.yourdev.net" }
        };

        about.TransientFor = mainWindow;
        about.Modal = true;
        about.Run();
    }

    private void OnHelpClicked(object? sender, EventArgs e)
    {
        LoadHelp();
    }

    private void LoadHelp()
    {
        try
        {
            string helpPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ui", "help.txt");
            if (!File.Exists(helpPath))
            {
                statusLabel.Text = "Error: Help file not found";
                return;
            }

            string helpText = File.ReadAllText(helpPath);

            // Clear any existing search state
            ClearSearchHighlights();
            searchEntry.Text = "";
            searchMatches.Clear();
            currentMatchIndex = -1;
            lastSearchTerm = null;
            manPageReferences.Clear();

            // Set the help text
            manPageView.Buffer.Text = helpText;

            // Format the help text like a man page
            FormatHelpText();

            isManPageLoaded = true;
            currentLoadedProgram = "GMan Help";
            statusLabel.Text = "Displaying: GMan Help";
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Error loading help: {ex.Message}";
        }
    }

    private void FormatHelpText()
    {
        // Create adapter and tag lookup
        var tagLookup = new Dictionary<string, TextTag>
        {
            [ManPageFormatter.HeaderTag] = headerTag,
            [ManPageFormatter.OptionTag] = optionTag,
            [ManPageFormatter.FilePathTag] = filePathTag,
            [ManPageFormatter.UrlTag] = urlTag
        };

        var adapter = new GtkTextBufferAdapter(manPageView.Buffer, tagLookup);

        // Use formatter to apply tags
        formatter.FormatHelpText(adapter);
    }

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        try
        {
            // Reload settings before showing dialog
            var oldUseSingleClick = settings.UseSingleClick;
            var oldEnableHelpFallback = settings.EnableHelpFallback;
            var oldFavoritesAtTop = settings.FavoritesAtTop;
            var oldAutoCopySelection = settings.AutoCopySelection;

            var (response, newEnableHelpFallback, newUseSingleClick, newFavoritesAtTop, newAutoCopySelection) = SettingsDialog.ShowDialog(mainWindow);

            if (response == ResponseType.Ok)
            {
                // Reload settings from disk
                settings = Settings.Load();

                // Update event handlers if click mode changed
                if (oldUseSingleClick != newUseSingleClick)
                {
                    if (newUseSingleClick)
                    {
                        // Enable single-click for both lists
                        programListView.Selection.Changed += OnProgramSelectionChanged;
                        favoritesListView.Selection.Changed += OnFavoritesSelectionChanged;
                    }
                    else
                    {
                        // Disable single-click for both lists
                        programListView.Selection.Changed -= OnProgramSelectionChanged;
                        favoritesListView.Selection.Changed -= OnFavoritesSelectionChanged;
                    }
                }

                // Apply favorites position if changed
                if (oldFavoritesAtTop != newFavoritesAtTop)
                {
                    // Need to re-create the paned structure to swap children
                    var child1 = leftPaned.Child1;
                    var child2 = leftPaned.Child2;

                    leftPaned.Remove(child1);
                    leftPaned.Remove(child2);

                    if (newFavoritesAtTop)
                    {
                        leftPaned.Pack1(favoritesBox, false, true);
                        leftPaned.Pack2(programsBox, true, true);
                    }
                    else
                    {
                        leftPaned.Pack1(programsBox, true, true);
                        leftPaned.Pack2(favoritesBox, false, true);
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

    /// <summary>
    /// Adapter to bridge GTK's TextBuffer to ManPageFormatter's ITextBuffer interface
    /// </summary>
    private class GtkTextBufferAdapter : ITextBuffer
    {
        private readonly TextBuffer buffer;
        private readonly Dictionary<string, TextTag> tagLookup;

        public GtkTextBufferAdapter(TextBuffer buffer, Dictionary<string, TextTag> tagLookup)
        {
            this.buffer = buffer;
            this.tagLookup = tagLookup;
        }

        public string Text => buffer.Text;

        public void ApplyTag(string tagName, int startOffset, int endOffset)
        {
            if (tagLookup.TryGetValue(tagName, out var tag))
            {
                TextIter start = buffer.GetIterAtOffset(startOffset);
                TextIter end = buffer.GetIterAtOffset(endOffset);
                buffer.ApplyTag(tag, start, end);
            }
        }
    }
}
