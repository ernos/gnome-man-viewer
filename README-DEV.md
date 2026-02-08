# GMan Developer Guide

This document explains how the GTK# application is structured, how the UI is built with Glade/Cambalache, and how the app logic connects to the UI. It is written for developers who are new to GTK but familiar with MVVM concepts from Android.

## Quick Overview

- The UI is defined in [ui/main_window.ui](ui/main_window.ui).
- The UI file is loaded at runtime by [MainWindow.cs](MainWindow.cs).
- Widget references are fetched by ID and wired to C# event handlers.
- Program list filtering happens in memory as the user types.
- Man pages are loaded by running the system `man` command.
- Search within loaded man pages highlights all matches in yellow.
- If no man page exists, the app falls back to showing `program --help` output.

## Project Layout

- [Program.cs](Program.cs): Application entry point, initializes GTK, parses CLI arguments.
- [MainWindow.cs](MainWindow.cs): Loads the UI, wires events, contains app logic.
- [ui/main_window.ui](ui/main_window.ui): UI definition built with Glade/Cambalache.
- [gman.csproj](gman.csproj): Project file, includes UI file as content.

## GTK Mental Model (MVVM Mapping)

GTK is not MVVM by default, but you can map the ideas:

- View: the .ui file (similar to an Android XML layout).
- Code-behind (MainWindow): plays the role of a simple ViewModel + Controller.
- Model: the list of programs and the man page content.

This project uses a straightforward code-behind approach:

- UI is loaded from the .ui file.
- C# code gets widget references by ID.
- Event handlers update the in-memory model and update view widgets.

If you want a stricter MVVM pattern later, you can:

- Move filtering and man loading into a separate class.
- Expose observable properties.
- Update the view by binding or by a small adapter layer.

## How the UI Is Loaded

In [MainWindow.cs](MainWindow.cs), the UI is loaded with `Gtk.Builder`:

- `builder.AddFromFile(...)` loads the .ui file.
- `builder.GetObject("widgetId")` returns widget instances.
- The code wires signals (e.g., `searchEntry.Changed`) to methods.

The UI file is copied to the build output folder via [gman.csproj](gman.csproj), so `dotnet run` works out of the box.

## UI Structure (main_window.ui)

The UI has three main areas:

1. Search row at the top
   - `searchEntry` (GtkEntry) - context-aware: filters programs OR searches man page text

2. Split view in the middle (GtkPaned)
   - Left: `programListView` (GtkTreeView) inside a scrolled window
   - Right: `manPageView` (GtkTextView) inside a scrolled window

3. Status label at the bottom
   - `statusLabel` (GtkLabel) - shows load state, search results, or error messages

These widget IDs must stay stable because the C# code looks them up by name.

## App Logic Flow

### Startup

- `Program.cs` parses command-line arguments (see CLI Args section below).
- `MainWindow` loads the UI and sets up event handlers.
- The program list is collected from common bin directories.
- The list is loaded into a `Gtk.ListStore` and shown in the TreeView.
- If a program was passed on CLI, it is auto-loaded.
- If a search term was passed on CLI, it is auto-searched within that program.

### Typing in the Search Box (Context-Aware)

When no man page is loaded:
- The `searchEntry.Changed` event fires on every edit.
- The code filters `allPrograms` in memory (case-insensitive substring match).
- The `ListStore` is cleared and repopulated with matching programs.
- The status bar shows match count.

When a man page is loaded:
- The `searchEntry.Changed` triggers `SearchInManPage()`.
- All occurrences of the search term are highlighted with yellow background.
- The view scrolls to the first match.
- The status bar shows total match count.

### Selecting a Program (Double-Click)

- Double-clicking a row in `programListView` triggers `RowActivated`.
- The selected program name is used to load its manual page.
- Search box is cleared for the new page.
- The app is now in "man page loaded" state, so typing will search the page.

### Loading a Man Page

1. `LoadManPage(string programName)` is called.
2. It attempts to fetch the man page with: `man <programName>`.
3. If successful, the page is displayed and `isManPageLoaded = true`.
4. If the man page doesn't exist, the app tries: `<programName> --help`.
   - If help is available, it is displayed with a warning banner at the top.
   - If neither exists, an error message is shown.
5. The search state is reset.

## Command-Line Arguments

The app supports optional command-line arguments:

```bash
gman [program-name] [-s|--search search-term]
```

**Examples:**

```bash
# No arguments: show the program list
gman

# Auto-load a man page
gman ls

# Load a man page and auto-search for a term
gman ls -s malloc

# Search for lowercase search terms works too
gman ls -s "file descriptor"
```

**Parsing Logic (in Program.cs):**

- First positional (non-option) argument is the program name.
- `-s` or `--search` followed by a value sets the search term.
- Search term only takes effect if a program name was provided.

**Sequence on Startup:**
1. Parse CLI args.
2. Create MainWindow with parsed program name and search term.
3. MainWindow loads programs and UI.
4. If program name is provided, auto-load that man page.
5. If search term is provided, auto-search within the loaded page.

## Text Highlighting & Search

### TextTag for Highlighting

In [MainWindow.cs](MainWindow.cs), a `TextTag` named "highlight" is created with a yellow background:

```csharp
highlightTag = new TextTag("highlight");
highlightTag.Background = "yellow";
manPageView.Buffer.TagTable.Add(highlightTag);
```

TextTags are GTK's way of applying formatting to ranges of text. Unlike HTML/CSS, they are applied programmatically to character ranges in the buffer.

### Search Algorithm

`SearchInManPage()` does the following:

1. Clears any previous highlights.
2. Converts search term to lowercase.
3. Iterates through the buffer using `TextIter.ForwardSearch()`.
   - Searches are case-insensitive.
   - Each match is tagged with the highlight tag.
4. Scrolls to the first match.
5. Updates the status bar with match count.

## Editing the UI With Glade or Cambalache

### Option A: Glade

1. Install Glade:
   - Ubuntu/Debian: `sudo apt-get install glade`
2. Open the UI file:
   - File -> Open -> [ui/main_window.ui](ui/main_window.ui)
3. Make changes, then save.

### Option B: Cambalache

1. Install Cambalache:
   - Ubuntu/Debian: `sudo apt-get install cambalache`
2. Open the UI file:
   - File -> Open -> [ui/main_window.ui](ui/main_window.ui)
3. Edit and save.

### Important Notes When Editing

- Keep the widget IDs the same unless you also update [MainWindow.cs](MainWindow.cs).
- The IDs used by code are:
  - `mainWindow`
  - `searchEntry`
  - `programListView`
  - `manPageView`
  - `statusLabel`
- Changing layout is safe; changing IDs requires code updates.

## Building and Running (Dev)

```bash
dotnet build
```

```bash
dotnet run
```

With arguments:

```bash
dotnet run -- ls -s malloc
```

## Troubleshooting

### UI File Not Found

If you see a UI loading error, check:

- [gman.csproj](gman.csproj) includes `ui/main_window.ui` as content.
- The file exists at [ui/main_window.ui](ui/main_window.ui).

### No Programs Listed

The app scans these paths:

- `/bin`
- `/usr/bin`
- `/usr/local/bin`
- `/sbin`
- `/usr/sbin`

If none of those exist or are accessible, the list will be empty.

### Man Page Not Found, Help Used Instead

The app will try `program --help` if the man page doesn't exist. If the program takes no `--help` argument, the app will show an error.

### Search Doesn't Find Text

- Search is case-insensitive, so any casing should match.
- Make sure a man page or help text is actually loaded (status bar shows "Displaying").
- Some programs output special formatting characters that may affect searching.

## Future Improvements

- Use a background task to load programs without blocking the UI.
- Add section selection (e.g., `man 2 open` vs `man 3 open`).
- Add persistent favorites and history.
- Introduce a ViewModel layer for testability.
- Add regex search support.
- Add case-sensitive toggle in UI.

