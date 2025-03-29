Backlog
=======

- required
   - !!! fix autostart, currently it opens dependency walker
   - add installation and usage to README.md
   - about box, with link to page, licenses
      - version:
         GitVersionInformation.ShortSha = "1e9324f"
         GitVersionInformation.SemVer = "0.6.4-small-improvements.1"
         GitVersionInformation.FullSemVer = "0.6.4-small-improvements.1+9",
         GitVersionInformation.InformationalVersion = "0.6.4-small-improvements.1+9.Branch.small-improvements.Sha.1e9324fae87029676c462dd83fb92dbb04cd51d6",
         GitVersionInformation.CommitDate = "2025-03-23",
      - icon? name, version, author, license + copyright, OK button, title "About ShadowKVM", Apache license mention, link to github releases, to readme?
         - check for latest version? "<version xyz> is available online"?
   - test installer on a machine without .NET 9.0
      - test on windows 7 and 8?

- useful
   - finish unit tests in C#
   - real configuration UI
   - installer takes forever to kill running ShadowKVM.exe
      - maybe it's sending WM_CLOSE to wrong windows?
   - dependabot skips updating many dependencies because of errors like this:  
     The package Microsoft.NET.Test.Sdk.17.13.0 is not compatible. Incompatible project frameworks: net9.0-windows8.0

- nice to have
   - parallelize monitorInputService.TryLoadMonitorInputs in ConfigGenerator
   - add a way to check for updates
   - show which display is which in case they have same names
   - winget support
   - the "config reloaded succesfully" window does not have focus and sometimes opens in background
   - always install to C:\Program Files, both on 64-bit and 32-bit systems
   - dependabot support for Installer
       - Directory.Packages.props? https://devblogs.microsoft.com/nuget/introducing-central-package-management/
       - or file.proj? https://github.com/search?q=repo%3Adependabot%2Fdependabot-core+csproj&type=code

Build and Test
==============

Install tools:
```sh
# dotnet tool install --global XamlStyler.Console
# dotnet tool install --global wix
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
https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/MSBuildIntegration.md
