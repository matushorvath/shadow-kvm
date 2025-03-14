Backlog
=======

- required
   - add installation and usage to README.md
   - about box, with link to page, licenses
   - test installer on a machine without .NET 9.0
   - only display reload notification if config file time is changed

- useful
   - investigate unit tests in C#
   - real configuration UI

- nice to have
   - parallelize inputs.Load in ConfigGenerator
   - command line params to specify data directory
   - add a way to check for updates
   - show which display is which in case they have same names

Useful Links
============

Config window triggered from the tray icon:
https://github.com/HavenDV/H.NotifyIcon/tree/master/src/apps/H.NotifyIcon.Apps.Wpf

MCCS 2.2:
https://milek7.pl/ddcbacklight/mccs.pdf

Start after logon:
https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys
https://docs.firegiant.com/wix3/howtos/files_and_registry/write_a_registry_entry/
