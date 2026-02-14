# GMan - GTK# Man Page Viewer

A graphical man page viewer for Linux/X11 environments, written in C# using GTK#. Features context-aware search, text highlighting, help fallback, and command-line integration.

## Features

* **Browse man pages** from a list of all available system programs
* **Search within loaded man pages** with yellow text highlighting
* **Context-aware search box** - filters program list when no page is loaded, searches page content when one is open
* **Help fallback** - if no man page exists, automatically shows `program --help` output with a warning banner
* **Command-line integration** - specify program and search term as CLI arguments
* **Live filtering** - as you type, the program list updates in real-time
* **Word wrapping** and scrollable text display
* **Status bar** for feedback on load state and search results

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
Version=1.0
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
