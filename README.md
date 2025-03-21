(work in progress)

# Shadow KVM

Shadow KVM is a Windows application that enhances a basic USB switch to function like a full KVM switch. It automatically switches your monitor inputs based on which computer currently has the USB keyboard connected.

## Use Case

If you have multiple computers connected to the same displays and a USB switch that shares a keyboard and mouse, Shadow KVM will automatically switch your display input when you change the USB switch to a different machine. This removes the need for a manual KVM switch while maintaining the same functionality.

## Installation & Usage

(TODO: Add installation steps, requirements, and setup instructions)

## Requirements

- Windows 8.0 or newer
- Monitors that support DDC/CI for input switching
- A USB switch with a connected keyboard (and optionally a mouse)

## How It Works

- Listens for USB keyboard connections using the Windows `CM_Register_Notification` API.
- When a new keyboard is detected, it determines which computer is currently active.
- Uses the DDC/CI protocol (`SetVCPFeature`) to switch the display input to the corresponding computer.
- Ensures seamless transitions between multiple machines connected to the same monitors.

## Third-Party Work

This project uses [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) to display a tray icon.
The tray icon handling code is partly based on an
[example](https://github.com/HavenDV/H.NotifyIcon/tree/master/src/apps/H.NotifyIcon.Apps.Wpf)
that is part of H.NotifyIcon source code.

This project includes the [Shadow Icon](https://icon-icons.com/icon/shadow/264912) by Mingcute,
licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
The icon was modified by adjusting the color to white/gray.

## Release Process

1. Prepare the release in `main` branch.
1. Tag the commit with the version number.
   ```sh
   $ git tag v0.1.0 main
   ```
1. Push the tag to GitHub.
   ```sh
   $ git push origin tag v0.1.0
   ```
1. GitHub Actions will run and prepare a release draft for the tag.
1. Edit the release draft to add release notes and publish the release.

## License

[MIT License](https://opensource.org/license/mit)  
(see [NOTICE.md](Installer/Notice.md) for details)
