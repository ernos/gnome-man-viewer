# GMan - GTK# Man Page Viewer

A graphical man page viewer for Linux/X11 environments, written in C# using GTK#. Features favorites list, smart filtering, advanced search, keyboard navigation, clickable man references, and command-line integration.

## Features

### Man Page Browsing
* **Browse man pages** from a list of all available system programs
* **Smart program filtering** - only shows programs with man pages (when help fallback disabled)
* **Favorites list** - save frequently used programs for quick access
* **Star indicators** - visual markers show which programs are favorited
* **Dual list view** - separate lists for favorites and all programs with show/hide controls
* **Clickable man references** - click SEE ALSO references to jump to related pages
* **Command-line integration** - specify program and search term as CLI arguments

### Search & Navigation
* **Search within loaded man pages** with yellow text highlighting for all matches
* **Orange highlight** for current match, yellow for other matches
* **Next/Previous buttons** for match navigation
* **Return key** jumps to next search match
* **Match counter** - "Match X of Y" display in status bar
* **Live filtering** - program list updates in real-time as you type

### Keyboard Navigation
* **Multi-character type-ahead** - type up to 5 characters to jump to programs
* **Single-click or double-click** mode for loading man pages (configurable)
* **Keyboard shortcuts**:
  - `+` key - add program to favorites (when in All Programs list)
  - `-` key - remove program from favorites (when in Favorites list)
  - `Enter` - load man page from either list
  - Type letters/numbers - quick jump with type-ahead (1 second buffer)
* **Contextual tooltips** - hover hints show available keyboard shortcuts

### Display & Interface
* **Syntax highlighting** - headers, commands, options, arguments, URLs, and file paths
* **Word wrapping** and scrollable text display
* **User-resizable split** - drag divider between favorites and programs lists
* **Collapsible lists** - hide either list to maximize space
* **Status bar** - feedback on load state, search results, and keyboard shortcuts
* **Help fallback** - shows `program --help` output when no man page exists

### Settings & Persistence
* **Favorites persistence** - automatically saves favorites to `~/.config/gman/settings.conf`
* **Position preference** - show favorites at top or bottom (configurable)
* **Click behavior** - choose single-click or double-click to load pages
* **Help fallback toggle** - enable/disable automatic `--help` execution

## Planned Future Feature Implementations

### Settings Configuration Window amd Button

* **System-wide installation** - *copy link to user bin path and create .desktop launcher*
* **Save window state and size**
* **Transparent background** - *would be useful to be able to see terminal aswell*
* **Dark mode option**

### Improved search bar and algorithm

* **Next/Previous**: *Buttons for jumping to next or previous match*
* **Support for Regular Expressions**: *Support for regex search and highlighting*

### **Not Decided yet**

* **Command text scratch pad**: For creating complex commands while you still have the man page visible. You can also check other commands and write down complex combinations of commands and scripts

## Changelog and release information

### **Version 1.0** - Major Release

**Search Overhaul**
* Added Previous and Next buttons for match navigation
* Return key now jumps to next match
* Current match highlights in orange, other matches in yellow
* "Match X of Y" counter in status bar
* Smart button enabling based on search state

**Favorites System**
* Dedicated favorites list with persistent storage
* Press `+` to add programs to favorites
* Press `-` to remove from favorites
* Star icons indicate favorited programs
* Show/hide controls for both lists
* Configurable position (top or bottom)
* User-resizable split between lists
* Automatic cleanup of unavailable programs

**Enhanced Keyboard Navigation**
* Multi-character type-ahead search (up to 5 characters, 1-second buffer)
* Visual feedback in status bar showing typed characters
* Prefix matching for quick program jumping
* Contextual keyboard shortcut hints in status bar
* Tooltips showing available shortcuts on hover

**Clickable Man References**
* SEE ALSO section references are now clickable links
* Click to load referenced man page
* Automatically selects and scrolls to program in list
* Styled with blue underline for visibility

**Smart Program Filtering**
* Only shows programs with man pages when help fallback disabled
* Queries system man database via `man -k .`
* Efficient set intersection (handles ~9,000 entries in <1 second)
* Status bar shows "programs with man pages available"
* Automatic refresh when settings change

**UI Improvements**
* Bash completion script for command-line usage
* Syntax highlighting for headers, commands, options, arguments
* File paths and URLs highlighted with underlines
* Collapsible list headers to maximize viewing space
* Clickable list labels toggle visibility

**Bug Fixes & Stability**
* Fixed GLib-GObject-CRITICAL errors and crashes (exit code 139)
* Proper GTK lifecycle management for TreeView models
* Fixed settings dialog disposal issues
* Added event loop processing safeguards

    **Released *2026-02-14***

### **Version 0.3**

1. Added a search and highlight function within the man text.
2.  If a man page does not exists for a bin file, it will run the bin file with ` --help` and output that instead, with a warning at the top.
3. Added argument parsing so that user can run it from command line with the `-s` or `--search` switch to open up a man document and highlight all of the searches automatically. Example:
 `gman nmap -s port`

    **Released *2026-02-08 14:18:00***

### **Version 0.2** 

1. Added an about button & dialogue
2. Single click now opens documentation in the viewer
    **Released *2026-02-07 10:32:57***

### **Version 0.1**

1. First release

**Released *2026-02-06 12:23:02***

## Requirements

* **. NET 8 LTS or later** - [Download from microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0)
* **GTK+ 3.0 libraries** - for X11 graphical interface
* **man command-line utility** - standard on Linux systems

## Installation Instructions

<details>

<summary>
Linux installation instructions
</summary>

### Linux (Ubuntu/Debian)

**Prerequisites:**

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0 libgtk-3-0 libgtk-3-dev man
```

**Clone and build:**

```bash
git clone https://github.com/yourusername/gman.git
cd gman
dotnet build -c Release
```

**Run:**

```bash
dotnet run
# Or run the compiled executable
./bin/Release/net8.0/gman
```

**Make a launcher(wayland):**

This is needed for the ubuntu dock and launcher to display the program with the correct icon.

Create file `gman.desktop` :

```
[Desktop Entry]
Version=1.1
Type=Application
Name=GMan
Comment=GTK# Man Page Viewer
Exec=/home/peb/projects/gman/bin/Release/net8.0/gman
Icon=/home/peb/projects/gman/ui/icon_128.png
Terminal=false
Categories=Utility;Development;Documentation;
Keywords=man;manual;documentation;help;
StartupWMClass=gman
```

***Don't forget to update both Exec and Icon paths to match yours***

```
# Make the desktop file executable
chmod +x gman.desktop

# Copy to your local applications folder
mkdir -p ~/.local/share/applications
cp gman.desktop ~/.local/share/applications/

# Update the desktop database
update-desktop-database ~/.local/share/applications/

# Build release version and update the Exec path if needed
dotnet build -c Release
```

### Linux (Fedora/RHEL)

**Prerequisites:**

```bash
sudo dnf update
sudo dnf install -y dotnet-sdk-8.0 gtk3 gtk3-devel man
```

**Clone and build:**

```bash
git clone https://github.com/yourusername/gman.git
cd gman
dotnet build -c Release
```

**Run:**

```bash
dotnet run
./bin/Release/net8.0/gman
```

</details>
<details>

<summary>
MacOS installation instructions</summary>

### Linux (Arch)

**Prerequisites:**

```bash
sudo pacman -S dotnet-sdk gtk3 man
```

**Clone and build:**

```bash
git clone https://github.com/yourusername/gman.git
cd gman
dotnet build -c Release
```

**Run:**

```bash
dotnet run
./bin/Release/net8.0/gman
```

</details>
<details>

<summary>
Linux (Ubuntu/Devian) installation instructions
</summary>

### macOS

GMan requires GTK+ 3.0, which is not natively available on macOS. You have two options:

**Option 1: Using Homebrew (Recommended)**

```bash
# Install dependencies via Homebrew
brew install gtk+3 dotnet man-db

# Clone and build
git clone https://github.com/yourusername/gman.git
cd gman
dotnet build -c Release

# Run
dotnet run
./bin/Release/net8.0/gman
```

**Option 2: Use a Linux VM or WSL**

Since GTK is primarily designed for Linux, consider using:
* **Parallels Desktop** or **VMware Fusion** with Ubuntu
* **Docker** with an X11 forwarding setup
* **VirtualBox** with a Linux guest

</details>
<details>

<summary>
Windows Installation Instructions
</summary>

### Windows

GMan is designed for X11 environments and is **not officially supported on native Windows**. However, you have options:

**Option 1: Windows Subsystem for Linux (WSL2) - Recommended**

```bash
# Install WSL2 with Ubuntu
wsl --install -d Ubuntu

# Inside WSL2 Ubuntu:
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0 libgtk-3-0 libgtk-3-dev man

# Clone and build
git clone https://github.com/yourusername/gman.git
cd gman
dotnet build -c Release

# Run with X Server (e.g., VcXsrv or Xming)
# Set DISPLAY variable before running
export DISPLAY=:0
dotnet run
./bin/Release/net8.0/gman
```

**Option 2: VirtualBox or Hyper-V with Linux Guest**

Use any Linux distribution in a virtual machine and follow the Linux installation steps above.

**Option 3: Docker**

```bash
# Build Docker image
docker build -t gman .

# Run with X11 forwarding
docker run -e DISPLAY=$DISPLAY -v /tmp/.X11-unix:/tmp/.X11-unix gman
```

</details>

## Building from Source

<details>

<summary>Compiling and building from source code</summary>

**Prerequisites:**

* . NET 8 SDK
* GTK+ 3.0 development files
* Git

**Build:**

```bash
dotnet build
```

**Build Release:**

```bash
dotnet build -c Release
```

**Run:**

```bash
dotnet run
```

**Run with arguments:**

```bash
dotnet run -- ls -s malloc
```

## Usage

### GUI Mode

```bash
# Show the program list and search interface
gman

# Auto-load a specific man page
gman ls

# Auto-load a man page and search for a term
gman ls -s malloc
```

</details>

## Usage and Examples

### Favorites Management

**Adding to Favorites:**
1. Navigate to a program in the All Programs list
2. Press the `+` key or hover for tooltip
3. Program appears in Favorites list with star icon in All Programs

**Removing from Favorites:**
1. Select program in Favorites list
2. Press the `-` key
3. Program removed from Favorites, star icon disappears

**List Visibility:**
* Click checkboxes or labels to show/hide lists
* Drag divider to resize split between lists
* Hidden lists collapse to save space
* Favorites persist across application restarts

### Keyboard Navigation

**Type-Ahead Search:**
* Type 1-5 characters quickly (within 1 second)
* Program list jumps to first match
* Status bar shows what you've typed
* Works in both Favorites and All Programs lists

**Keyboard Shortcuts:**
* `+` - Add selected program to favorites (All Programs list)
* `-` - Remove selected program from favorites (Favorites list)
* `Enter` - Load man page for selected program
* Letters/numbers - Quick jump to matching programs
* `Return` (in search box) - Jump to next search match

### Search Features

**Within Man Pages:**
* Type in search box to highlight all matches
* All matches highlighted in yellow
* Current match highlighted in orange  
* Navigate with Previous/Next buttons or Return key
* Status shows "Match X of Y"

**Clickable References:**
* Click blue underlined references in SEE ALSO sections
* Automatically loads referenced man page
* Program selected and scrolled into view

### Context-Aware Search

When **no page is loaded:**
* Type in the search box to filter the program list by name
* Matching programs are highlighted in the list
* Status bar shows count of matching programs

When **a page is loaded:**
* Type in the search box to search within the man page text
* All matches are highlighted with yellow background
* View automatically scrolls to the first match
* Status bar shows total match count

### Examples

```bash
# Open GMan and manually select 'grep'
gman

# Show built-in help
gman --help

# Directly open the 'grep' man page
gman grep

# Open 'grep' and auto-search for the word 'pattern'
gman grep -s pattern

# Search for a multi-word phrase
gman grep -s "regular expression"

# Show help if no man page exists
gman gcc -s "optimization"
```

## Command-Line Arguments

```
gman [program-name] [-s|--search search-term]
```

* `program-name` (optional) - the program to load the man page for
* `-s` or `--search` (optional) - search term to find within the man page
  + Only takes effect if a program name is provided

### Examples

| Command | Result |
|---------|--------|
| `gman` | Show program list |
| `gman ls` | Auto-load `ls` man page |
| `gman ls -s "directory"` | Load `ls` , search for "directory" |
| `gman grep --search "pattern"` | Load `grep` , search for "pattern" |
| `gman nonexistent` | Show `nonexistent --help` if man page not found |

## Project Structure

* [Program.cs](Program.cs) - Application entry point, CLI argument parsing
* [MainWindow.cs](MainWindow.cs) - UI logic, search, highlighting, help fallback
* [ui/main_window.ui](ui/main_window.ui) - Glade/Cambalache UI definition
* [gman.csproj](gman.csproj) - Project configuration
* [README-DEV.md](README-DEV.md) - Developer guide (MVVM, GTK concepts, architecture)

## Technical Details

### Architecture

GMan uses a **code-behind pattern** familiar to MVVM developers from Android:

* **View**: [ui/main_window.ui](ui/main_window.ui) (Glade definition)
* **Code-behind**: [MainWindow.cs](MainWindow.cs) (event handlers + logic)
* **Model**: In-memory lists of programs and search results

### Search Implementation

Text search uses GTK's `TextTag` system with case-insensitive `ForwardSearch()` :

1. All occurrences of the search term are found
2. Each match is tagged with a yellow background
3. View jumps to the first match
4. Status bar shows match count

### Help Fallback

When a man page doesn't exist:

1. App attempts to load the manual with `man <program>`
2. If not found, runs `<program> --help`
3. If help exists, displays it with a warning banner
4. If neither exists, shows an error message

## Contributing

For development setup and contribution guidelines, see [README-DEV.md](README-DEV.md).

This project was created as a learning exercise to transfer GUI development skills from Android/Kotlin to C#/GTK#.

## License

MIT

## Credits and information form author

I know C# seems like a strange choice when building an application for Linux using GTK. However, I want to give you my reasoning for this anand it's really because I am trying to showcase my skills and proficiency in C# since that is the largest market and has the most jobs on offer. I'm i between jobs now and this is my first ever C# application, having mostly worked in C/C++/Full-Stack and most recently in Android development with Kotlin. Check out my portfolio website for other applications or to get in contact with me!

* Portfolio: https://www.yourdev.net - *My portfolio right now mostly of Android Applications*

* Blog: https://www.yourdev.net/blog.php -
    *My blog where i write programming tutorials and other tech related stuff.*
