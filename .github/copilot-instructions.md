# GMan - GTK# Man Page Viewer

A C# GTK# application for viewing Unix man pages in X11 environments.

## File structure
- `MainWindow.cs` - This is the main window logic
- `Program.cs` - Program entry point. 
- `SettingsDialog.cs` - Settings dialog and settings persistence
- `doc/TODO.md` - Plans for future implementations and record of bugs that needs fixing
- `doc/CHANGELOG.md` - Changelog. You need to update this whenever you make a feature impementation or a bug fix.
- `doc/README.md` - Readme file targeted at normal users.
- `doc/README-DEV.md` - Technical readme file targeted for other developers who might want to contribute
- `ui/main_window.ui` - GTK Builder UI definition file for the main window
- `tests/exFormattingTest.cs` - Unit tests for ex-based formatting

## Imporant rules to follow when:

- **User asks for a commit messags**:
  + Use present tense ("Add feature" not "Added feature")
  + Be concise but descriptive (50 characters or less for summary, more detail in body if needed)
  + Reference relevant issues or TODO items if applicable (e.g., "Fixes #123", "Related to TODO item X")
  + Use markdown formatting in the body (e.g., headers like feature implementation/bug fixes, bullet points, code snippets)
  + Post the commit message in markdown format in the chat for the user to copy as raw text (use triple backticks with "md" for syntax highlighting)

## Feature implementation guidelines

- Analyze what feature the user asks for and ask questions if there is something unclear before you begin! 
- Document your changes in `doc/CHANGELOG.md`.
- Write Unit or implementation tests when you feel it necessary.
- Update your `.github/copilot-instructions.md` with new functions.

## Tech Stack
- Language: C#
- Framework: .NET 8 LTS
- UI: GTK# (GtkSharp)
- Platform: X11/Linux

## Code Architecture

### Program.cs - Application Entry Point

**Main responsibilities:**
- Initialize GTK application with `Application.Init()`
- Set default window icon from `ui/icon_128.png`
- Parse command-line arguments
- Create and show MainWindow

**Command-line argument parsing:**
```csharp
// Format: [program-name] [-s|--search search-term]
// Examples:
//   gman              - Launch with no program selected
//   gman ls           - Launch with 'ls' man page loaded
//   gman ls -s malloc - Launch 'ls' and search for 'malloc'
static (string? programName, string? searchTerm) ParseArguments(string[] args)
```

### SettingsDialog.cs - Settings Management

**Two main classes:**

1. **SettingsDialog** - GTK dialog for user settings
   - Creates modal dialog with checkbox and radio buttons
   - `Run()` method displays dialog and saves settings on OK

2. **Settings** - Settings persistence and loading
   - Stores settings in `~/.config/gman/settings.conf`
   - Settings properties:
     - `EnableHelpFallback` (bool) - Run programs with --help when no man page exists (default: false for security)
     - `UseSingleClick` (bool) - Single-click vs double-click to load man pages (default: false)
     - `ShowNotes` (bool) - Show/hide notes panel on startup (default: false)
     - `Favorites` (List<string>) - List of favorite programs
     - `FavoritesAtTop` (bool) - Show favorites at top vs bottom (default: true)

**Settings file format:**
```
EnableHelpFallback=true
UseSingleClick=false
ShowNotes=false
Favorites=ls,grep,find
FavoritesAtTop=true
```

**Key methods:**
```csharp
Settings.Load()  // Static method to load settings from disk
settings.Save()  // Instance method to persist settings
```

**Notes storage:**
- Notes are stored separately in `~/.config/gman/notes/program-name.txt`
- Each man page has its own notes file
- Notes are auto-saved when typing or switching pages

### MainWindow.cs - Main Application Logic

**Core GTK widgets (loaded from .ui file):**
- `Window mainWindow` - Main application window
- `Entry searchEntry` - Search text entry field
- `TreeView programListView` - List of available programs
- `TreeView favoritesListView` - List of favorite programs
- `TextView manPageView` - Display area for man pages
- `TextView notesView` - Text area for taking notes
- `Label statusLabel` - Status bar at bottom
- `Button aboutButton`, `settingsButton`, `helpButton` - Header bar buttons
- `Button nextButton`, `previousButton` - Search navigation
- `CheckButton showFavoritesCheck`, `showProgramsCheck`, `showNotesCheck` - Visibility toggles
- `Paned leftPaned` - Vertical paned for favorites/programs split
- `Paned rightPaned` - Horizontal paned for man page/notes split

**State management fields:**
```csharp
private List<string> allPrograms           // All discovered executables
private ListStore programStore             // GTK model for TreeView
private Settings settings                   // Current settings
private bool isManPageLoaded               // Whether a man page is loaded
private string? currentLoadedProgram       // Currently displayed program
private string? lastSearchTerm             // Last text searched
private List<(TextIter start, TextIter end)> searchMatches  // All search matches
private int currentMatchIndex              // Current highlighted match
private Dictionary<(int, int), string> manPageReferences  // Clickable man references
```

**TextTags for syntax highlighting:**
```csharp
highlightTag       // Yellow background for search matches
currentMatchTag    // Orange background for current match
headerTag          // Blue, bold, 1.3x scale for section headers (e.g., "NAME", "SYNOPSIS")
commandTag         // Purple, bold for command names
optionTag          // Orange, bold for options (e.g., -h, --help)
argumentTag        // Red, italic for argument placeholders (e.g., <FILE>, USERNAME)
boldTag            // Bold weight for emphasized text
filePathTag        // Teal/green with underline for file paths (/etc/config, ~/file)
urlTag             // Blue with underline for URLs (http://, https://)
manReferenceTag    // Blue with underline for man page references (ls(1), grep(1))
```

## Important Functions

### Initialization and Setup

**Constructor: `MainWindow(string? autoLoadProgram = null, string? autoSearchTerm = null)`**
- Loads UI from `main_window.ui` using GTK Builder
- Initializes all TextTags for syntax highlighting
- Loads settings with `Settings.Load()`
- Wires up event handlers
- Calls `LoadPrograms()` to scan system directories
- Optionally auto-loads a program and search term (from CLI args)

**`LoadPrograms()`**
- Scans directories: `/bin`, `/usr/bin`, `/usr/local/bin`, `/sbin`, `/usr/sbin`
- Collects all executable names into a set (case-insensitive)
- **Smart filtering (when `settings.EnableHelpFallback == false`):**
  - Calls `GetManPageNames()` to query system man page database
  - Performs set intersection to only include executables with man pages
  - Falls back to showing all programs if `man -k` fails
  - Status shows "programs with man pages available"
- Otherwise shows all executables (current behavior)
- Calls `RefreshProgramList("")` to populate TreeView
- Updates status label with count

**`GetManPageNames()` returns HashSet<string>**
- Executes `man -k .` to query system's man page database (whatis)
- Parses output format: `"program_name (section) - description"`
- Extracts program names using regex: `^([^\s(]+)\s*\(`
- Uses case-insensitive HashSet for efficient intersection
- Returns empty set on failure (graceful degradation)

### Man Page Loading

**`LoadManPage(string pageName)`**
- Main entry point for loading man pages
- Flow:
  1. Save notes for currently loaded program (if switching pages)
  2. Try `GetManPageContent(pageName, width)` to get man page with calculated width
  3. If empty and `settings.EnableHelpFallback` is true, try `GetHelpContent(pageName)`
  4. Display content in `manPageView` with appropriate formatting
  5. Set `isManPageLoaded = true` and `currentLoadedProgram = pageName`
  6. Load notes for the new program with `LoadNotes(pageName)`
  7. Clear search state (searchEntry, searchMatches, currentMatchIndex)
- Handles errors gracefully with status messages

**`GetManPageContent(string pageName, int width = 80)` returns string**
- Executes `man {pageName}` using System.Diagnostics.Process
- Sets `MANWIDTH` environment variable to format for specified character width
- Redirects stdout/stderr
- Removes control characters with regex: `[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]`
- Returns empty string on failure

**`CalculateTextViewCharacterWidth()` returns int**
- Measures the allocated width of manPageView in pixels
- Uses Pango layout to measure monospace character width
- Calculates how many characters fit across the TextView
- Returns value between 40-200 characters (default: 80)

**`GetHelpContent(string programName)` returns string**
- Executes `{programName} --help` with 3-second timeout
- Closes stdin immediately to prevent blocking
- Uses Task.Wait(3000) for timeout handling
- Kills process if timeout or doesn't exit
- Removes control characters
- Returns empty string on failure

**`FormatManPage(string programName)`**
- Complex function that applies TextTags to buffer for syntax highlighting
- Processes line by line:
  1. **Section headers** - Detects all-caps lines (e.g., "NAME", "SYNOPSIS")
  2. **Command names** - Highlights program name (whole word matches only)
  3. **Options** - Regex matches `-x`, `--option`, `--option=value`, etc.
  4. **Arguments** - Matches `<WORD>`, `UPPERCASE_WORDS`, placeholder patterns
  5. **URLs** - Matches `http://` and `https://`
  6. **File paths** - Matches paths starting with `/` or `~/`
  7. **Man references** - In "SEE ALSO" section, matches `program(1)` patterns
- Stores man references in `manPageReferences` dictionary for click handling

### Search Functionality

**`SearchInManPage(string searchTerm)`**
- Case-insensitive search using `TextIter.ForwardSearch()`
- Stores all matches in `searchMatches` list
- Applies `highlightTag` to all matches
- Calls `NavigateToMatch(0)` to jump to first match
- Updates status label with match count
- Enables next/previous buttons

**`NavigateToMatch(int index)`**
- Removes `currentMatchTag` from previous match
- Applies `currentMatchTag` to current match
- Scrolls TextView to show match with `ScrollToIter()`
- Updates status label: "Match X of Y"

**`ClearSearchHighlights()`**
- Removes `highlightTag` and `currentMatchTag` from entire buffer

### Notes Functionality

**`LoadNotes(string programName)`**
- Loads notes from `~/.config/gman/notes/program-name.txt`
- Called automatically when a man page is loaded
- Sets `notesView.Buffer.Text` to file contents or empty string if file doesn't exist
- Handles errors gracefully by setting empty text

**`SaveNotes(string programName)`**
- Saves current notes to `~/.config/gman/notes/program-name.txt`
- Creates notes directory if it doesn't exist
- Refreshes program list if this is the first time notes are saved (to show icon)
- Called automatically when:
  - User types in notes view (via `OnNotesChanged`)
  - Switching between man pages
  - Closing the application

**`GetNotesPath(string programName)` returns string**
- Returns full path to notes file for a given program
- Path format: `~/.config/gman/notes/program-name.txt`

**`HasNotes(string programName)` returns bool**
- Checks if a notes file exists for the given program
- Used to show notes icon in program list
- Returns true if notes file exists, false otherwise

**`OnNotesChanged(object? sender, EventArgs e)`**
- Auto-saves notes when buffer changes
- Only saves if a man page is currently loaded

**`OnShowNotesToggled(object? sender, EventArgs e)`**
- Toggles notes panel visibility
- Saves visibility preference to settings
- Calls `UpdateNotesVisibility()` to apply changes

**`UpdateNotesVisibility()`**
- Shows or hides notes panel based on `showNotesCheck.Active`
- When showing: allocates 300px width to notes panel
- When hiding: gives all space to man page view

**`OnWindowKeyPress(object? sender, KeyPressEventArgs args)`**
- Handles global keyboard shortcuts
- 'n' or 'N' key: Toggles notes panel visibility
- Only works when focus is not on searchEntry or notesView

### Event Handlers

**`OnSearchTextChanged(object? sender, EventArgs e)`**
- If `isManPageLoaded == true`: searches within loaded man page
- If `isManPageLoaded == false`: does nothing (program list uses keyboard navigation)

**`OnProgramSelected(object? sender, RowActivatedArgs args)`**
- Triggered on double-click (or Enter key) in program list
- Gets selected program from TreeView and calls `LoadManPage()`

**`OnProgramSelectionChanged(object? sender, EventArgs e)`**
- Only attached when `settings.UseSingleClick == true`
- Triggers on arrow key navigation or single-click
- Loads man page if different from `currentLoadedProgram`

**`OnProgramListKeyPress(object? sender, KeyPressEventArgs args)`**
- **Enter key**: Loads selected program's man page
- **Alphanumeric keys**: Jump to first program starting with typed character
- Uses GTK's `Gdk.Keyval.ToUnicode()` to convert key to character

**`OnManPageViewClicked(object? sender, ButtonPressEventArgs args)`**
- Left-click only
- Converts window coordinates to buffer coordinates
- Checks if click is on a man page reference (using `manPageReferences` dictionary)
- If clicked on reference: calls `LoadManPageAndSelect(programName)`
- If clicked elsewhere and man page loaded: gives focus to search entry

**`OnSettingsClicked(object? sender, EventArgs e)`**
- Creates and runs SettingsDialog
- On OK: reloads settings from disk
- If `UseSingleClick` changed: attaches/detaches `OnProgramSelectionChanged` handler
- If `EnableHelpFallback` changed: calls `LoadPrograms()` to refresh the program list with new filtering

## Common Patterns

### GTK UI Loading Pattern
```csharp
var builder = new Builder();
builder.AddFromFile(GetUiPath());
mainWindow = (Window)builder.GetObject("mainWindow");
searchEntry = (Entry)builder.GetObject("searchEntry");
// Null checks for all widgets
if (mainWindow == null || searchEntry == null /* ... */)
    throw new InvalidOperationException("Failed to load UI");
```

### Event Handler Wiring
```csharp
// Standard event handlers
mainWindow.DeleteEvent += OnDeleteEvent;
searchEntry.Changed += OnSearchTextChanged;
aboutButton.Clicked += OnAboutClicked;

// GLib.ConnectBefore for key events (to intercept before default handling)
[GLib.ConnectBefore]
private void OnSearchEntryKeyPress(object? sender, KeyPressEventArgs args)
{
    if (args.Event.Key == Gdk.Key.Return)
    {
        // Handle Enter key
        args.RetVal = true;  // Mark event as handled
    }
}
```

### TreeView + ListStore Pattern
```csharp
// Setup programs list with three columns: favorite icon, notes icon, text
programStore = new ListStore(typeof(string), typeof(string), typeof(string));
programListView.Model = programStore;

// Column 0: Favorite icon
var favoriteIconColumn = new TreeViewColumn();
var favoriteIconRenderer = new CellRendererPixbuf();
favoriteIconRenderer.StockSize = (uint)IconSize.Menu;
favoriteIconColumn.PackStart(favoriteIconRenderer, false);
favoriteIconColumn.AddAttribute(favoriteIconRenderer, "icon-name", 0);
programListView.AppendColumn(favoriteIconColumn);

// Column 1: Notes icon
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

// Populate
programStore.Clear();
foreach (var program in programs)
{
    string favoriteIcon = IsFavorite(program) ? "starred" : "";
    string notesIcon = HasNotes(program) ? "text-x-generic" : "";
    programStore.AppendValues(favoriteIcon, notesIcon, program);
}

// Iterate
programStore.Foreach((model, path, iter) =>
{
    var value = programStore.GetValue(iter, 2)?.ToString(); // Column 2 is program name
    // Process value...
    return false;  // Continue iteration (true = stop)
});

// Get selected item
if (programListView.Selection.GetSelected(out TreeIter iter))
{
    var value = programStore.GetValue(iter, 2)?.ToString(); // Column 2 is program name
}
```

### TextBuffer Formatting Pattern
```csharp
TextBuffer buffer = textView.Buffer;
string text = buffer.Text;

// Create and add tag to tag table
var tag = new TextTag("tagName");
tag.Foreground = "#2E86AB";
tag.Weight = Pango.Weight.Bold;
buffer.TagTable.Add(tag);

// Apply tag to range
TextIter start = buffer.GetIterAtOffset(startOffset);
TextIter end = buffer.GetIterAtOffset(endOffset);
buffer.ApplyTag(tag, start, end);

// Remove tag
buffer.RemoveTag(tag, buffer.StartIter, buffer.EndIter);

// Search in buffer
TextIter iter = buffer.StartIter;
while (iter.ForwardSearch(searchText, 0, out TextIter matchStart, out TextIter matchEnd, buffer.EndIter))
{
    // Process match
    iter = matchEnd;
}
```

### Process Execution Pattern
```csharp
var process = new System.Diagnostics.Process();
process.StartInfo.FileName = "command";
process.StartInfo.Arguments = "args";
process.StartInfo.UseShellExecute = false;
process.StartInfo.RedirectStandardOutput = true;
process.StartInfo.RedirectStandardError = true;
process.StartInfo.CreateNoWindow = true;

process.Start();
string output = process.StandardOutput.ReadToEnd();
process.WaitForExit();
```

### Process with Timeout Pattern
```csharp
process.Start();
process.StandardInput.Close();  // Prevent blocking on input

var outputTask = System.Threading.Tasks.Task.Run(() => process.StandardOutput.ReadToEnd());

if (!outputTask.Wait(3000))  // 3 second timeout
{
    try { process.Kill(); } catch { }
    return string.Empty;
}

string output = outputTask.Result;
if (!process.HasExited)
{
    try { process.Kill(); } catch { }
}
```

## Key State Transitions

1. **Application startup:**
   - `Program.Main()` → Parse args → Create `MainWindow()` → `LoadPrograms()` → Optional `LoadManPage()` → `ShowAll()`

2. **Loading a man page:**
   - User action (click/Enter/reference click) → `LoadManPage()` → `GetManPageContent()` → Optional `GetHelpContent()` → Set buffer text → `FormatManPage()` → Update state (isManPageLoaded, currentLoadedProgram)

3. **Search within man page:**
   - User types in searchEntry → `OnSearchTextChanged()` → `SearchInManPage()` → Store matches → Highlight all → `NavigateToMatch(0)`

4. **Navigate search results:**
   - Next/Previous button or Enter in searchEntry → `OnNextClicked()`/`OnPreviousClicked()` → Calculate index → `NavigateToMatch(index)`

5. **Settings change:**
   - Settings button → `SettingsDialog.Run()` → Save to disk → Reload in MainWindow → Update event handlers if needed

## Security Considerations

- **EnableHelpFallback** defaults to `false` because it executes arbitrary programs with `--help` flag
- When executing programs:
  - Close stdin immediately
  - Use timeout (3 seconds)
  - Kill process if timeout exceeded
  - Remove control characters from output

## Testing

- Tests located in `tests/` directory
- Use `dotnet test` to run tests
- Current tests focus on formatting functionality

## Key Features

### Clickable Man Page References
Man pages in the SEE ALSO section now support clickable references. When a man page reference like `apparmor(7)` or `aa-stack(8)` is displayed:
- The reference is styled as a clickable link (blue, underlined) using `manReferenceTag`
- The clickable regions are tracked in `manPageReferences` dictionary mapping buffer offsets to program names
- Clicking a reference calls `LoadManPageAndSelect()` which loads the man page and selects it in the program list
- Detection is done via regex pattern `([a-zA-Z0-9_\-\.]+)\(\d+\)` in the `FormatManPage()` method
