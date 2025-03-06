Configuration
=============

- command line params to specify data directory
- use YamlDotNet naming conventions when reading/writing enums
- ConfigGenerator should use enums in the output when possible

Release Automation
==================

- ci/cd, releases
- investigate unit tests in C#

Misc
====

- start application on login
   - https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys
   - https://docs.firegiant.com/wix3/howtos/files_and_registry/write_a_registry_entry/

Configuration UI
================

- list 0x60 valid values in UI, give them string descriptions
- show which display is which in case they have same names
- about box, with link to page, licenses

See https://github.com/HavenDV/H.NotifyIcon/tree/master/src/apps/H.NotifyIcon.Apps.Wpf for example how to trigger a window from the tray icon

MCCS 2.2: https://milek7.pl/ddcbacklight/mccs.pdf
https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys
