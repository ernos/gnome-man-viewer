# Integration Testing Guide for GMan

This document provides a comprehensive guide for manual integration testing of the GMan application after the refactoring.

## Overview

While unit tests cover individual services (179 tests covering business logic), integration tests verify that all components work together correctly in the actual application. Since GMan is a GTK# application, manual testing is the primary method for integration verification.

## Test Environment Setup

### Prerequisites
- Linux system with X11
- .NET 8.0 SDK installed
- GTK# 3.24+ installed
- Man page database available (`man -k` command works)

### Build and Run
```bash
cd /home/peb/projects/gman
dotnet build -c Debug
dotnet run
```

## Integration Test Scenarios

### 1. Program Discovery and Loading

**Test 1.1: Load Application**
- **Action:** Launch GMan
- **Expected:** 
  - Window appears with program list populated
  - Status shows "Ready - X programs available" or "Ready - X programs with man pages available"
  - Help/welcome message displayed by default

**Test 1.2: Browse Program List**
- **Action:** Scroll through program list
- **Expected:**
  - List is sorted alphabetically (case-insensitive)
  - Favorite icons (★) appear for favorited programs
  - Notes icons (📄) appear for programs with notes
  - No duplicates in the list

**Test 1.3: Load Man Page**
- **Action:** 
  1. Double-click (or single-click if enabled) on "ls" in program list
  2. Wait for content to load
- **Expected:**
  - Man page content displays in main view
  - Status shows "Displaying: ls"
  - Window title updates to "GMan - LS Manual"
  - Add to Favorites button becomes sensitive
  - Man page is formatted with syntax highlighting

**Test 1.4: Load Non-Existent Man Page (Help Disabled)**
- **Action:**
  1. Settings → Disable "Run programs with --help"
  2. Try to load a program without a man page
- **Expected:**
  - Error message: "Error: No manual entry for 'program'"
  - Add to Favorites button stays insensitive

**Test 1.5: Load Non-Existent Man Page (Help Enabled)**
- **Action:**
  1. Settings → Enable "Run programs with --help"
  2. Try to load a program without man page but with --help (e.g., some utilities)
- **Expected:**
  - Warning banner appears: "⚠️ WARNING: No man page found"
  - Help content displays below warning
  - Add to Favorites button becomes sensitive

### 2. Search Functionality

**Test 2.1: Search Within Man Page**
- **Action:**
  1. Load "ls" man page
  2. Type "option" in search box
- **Expected:**
  - All instances of "option" highlighted in yellow
  - First match highlighted in orange
  - Status shows "Match 1 of X for 'option' in ls"
  - Next/Previous buttons enabled

**Test 2.2: Navigate Search Results**
- **Action:** 
  1. Search for "option" in ls
  2. Click Next button 3 times
  3. Click Previous button 2 times
- **Expected:**
  - Orange highlight moves to next/previous match
  - Status updates: "Match X of Y"
  - Scrolls to show current match
  - Wraps around at end/beginning

**Test 2.3: Clear Search**
- **Action:**
  1. Search for "option"
  2. Clear search box
- **Expected:**
  - All highlights removed
  - Status shows "Displaying: program-name"
  - Next/Previous buttons disabled

**Test 2.4: Case-Insensitive Search**
- **Action:**
  1. Load man page
  2. Search for "OPTION"
- **Expected:**
  - Finds matches for "option", "Option", "OPTION"

### 3. Favorites Management

**Test 3.1: Add to Favorites**
- **Action:**
  1. Load "ls" man page
  2. Click "+" (Add to Favorites) button
- **Expected:**
  - Status shows "Added 'ls' to favorites"
  - Star icon (★) appears next to "ls" in program list
  - "ls" appears in Favorites list
  - Clicking "+" again shows "ls is already in favorites"

**Test 3.2: Remove from Favorites (Context Menu)**
- **Action:**
  1. Right-click on favorited program in Favorites list
  2. Select "Remove from Favorites"
- **Expected:**
  - Program removed from Favorites list
  - Star icon removed from program list
  - Status shows "Removed 'program' from favorites"

**Test 3.3: Favorites Persistence**
- **Action:**
  1. Add 3 programs to favorites
  2. Close GMan
  3. Reopen GMan
- **Expected:**
  - All 3 programs still in Favorites list
  - Star icons still visible

**Test 3.4: Favorites Position (Top)**
- **Action:**
  1. Settings → "Show favorites at top" (default)
  2. Observe layout
- **Expected:**
  - Favorites list appears above program list

**Test 3.5: Favorites Position (Bottom)**
- **Action:**
  1. Settings → "Show favorites at bottom"
  2. Observe layout
- **Expected:**
  - Favorites list appears below program list
  - Setting persists after restart

### 4. Notes Functionality

**Test 4.1: Add Notes**
- **Action:**
  1. Load "grep" man page
  2. Press 'n' or click Notes checkbox
  3. Type "My grep notes" in notes area
- **Expected:**
  - Notes panel appears (300px wide)
  - Notes auto-save while typing
  - Notes icon (📄) appears next to "grep" in program list

**Test 4.2: Load Existing Notes**
- **Action:**
  1. Add notes to "grep"
  2. Load different man page
  3. Load "grep" again
- **Expected:**
  - Previous notes content appears in notes panel

**Test 4.3: Notes Persistence**
- **Action:**
  1. Add notes to "ls"
  2. Close GMan
  3. Reopen and load "ls"
- **Expected:**
  - Notes content preserved

**Test 4.4: Toggle Notes Panel**
- **Action:**
  1. Press 'n' key (or click checkbox)
  2. Press 'n' again
- **Expected:**
  - Panel shows/hides
  - Width allocates correctly

### 5. Type-Ahead Navigation

**Test 5.1: Type-Ahead in Program List**
- **Action:**
  1. Click in program list (ensure focus)
  2. Type "gr" quickly
- **Expected:**
  - Status shows: "Type-ahead: gr"
  - Jumps to first program starting with "gr" (e.g., "grep")
  - Selection highlights program

**Test 5.2: Type-Ahead Timeout**
- **Action:**
  1. Type "g"
  2. Wait 1.5 seconds
  3. Type "r"
- **Expected:**
  - First types "g": jumps to first "g" program
  - After timeout: buffer resets
  - Typing "r": jumps to first "r" program (not "gr")

**Test 5.3: Type-Ahead in Favorites List**
- **Action:**
  1. Add 5+ programs to favorites
  2. Click in Favorites list
  3. Type first letters of a favorite
- **Expected:**
  - Jumps to matching favorite
  - Works same as program list

**Test 5.4: Case-Insensitive Type-Ahead**
- **Action:**
  1. Type "LS" in program list
- **Expected:**
  - Jumps to "ls" (matches case-insensitively)

### 6. Settings Management

**Test 6.1: Change Click Behavior**
- **Action:**
  1. Settings → Select "Single-click to load man page"
  2. Click OK
  3. Single-click on program
- **Expected:**
  - Man page loads immediately on single click
  - Setting persists after restart

**Test 6.2: Double-Click Behavior**
- **Action:**
  1. Settings → Select "Double-click to load man page"
  2. Click OK
  3. Single-click on program
- **Expected:**
  - Man page does NOT load
  - Double-click loads man page

**Test 6.3: Enable Help Fallback**
- **Action:**
  1. Settings → Check "Run programs with --help"
  2. Observe warning message
  3. Click OK
- **Expected:**
  - Warning shown about security
  - Program list may refresh (filtering changes)
  - Setting persists after restart

**Test 6.4: Auto-Copy Selection**
- **Action:**
  1. Settings → Enable "Automatically copy selected text"
  2. Load man page
  3. Select text with mouse
- **Expected:**
  - Selected text automatically copied to clipboard
  - Can paste into other applications

### 7. Formatting and Display

**Test 7.1: Syntax Highlighting**
- **Action:** Load "ls" man page, observe formatting
- **Expected:**
  - Section headers (NAME, SYNOPSIS) in blue, bold, larger
  - Command name "ls" in purple
  - Options (-l, --help) in orange
  - Arguments (<FILE>) in red, italic
  - File paths (/etc/config) in teal with underline
  - URLs in blue with underline

**Test 7.2: Man Page References**
- **Action:**
  1. Load "bash" man page
  2. Scroll to "SEE ALSO" section
  3. Click on a reference like "sh(1)"
- **Expected:**
  - Clicking loads the referenced man page ("sh")
  - Program selected in program list

**Test 7.3: Window Resizing**
- **Action:**
  1. Load man page
  2. Resize window wider/narrower
  3. Load different man page
- **Expected:**
  - Man page reformats to new width
  - Text flows correctly

### 8. UI Responsiveness

**Test 8.1: Panel Visibility Toggles**
- **Action:**
  1. Uncheck "Favorites" checkbox
  2. Uncheck "Programs" checkbox  
  3. Check both again
- **Expected:**
  - Panels hide/show correctly
  - Space reallocated appropriately
  - Settings persist

**Test 8.2: Status Messages**
- **Action:** Perform various operations, observe status bar
- **Expected:**
  - Status updates for: loading, errors, favorites added/removed, search results
  - Messages are clear and informative

**Test 8.3: Keyboard Shortcuts**
- **Actions:**
  - Press 'n' → Toggle notes
  - Type in search → Searches if man page loaded
  - Type in list → Type-ahead navigation
  - Enter in search → Next search result
- **Expected:**
  - All shortcuts work as expected
  - No conflicts between shortcuts

### 9. Error Handling

**Test 9.1: Invalid Man Page**
- **Action:** Try to load "nonexistent-program-xyz"
- **Expected:**
  - Error message displayed
  - No crash
  - Can continue using application

**Test 9.2: Load Man Page While Typing**
- **Action:**
  1. Start typing in notes
  2. Immediately load different program
- **Expected:**
  - Notes auto-saved before switch
  - No data loss

**Test 9.3: Rapid Program Switching**
- **Action:** 
  1. Load "ls"
  2. Immediately load "grep"
  3. Immediately load "sed"
- **Expected:**
  - Each loads correctly
  - No crashes
  - Notes saved properly

### 10. Command-Line Arguments

**Test 10.1: Launch with Program**
```bash
dotnet run -- ls
```
- **Expected:**
  - GMan opens with "ls" man page loaded
  - "ls" selected in program list

**Test 10.2: Launch with Search Term**
```bash
dotnet run -- grep -s "pattern"
```
- **Expected:**
  - GMan opens with "grep" loaded
  - Search box contains "pattern"
  - First match highlighted

**Test 10.3: Launch with --help**
```bash
dotnet run -- --help
```
- **Expected:**
  - GMan opens with help message displayed

## Regression Test Checklist

After any code changes, verify:

- [ ] Application launches without errors
- [ ] Program list populates correctly
- [ ] Can load man pages
- [ ] Search within man page works
- [ ] Can add/remove favorites
- [ ] Notes can be added and persist
- [ ] Type-ahead navigation works
- [ ] Settings can be changed and persist
- [ ] All 199 unit tests pass (`dotnet test`)

## Known Issues / Expected Behavior

- **Control characters:** Some man pages may contain control characters that appear as boxes or special symbols
- **Width calculation:** Initial load may not have perfect width; reloading the page after resize corrects this
- **Type-ahead timing:** 1-second timeout is intentional; typing too slowly resets buffer

## Reporting Issues

When reporting integration test failures:
1. Describe the exact steps to reproduce
2. Include expected vs actual behavior
3. Note your system info (distro, GTK version, .NET version)
4. Include any error messages from console output
5. Mention if issue appeared after specific refactoring phase

## Automated Integration Testing (Future)

For future work, consider:
- **UI testing frameworks:** GTK# test harness or scripted X11 interactions
- **Snapshot testing:** Compare rendered output against golden images
- **Performance testing:** Measure load times for large man pages
- **Accessibility testing:** Screen reader compatibility, keyboard-only navigation
