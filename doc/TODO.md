# GMAN GTK GRAPHICAL MANUAL VIEWER - TODO, FUTURE PLANS AND BUGS

## Feature implementations

* ~~**Clickable links to other man pages**: Down at the bottom section "SEE ALSO" you should be able to click on them to jump to their man pages directly~~ ✓ Completed
* ~~**Hide non-man page programs** If the option to not view programs that dont have man pages, we should not view them in the list.~~
* ~~**Multi-character type-ahead search**: When user is focused on the programs list and starts typing, it should listen to 1-5 characters if they are typed in~~ ✓ Completed
* ~~When opening the program with a program as argument, it should scroll down to that program in the programs list~~

### New Feauture: Save List OIF Favorite mab pagses

I want to implement a new function for adding favorites to a separate lest. Both the 'All Programs' list and the 'Favorites List' will have a 'Shot list checkbox'
at the top. If it  is checked, that list will show. If both are checked, both lista will show, and favorites will only be so tall (height) as it has items. (This can be added in later if it is complicated). 

- '+' key (or other key if it seems more appropiate) will only be checked for if the main programs list has focus. If it does, it will put the current item in the favorites list aswell. 
- '-' Deletes items from favorites list if it is in focus

When we have all of this completed and tested, we can add a right click context menu for adding or deleting a favorote item.

Ask me any questions you might need clarity in before starting and then create an implementation plan.
## BUGS

* **Opening of alsabat-test runs an annoying alsa sound beep test**:, if the *'run program with --help'* option is enabled.
* ~~**Regexp bug when parsing options in man pages:**: `-virtual-pixel` (from program animate) gets handled incorrectly because of the '-' in the word.~~
