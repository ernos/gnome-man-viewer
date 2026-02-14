## Feature implementations

* ~~**Clickable links to other man pages**: Down at the bottom section "SEE ALSO" you should be able to click on them to jump to their man pages directly~~ ✓ Completed

* **Hide non-man page programs** If the option to not view programs that dont have man pages, we should not view them in the list. I dont really know how this would be done though, is there an exdisting list or would we have to check ourselves?
* When user is focused on the programs list and starts typing, it should listen to 1-5 characters if they are typed in
* When opening the program with a program as argument, it should scroll down to that program in the programs list

## BUGS

* Opening of alsabat-test runs an annoying sound beep test, if the *'run program with --help'* option is enabled.
* Regexp bug when parsing options in man pages. `-virtual-pixel` (from program animate) gets handled incorrectly because of the '-' in the word.
