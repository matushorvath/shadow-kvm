[![Build and Test](https://github.com/matushorvath/shadow-kvm/actions/workflows/build.yml/badge.svg)](https://github.com/matushorvath/shadow-kvm/actions/workflows/build.yml)
[![GitHub Release](https://img.shields.io/github/v/release/matushorvath/shadow-kvm?display_name=release)](https://github.com/matushorvath/shadow-kvm/releases/latest)

<img src="Documents/Application.png" width="50px" height="50px" align="right">

# Shadow KVM

Shadow KVM is a Windows application that transforms a basic USB switch into a full KVM switch.
It automatically switches your monitor inputs based on which computer currently has the keyboard or mouse connected.

## Use Case

Do you have multiple computers sharing the same display and a USB switch for your keyboard and mouse?
Shadow KVM automatically switches your display input when you toggle the USB switch to a different machine.

<!-- ![Hardware Connection Diagram](Documents/Connections.png) -->
<img src="Documents/Connections.png" width="33%" height="33%" alt="Hardware Connection Diagram">

## Installation & Usage

Download and install the latest [ShadowKVM release](https://github.com/matushorvath/shadow-kvm/releases/latest). When you start Shadow KVM for the first time, it will prompt you to generate a configuration file:

<img src="Documents/FirstRun%20-%20CreateConfig.png" width="33%" height="33%" alt="First Run - Create Config">

The configuration file is pre-populated with your currently connected monitors to give you a starting point. Shadow KVM will open the config file in a text editor:

<img src="Documents/FirstRun%20-%20ConfigFile.png" width="50%" height="50%" alt="First Run - Config File">

The config file includes helpful comments explaining each setting.
Once you've finished editing, save and close the editor. Shadow KVM will automatically reload the configuration.

### Trigger Device

First, configure the `trigger-device` property. Shadow KVM switches monitor inputs whenever this device connects or disconnects.

The default is `keyboard`, meaning monitor inputs switch each time a keyboard connects to or disconnects from your computer.

```yaml
trigger device:
    class: keyboard
```

You can use a mouse instead, or specify a particular keyboard or mouse by Vendor ID and Product ID. This is useful when multiple keyboards or mice are attached and you only want one to trigger Shadow KVM. See the config file for additional details.

```yaml
trigger device:
    class: mouse
    vendor-id: 2f68
```

You can find vendor and product IDs in the Shadow KVM logs (see below).

### Monitors

Next, configure the `monitors` section. Shadow KVM pre-fills this based on your attached monitors.

You need to specify which monitor input each computer connects to using the `attach` and `detach` sections:

```yaml
monitors: 
  - description: Dell S2722QC(HDMI1)
    adapter: NVIDIA GeForce RTX 3050
    serial-number: 7XTBLZ3
    attach:
      code: input-select
      value: hdmi1
    detach:
      code: input-select
      value: 27    # other options: analog1, hdmi2
```

In this example, when your trigger device (keyboard or mouse) connects to the computer, Shadow KVM switches the monitor to HDMI1. When the trigger device disconnects, Shadow KVM switches to input "27".

Other supported inputs are listed in the "other options" comment. Edit the `value` setting for both `attach` and `detach` to match the monitor inputs where your computers are connected.

The "27" input doesn't have a friendly name like "hdmi2" or "analog1". This can happen with newer input types that don't yet have human-readable names in the DDC/CI standard. If you see a numbered input, check your monitor's OSD menu to map it to the detected values.

For example, in the case above, if the monitor's OSD menu shows VGA, HDMI1, HDMI2, and USB-C inputs, then analog1 likely refers to VGA, hdmi1 and hdmi2 map to HDMI1 and HDMI2, and 27 must be the USB-C input since it's the only unmapped option. Your monitor may have completely different inputs and numbering—always check your specific monitor's OSD menu and compare it with the values Shadow KVM detects.

### Controlling Shadow KVM

Shadow KVM displays an icon in the system tray with a menu for quick access:

<img src="Documents/Tray%20-%20Menu.png" width="50%" height="50%" alt="Tray - Menu">

You can temporarily disable or enable Shadow KVM. When disabled, it won't react to device connections or disconnections. You can also configure it to start automatically at logon, and view or edit the config file.

### Additional Information

Shadow KVM supports multiple monitors if you have more than one display connected to your computers. Currently, only one trigger device is supported.

**Config file location:** `C:\Users\<username>\AppData\Roaming\Shadow KVM\config.yaml`  
**Log files** are stored in the same directory and contain useful troubleshooting information.

To regenerate the default config file, exit Shadow KVM, delete the existing config file, and restart the application. It will prompt you to generate a new one.

## Requirements

- Windows 10.0 or newer
- Monitors with DDC/CI support for input switching
- A USB switch with a connected keyboard and/or mouse

## How It Works

- Listens for USB keyboard connections using the Windows `CM_Register_Notification` API
- Detects when a new keyboard is connected to determine which computer is active
- Uses the DDC/CI protocol (`SetVCPFeature`) to switch the display input to the corresponding computer
- Ensures seamless transitions between multiple machines sharing the same monitors

## Third-Party Work

This project uses [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) to display a tray icon.
The tray icon implementation is partly based on an
[example](https://github.com/HavenDV/H.NotifyIcon/tree/master/src/apps/H.NotifyIcon.Apps.Wpf)
from the H.NotifyIcon source code.

This project includes the [Shadow Icon](https://icon-icons.com/icon/shadow/264912) by Mingcute,
licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
The icon colors have been modified.

## Release Process

1. Prepare the release in the `main` branch
1. Tag the commit with the version number:
   ```sh
   $ git tag v0.1.0 main
   ```
1. Push the tag to GitHub:
   ```sh
   $ git push origin tag v0.1.0
   ```
1. GitHub Actions will create a release draft for the tag
1. Edit the release draft, add release notes, and publish

## License

[MIT License](https://opensource.org/license/mit)  
(see [NOTICE.md](Installer/Notice.md) for details)
