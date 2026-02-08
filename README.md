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

## Requirements

* **. NET 8 LTS or later** - [Download from microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0)
* **GTK+ 3.0 libraries** - for X11 graphical interface
* **man command-line utility** - standard on Linux systems

<details>

<summary>
## Installation 
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

## Building from Source

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
