(work in progress)

# ShadowKVM

ShadowKVM is a Windows application that enhances a basic USB switch to function like a full KVM switch. It automatically switches your monitor inputs based on which computer currently has the USB keyboard connected.

## Use Case

If you have multiple computers connected to the same displays and a USB switch that shares a keyboard and mouse, ShadowKVM will automatically switch your display input when you change the USB switch to a different machine. This removes the need for a manual KVM switch while maintaining the same functionality.

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

## License

MIT
