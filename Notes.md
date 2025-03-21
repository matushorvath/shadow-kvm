Backlog
=======

- required
   - add installation and usage to README.md
   - about box, with link to page, licenses
   - test installer on a machine without .NET 9.0

- useful
   - finish unit tests in C#
   - real configuration UI
   - log version number on startup

- nice to have
   - parallelize inputs.Load in ConfigGenerator
   - add a way to check for updates
   - show which display is which in case they have same names

Build and Test
==============

Install tools:
```sh
dotnet tool install XamlStyler.Console --global
```

Build and test:
```sh
xstyler --recursive --passive --directory . # this currently fails on attribute formatting
dotnet format --verify-no-changes
dotnet build
dotnet test
```

Useful Links
============

Config window triggered from the tray icon:
https://github.com/HavenDV/H.NotifyIcon/tree/master/src/apps/H.NotifyIcon.Apps.Wpf

MCCS 2.2:
https://milek7.pl/ddcbacklight/mccs.pdf

Unit tests:
https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test
https://github.com/devlooped/moq/wiki/Quickstart
https://www.roundthecode.com/dotnet-tutorials/moq-mocking-objects-xunit-dotnet
