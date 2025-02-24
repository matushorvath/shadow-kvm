Configuration Persistence
=========================

- command line params to specify config file
- generate config file if it does not exist
    - detect if 0x60 looks correct (it has valid values)

Release Automation
==================

- installer (include Apache 2.0 license because icon)
- ci/cd, releases
- investigate unit tests in C#

Misc
====

- log file, add log messages everywhere

Configuration UI
================

- list 0x60 valid values in UI, give them string descriptions
- show which display is which in case they have same names
- about box, with link to page, licenses

See https://github.com/HavenDV/H.NotifyIcon/tree/master/src/apps/H.NotifyIcon.Apps.Wpf for example how to trigger a window from the tray icon
