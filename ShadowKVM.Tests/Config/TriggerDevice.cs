namespace ShadowKVM.Tests;

public class TriggerDeviceTests
{
    static readonly Guid _testGuid = new("d63a8f52-7b7d-4b5a-9f12-e7c3a6d4b8f0");

    [Fact]
    public void Constructor_WithDefaults()
    {
        var triggerDevice = new TriggerDevice();

        Assert.Equal(TriggerDeviceType.Keyboard, triggerDevice.Class.Enum);
        Assert.Null(triggerDevice.VendorId);
        Assert.Null(triggerDevice.ProductId);
        Assert.Null(triggerDevice.LoadedVersion);
    }

    [Fact]
    public void Constructor_WithParameters()
    {
        var triggerDevice = new TriggerDevice { Class = new(_testGuid), VendorId = 12345, ProductId = 9876, LoadedVersion = 56 };

        Assert.Equal(_testGuid, triggerDevice.Class.Raw);
        Assert.Equal(12345, triggerDevice.VendorId);
        Assert.Equal(9876, triggerDevice.ProductId);
        Assert.Equal(56, triggerDevice.LoadedVersion);
    }

    [Fact]
    public void WriteYaml_ThrowsNotImplemented()
    {
        var converter = new TriggerDeviceTypeConverter();

        Assert.Throws<NotImplementedException>(() =>
            converter.WriteYaml(null!, null, typeof(TriggerDevice), null!));
    }
}
