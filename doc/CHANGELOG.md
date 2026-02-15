# Changelog

All notable changes to GMan will be documented in this file.

## [Unreleased]

### Added

* **Note-taking for individual man pages**: Added a collapsible notes panel for taking notes on each man page:
  + Notes panel appears to the right of the man page view
  + Toggle visibility with checkbox in panel header or press 'n' key
  + Notes automatically saved to `~/.config/gman/notes/program-name.txt`
  + Notes persist between application sessions
  + Auto-save when typing or switching between man pages
  + Notes are program-specific - each man page has its own notes file
  + Notes panel visibility preference saved in settings
  + Monospace font for consistent formatting
  + **Visual indicator**: Programs with notes show a document icon (📄) in the program list

* **Favorites list with persistent storage**: Added a dedicated favorites list with full persistence and visual indicators:
  + Separate favorites list displayed in the left pane alongside the main program list
  + Press '+' key while in the All Programs list to add the selected program to favorites
  + Press '-' key while in the Favorites list to remove the selected item
  + Star (⭐) icon appears next to favorited programs in the All Programs list
  + Favorites automatically saved to `~/.config/gman/settings.conf`
  + Works with both single-click and double-click modes
  + Type-ahead search works in both lists
  + Enter key loads man pages from either list
  + Duplicate detection with status message feedback
  + Automatically removes unavailable programs from favorites on startup

* **Show/hide controls for program lists**: Added checkboxes to toggle visibility of both lists:
  + Each list has a checkbox in its header to show/hide that list
  + Both checkboxes checked by default (both lists visible)
  + User-resizable split between lists with drag handle (GtkPaned)
  + Default split position: 200px for favorites, remainder for programs

* **Configurable favorites position**: Added setting to control favorites list position:
  + New setting in Settings dialog: "Show favorites at top" vs "Show favorites at bottom"
  + Defaults to showing favorites at top
  + Changes apply immediately when settings are saved
  + Setting persisted to configuration file

* **Built-in Help system**: Added comprehensive help documentation accessible within the application:
  + New Help button in toolbar displays formatted help text
  + Support for `gman --help` command-line argument
  + Help text formatted with syntax highlighting (headers, options, file paths, URLs)
  + Covers all features: favorites, keyboard shortcuts, search, settings
  + User-friendly quick reference guide in man page style

* **Enhanced UI interaction**: Improved user experience with list controls:
  + Clickable list labels toggle visibility (click "Favorites" or "Available Programs" text)
  + Contextual tooltips show keyboard shortcuts when hovering over list items
  + Status bar displays hints for available keyboard shortcuts based on focus
  + Collapsible lists save space by shrinking to header-only height when hidden
  + Preserved divider position restores when showing both lists again

### Technical Details

* **Favorites implementation**:
  + Added `List<string> favorites` field to track favorite programs
  + Added `ListStore favoritesStore` for GTK TreeView model
  + Modified program list to use multi-column TreeView (icon + text) for star indicators
  + Single-column TreeView for favorites (no icon needed)
  + Added `AddToFavorites()` and `RemoveFromFavorites()` helper methods
  + Added `RefreshFavoritesList()` to sync UI with data
  + Added `CleanupFavorites()` to remove unavailable programs on startup
  + Added `SaveFavorites()` to persist changes immediately

* **Settings persistence**:
  + Added `Favorites` property to Settings class (List<string>)
  + Added `FavoritesAtTop` property to Settings class (bool, default true)
  + Settings stored as comma-separated string: `Favorites=ls,grep,man`
  + Updated `Settings.Load()` to parse favorites list
  + Updated `Settings.Save()` to serialize favorites list

* **UI structure changes**:
  + Replaced simple `leftBox` with vertical `GtkPaned` containing two sections
  + Added `favoritesBox` and `programsBox` containers
  + Added `showFavoritesCheck` and `showProgramsCheck` checkboxes
  + Added `favoritesListView` TreeView with event handlers
  + Updated `programStore` from single-column to two-column (icon, text)
  + All `programStore.GetValue(iter, 0)` references updated to use column 1 for text

* **Event handlers**:
  + Added `OnFavoritesRowActivated()` for double-click
  + Added `OnFavoritesKeyPress()` for Enter/- key and type-ahead
  + Added `OnFavoritesSelectionChanged()` for single-click mode
  + Added `OnShowFavoritesToggled()` and `OnShowProgramsToggled()` for checkbox toggles
  + Updated `OnProgramListKeyPress()` to handle '+' key for adding favorites
  + Updated `OnSettingsClicked()` to handle favorites position changes

* **Multi-character type-ahead search in program list**: When the program list has focus, you can now type multiple characters (up to 5) to quickly jump to programs. Features include:
  + Type quickly (within 1 second) to accumulate characters - e.g., type "gre" to jump to "grep"
  + Visual feedback in status bar shows what you've typed ("Type-ahead: gre")
  + Automatically clears after 1 second of no typing
  + Prefix matching - only matches programs that START with typed text
  + Works seamlessly with existing single-click/double-click behavior

* **Smart program list filtering**: When "Run programs with --help if no man page exists" is disabled in settings, the program list now only shows executables that have man pages. This is achieved by:
  + Querying the system's man page database via `man -k .`
  + Intersecting the list with installed executables from system directories
  + Removing entries that don't have corresponding man pages
  + This reduces clutter and ensures all listed programs can be documented
  + Status bar shows "programs with man pages available" when filtering is active
  + List automatically refreshes when the setting is toggled

* **Clickable man page references**: Program references in the "SEE ALSO" section (e.g.,  `apparmor(7)`,  `aa-stack(8)`) are now clickable links styled in blue with underline. Clicking a reference will:
  + Load that program's man page
  + Find and highlight the program in the program list
  + Scroll to show the selected program

### Technical Details

* **Program list filtering implementation**:
  + Added `GetManPageNames()` method to query `man -k .` and parse program names using regex `^([^\s(]+)\s*\(`
  + Modified `LoadPrograms()` to perform set intersection when `settings.EnableHelpFallback == false`
  + Uses `HashSet<string>` with case-insensitive comparison for efficient intersection
  + Graceful fallback to showing all programs if `man -k` fails or returns no results
  + Updated `OnSettingsClicked()` to detect `EnableHelpFallback` changes and reload the program list

* **Clickable man page references**:
  + Added `manReferenceTag` TextTag for styling clickable man page references
  + Added `manPageReferences` dictionary to track clickable regions and their associated program names
  + Enhanced `FormatManPage()` to detect man page references in SEE ALSO sections using regex pattern `([a-zA-Z0-9_\-\.]+)\(\d+\)`
  + Updated `OnManPageViewClicked()` to handle clicks on man page references and load the corresponding pages
  + Added `LoadManPageAndSelect()` helper method to load a man page and select it in the program list
