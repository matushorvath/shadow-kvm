Backlog
=======

- required
   - filter out missing display name, serial number or adapter when generating config
   - add installation and usage to README.md
   - about box, with link to page, licenses
   - check installer behavior when the application is running already

- useful
   - investigate unit tests in C#
   - start application on login
   - ask start the application after installation
   - real configuration UI
      - show which display is which in case they have same names

- nice to have
   - add missing logs (see TODOs in code)
   - parallelize inputs.Load in ConfigGenerator
   - command line params to specify data directory
   - add a way to check for updates

Useful Links
============

Config window triggered from the tray icon:
https://github.com/HavenDV/H.NotifyIcon/tree/master/src/apps/H.NotifyIcon.Apps.Wpf

MCCS 2.2:
https://milek7.pl/ddcbacklight/mccs.pdf

Start after logon:
https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys
https://docs.firegiant.com/wix3/howtos/files_and_registry/write_a_registry_entry/
