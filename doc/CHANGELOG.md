# Changelog

All notable changes to GMan will be documented in this file.

## [Unreleased]

### Added

* **Clickable man page references**: Program references in the "SEE ALSO" section (e.g.,  `apparmor(7)`,  `aa-stack(8)`) are now clickable links styled in blue with underline. Clicking a reference will:
  + Load that program's man page
  + Find and highlight the program in the program list
  + Scroll to show the selected program

### Technical Details

* Added `manReferenceTag` TextTag for styling clickable man page references
* Added `manPageReferences` dictionary to track clickable regions and their associated program names
* Enhanced `FormatManPage()` to detect man page references in SEE ALSO sections using regex pattern `([a-zA-Z0-9_\-\.]+)\(\d+\)`
* Updated `OnManPageViewClicked()` to handle clicks on man page references and load the corresponding pages
* Added `LoadManPageAndSelect()` helper method to load a man page and select it in the program list
