Configuration Persistence
=========================

- command line params to specify data directory
- generate config file if it does not exist
    - detect if 0x60 looks correct (it has valid values)

Release Automation
==================

- ci/cd, releases
- investigate unit tests in C#

Misc
====

- merge device-class and device-type to device, it's really confusing
- start application on login

Configuration UI
================

- list 0x60 valid values in UI, give them string descriptions
- show which display is which in case they have same names
- about box, with link to page, licenses

See https://github.com/HavenDV/H.NotifyIcon/tree/master/src/apps/H.NotifyIcon.Apps.Wpf for example how to trigger a window from the tray icon

MCCS 2.2: https://milek7.pl/ddcbacklight/mccs.pdf
