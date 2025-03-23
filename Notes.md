Backlog
=======

- required
   - add installation and usage to README.md
   - about box, with link to page, licenses
      - version:
         GitVersionInformation.ShortSha = "1e9324f"
         GitVersionInformation.SemVer = "0.6.4-small-improvements.1"
         GitVersionInformation.FullSemVer = "0.6.4-small-improvements.1+9",
         GitVersionInformation.InformationalVersion = "0.6.4-small-improvements.1+9.Branch.small-improvements.Sha.1e9324fae87029676c462dd83fb92dbb04cd51d6",
         GitVersionInformation.CommitDate = "2025-03-23",
   - test installer on a machine without .NET 9.0

- useful
   - finish unit tests in C#
   - real configuration UI
   - mark callbacks in UT as .Verifiable() and call Mock.Verify() at the end
   - give up on using internal, switch to public (it just creates problems)

- nice to have
   - parallelize monitorInputService.TryLoadMonitorInputs in ConfigGenerator
   - add a way to check for updates
   - show which display is which in case they have same names
   - chocolatey/winget support

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
