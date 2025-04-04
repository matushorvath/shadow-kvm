Backlog
=======

- required
   - add installation and usage to README.md
   - test installer on a machine without .NET 9.0

- useful
   - finish unit tests in C#
   - real configuration UI
   - installer takes forever to kill running ShadowKVM.exe
      - maybe it's sending WM_CLOSE to wrong windows?

- nice to have
   - parallelize monitorInputService.TryLoadMonitorInputs in ConfigGenerator
   - add a way to check for updates
      - perhaps add to about box: "<version xyz> is available online"?
   - show which display is which in case they have same names
   - winget support
   - the "config reloaded succesfully" window does not have focus and sometimes opens in background
   - always install to C:\Program Files, both on 64-bit and 32-bit systems
   - decide about <xxx/> vs <xxx /> in XML
   - VS Code wants the System imports first in source files
   - apply windows 11 design guidelines
      - https://learn.microsoft.com/en-us/windows/apps/design/
      - dark mode

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
