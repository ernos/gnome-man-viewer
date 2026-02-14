# Changelog

All notable changes to GMan will be documented in this file.

## [Unreleased]

### Added

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
