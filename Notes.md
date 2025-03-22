Backlog
=======

- required
   - clean the project before building the installer, to avoid packing in the tests
   - add installation and usage to README.md
   - about box, with link to page, licenses
   - test installer on a machine without .NET 9.0

- useful
   - finish unit tests in C#
   - real configuration UI
   - log version number on startup
   - use a color icon for the executable and the installer

- nice to have
   - parallelize monitorInputService.TryLoadMonitorInputs in ConfigGenerator
   - add a way to check for updates
   - show which display is which in case they have same names
   - mark callbacks in UT as .Verifiable() and call Mock.Verify() at the end

Build and Test
==============

Install tools:
```sh
# dotnet tool install --global XamlStyler.Console
# dotnet tool install --global  wix
```

Build and test:
```sh
# this insists on formatting the attributes in a way I don't like
# xstyler --recursive --passive --directory .

# this actually makes the source worse
# wix format Installer/*.wxs

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
