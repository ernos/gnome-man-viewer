# GMan - GTK# Man Page Viewer

A simple graphical man page viewer for X11/Linux environments, written in C# using GTK#.

## Features

* Search for and view Unix man pages
* Clean GTK# interface
* Text display with word wrapping
* Status bar for feedback

## Requirements

* . NET 8 LTS or later
* GTK+ 3.0 libraries (for X11)
* `man` command-line utility

## Installation

### Dependencies (Ubuntu/Debian)

```bash
sudo apt-get install libgtk-3-0 gtk-3-examples
```

### For development

```bash
sudo apt-get install libgtk-3-dev
```

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

Or run the compiled executable directly:

```bash
./bin/Release/net8.0/gman
```

## Usage

1. Enter a man page name in the search box (e.g., `ls`, `man`, `grep`)
2. Click "View" or press Enter
3. The man page content will be displayed in the text area

## Project Structure

* `Program.cs` - Application entry point
* `MainWindow.cs` - Main UI window implementation
* `gman.csproj` - Project configuration

## Additional information

I created this project mainly because I wanted to transfer my application development skills over from android and kotlin to C# and Gtk. I am in no way new to the coding syntax, I've been programming C/C++ for many many years, but i never got into the GUI side of it really. So that's why README-DEV.md exists,

## License

MIT
