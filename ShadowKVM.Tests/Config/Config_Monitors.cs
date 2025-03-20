using Serilog.Events;
using System.IO.Abstractions.TestingHelpers;

namespace ShadowKVM.Tests;

public class ConfigMonitorsTests
{
    [Fact]
    public void LoadConfig_ThrowsWithMissingMonitors()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigException>(configService.LoadConfig);

        Assert.Equal(@"At least one monitor needs to be specified in configuration", exception.Message);
    }

    [Fact]
    public void LoadConfig_ThrowsWithZeroMonitors()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigException>(configService.LoadConfig);

        Assert.Equal(@"At least one monitor needs to be specified in configuration", exception.Message);
    }

    [Fact]
    public void LoadConfig_ThrowsWithNoMonitorId()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - attach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigException>(configService.LoadConfig);

        Assert.Equal(@"Either description, adapter or serial-number needs to be specified for each monitor", exception.Message);
    }

    [Fact]
    public void LoadConfig_LoadsMonitorWithDescription()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - description: dEsCrIpTiOn 1
                    attach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Null(monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
        });
    }

    [Fact]
    public void LoadConfig_LoadsMonitorWithAdapter()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - adapter: aDaPtEr 1
                    attach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.Null(monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
        });
    }

    [Fact]
    public void LoadConfig_LoadsMonitorWithSerialNumber()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - serial-number: sErIaLnUmBeR 1
                    attach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.Null(monitor.Description);
            Assert.Null(monitor.Adapter);
            Assert.Equal("sErIaLnUmBeR 1", monitor.SerialNumber);
        });
    }

    [Fact]
    public void LoadConfig_LoadsMonitorWithAllIds()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - description: dEsCrIpTiOn 1
                    adapter: aDaPtEr 1
                    serial-number: sErIaLnUmBeR 1
                    attach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Equal("sErIaLnUmBeR 1", monitor.SerialNumber);
        });
    }

    [Fact]
    public void LoadConfig_LoadsComplexMonitors()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                trigger-device: mouse
                monitors:
                  - serial-number: sErIaLnUmBeR 1
                    adapter: aDaPtEr 1
                    attach:
                      code: input-select
                      value: hdmi1
                    detach:
                      code: 0x99
                      value: display-port1
                  - adapter: aDaPtEr 2
                    detach:
                      code: input-select
                      value: 123
                    description: dEsCrIpTiOn 2
                  - attach:
                      value: s-video2
                      code: 210
                    serial-number: sErIaLnUmBeR 3
                    description: dEsCrIpTiOn 3
                log-level: fatal
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Equal(1, config.Version);
        Assert.Equal(LogEventLevel.Fatal, config.LogLevel);

        Assert.Equal(TriggerDeviceType.Mouse, config.TriggerDevice.Enum);
        Assert.Equal(new Guid("{378DE44C-56EF-11D1-BC8C-00A0C91405DD}"), config.TriggerDevice.Raw);

        Assert.Collection(config.Monitors ?? [],
        monitor =>
        {
            Assert.Null(monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Equal("sErIaLnUmBeR 1", monitor.SerialNumber);

            Assert.NotNull(monitor.Attach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Attach.Code.Enum);
            Assert.Equal(0x60, monitor.Attach.Code.Raw);
            Assert.Equal(VcpValueEnum.Hdmi1, monitor.Attach.Value.Enum);
            Assert.Equal(0x11, monitor.Attach.Value.Raw);

            Assert.NotNull(monitor.Detach);
            Assert.Null(monitor.Detach.Code.Enum);
            Assert.Equal(0x99, monitor.Detach.Code.Raw);
            Assert.Equal(VcpValueEnum.DisplayPort1, monitor.Detach.Value.Enum);
            Assert.Equal(0x0f, monitor.Detach.Value.Raw);
        },
        monitor =>
        {
            Assert.Equal("dEsCrIpTiOn 2", monitor.Description);
            Assert.Equal("aDaPtEr 2", monitor.Adapter);
            Assert.Null(monitor.SerialNumber);

            Assert.Null(monitor.Attach);

            Assert.NotNull(monitor.Detach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Detach.Code.Enum);
            Assert.Equal(0x60, monitor.Detach.Code.Raw);
            Assert.Null(monitor.Detach.Value.Enum);
            Assert.Equal(123, monitor.Detach.Value.Raw);
        },
        monitor =>
        {
            Assert.Equal("dEsCrIpTiOn 3", monitor.Description);
            Assert.Null(monitor.Adapter);
            Assert.Equal("sErIaLnUmBeR 3", monitor.SerialNumber);

            Assert.NotNull(monitor.Attach);
            Assert.Null(monitor.Attach.Code.Enum);
            Assert.Equal(210, monitor.Attach.Code.Raw);
            Assert.Equal(VcpValueEnum.SVideo2, monitor.Attach.Value.Enum);
            Assert.Equal(0x08, monitor.Attach.Value.Raw);

            Assert.Null(monitor.Detach);
        });
    }
}
