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

- device changes after a reboot/reconnect, don't use it
   - try to get display serial number or similar
- merge device-class and device-type to device, it's really confusing

Configuration UI
================

- list 0x60 valid values in UI, give them string descriptions
- show which display is which in case they have same names
- about box, with link to page, licenses

See https://github.com/HavenDV/H.NotifyIcon/tree/master/src/apps/H.NotifyIcon.Apps.Wpf for example how to trigger a window from the tray icon

MCCS 2.2: https://milek7.pl/ddcbacklight/mccs.pdf

Monitor Serial Number
=====================

Get-WmiObject WmiMonitorID -Namespace root\wmi |
  ForEach-Object {
    $Manufacturer   = [System.Text.Encoding]::ASCII.GetString($_.ManufacturerName).Trim(0x00)
    $Name           = [System.Text.Encoding]::ASCII.GetString($_.UserFriendlyName).Trim(0x00)
    $Serial         = [System.Text.Encoding]::ASCII.GetString($_.SerialNumberID).Trim(0x00)
    "{0}, {1}, {2}" -f $Manufacturer,$Name,$Serial
  }

https://learn.microsoft.com/en-us/answers/questions/216983/how-to-get-the-serial-number-of-the-monitors-using
https://learn.microsoft.com/en-us/windows/win32/wmicoreprov/wmimonitorid?redirectedfrom=MSDN

https://learn.microsoft.com/en-us/windows/win32/api/_display/
https://learn.microsoft.com/en-us/uwp/api/windows.devices.display.displaymonitor?view=winrt-26100
https://ofekshilon.com/2011/11/13/reading-monitor-physical-dimensions-or-getting-the-edid-the-right-way/
