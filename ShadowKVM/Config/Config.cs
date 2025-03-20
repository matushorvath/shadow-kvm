using Serilog.Events;

namespace ShadowKVM;

internal class Config
{
    public int Version { get; set; }
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;
    public TriggerDevice TriggerDevice { get; set; } = new TriggerDevice(TriggerDeviceType.Keyboard);

    public List<MonitorConfig>? Monitors { get; set; }

    // MD5 checksum of the loaded configuration file
    public byte[]? LoadedChecksum { get; set; }
}

internal class MonitorConfig
{
    public string? Description { get; set; }
    public string? Adapter { get; set; }
    public string? SerialNumber { get; set; }

    public ActionConfig? Attach { get; set; }
    public ActionConfig? Detach { get; set; }
}

public enum VcpCodeEnum : byte
{
    InputSelect = 0x60
}

public enum VcpValueEnum : byte
{
    Analog1 = 0x01,
    Analog2 = 0x02,
    Dvi1 = 0x03,
    Dvi2 = 0x04,
    Composite1 = 0x05,
    Composite2 = 0x06,
    SVideo1 = 0x07,
    SVideo2 = 0x08,
    Tuner1 = 0x09,
    Tuner2 = 0x0A,
    Tuner3 = 0x0B,
    Component1 = 0x0C,
    Component2 = 0x0D,
    Component3 = 0x0E,
    DisplayPort1 = 0x0F,
    DisplayPort2 = 0x10,
    Hdmi1 = 0x11,
    Hdmi2 = 0x12
}

internal class ActionConfig
{
    public OpenEnumByte<VcpCodeEnum> Code { get; set; } = new OpenEnumByte<VcpCodeEnum>();
    public OpenEnumByte<VcpValueEnum> Value { get; set; } = new OpenEnumByte<VcpValueEnum>();
}
