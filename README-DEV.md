# GMan Developer Guide

This document explains how the GTK# application is structured, how the UI is built with Glade/Cambalache, and how the app logic connects to the UI. It is written for developers who are new to GTK but familiar with MVVM concepts from Android.

## Quick Overview

- The UI is defined in [ui/main_window.ui](ui/main_window.ui).
- The UI file is loaded at runtime by [MainWindow.cs](MainWindow.cs).
- Widget references are fetched by ID and wired to C# event handlers.
- Program list filtering happens in memory as the user types.
- Man pages are loaded by running the system `man` command.

## Project Layout

- [Program.cs](Program.cs): Application entry point, initializes GTK, creates the window.
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
   - `searchEntry` (GtkEntry)

2. Split view in the middle (GtkPaned)
   - Left: `programListView` (GtkTreeView) inside a scrolled window
   - Right: `manPageView` (GtkTextView) inside a scrolled window

3. Status label at the bottom
   - `statusLabel` (GtkLabel)

These widget IDs must stay stable because the C# code looks them up by name.

## App Logic Flow

### Startup

- `MainWindow` loads the UI and sets up event handlers.
- The program list is collected from common bin directories.
- The list is loaded into a `Gtk.ListStore` and shown in the TreeView.

### Typing in the Search Box

- The `searchEntry.Changed` event fires on every edit.
- The code filters `allPrograms` in memory.
- The `ListStore` is cleared and repopulated.

### Selecting a Program

- Double-clicking a row triggers `RowActivated`.
- The selected program name is used to run `man <program>`.
- The output is put into the `TextView` buffer.

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

### Man Page Not Found

Some programs do not have man pages. The app will show a friendly error and keep running.

## Future Improvements

- Use a background task to load programs without blocking the UI.
- Add section selection (e.g., `man 2 open` vs `man 3 open`).
- Add persistent favorites and history.
- Introduce a ViewModel layer for testability.
