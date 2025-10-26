using System.IO.Abstractions.TestingHelpers;
using Moq;
using Serilog;
using Serilog.Events;
using YamlDotNet.Serialization;

namespace ShadowKVM.Tests;

// TODO invalid yaml strings in description, adapter, serial - are they correctly escaped?

// In some cases xUnit will decide to collapse a theory into a single test case,
// which makes the VS Code "Testing" sidebar bug out and not show test results for that theory.
//
// Specifically the types used by theory data need to be serializable (by xUnit definition of the term).
// It seems classes are not considered serializable, so we use a workaround to pass data to theory.
//
// See also:
// - xUnit SerializationHelper.Instance.IsSerializable
// - https://github.com/xunit/xunit/blob/3c10d5b009e6142c722364a04d251a29c678d25f/src/xunit.v3.core/Framework/TheoryDiscoverer.cs#L193
// - https://github.com/xunit/xunit/issues/547

public class ConfigGenerator_TemplateTests
{
    public Mock<IMonitorService> _monitorServiceMock = new();
    public Mock<IMonitorInputService> _monitorInputServiceMock = new();
    public Mock<ILogger> _loggerMock = new();

    static SafePhysicalMonitorHandle NH = SafePhysicalMonitorHandle.Null; // short name for null handle

    // Workaround for broken VS Code behavior when using theory data with objects (see above):
    // Save the actual theory data in a dictionary, pass dictionary keys to the theory.

    record TestDatum(Monitors monitors, MonitorInputs?[] monitorInputs, string expectedFragment);

    static Dictionary<string, TestDatum> TestData => new()
    {
        ["all codes"] = new(
            new Monitors(),
            [],
            """
            # Supported command code strings are:
            #   input-select (96)
            """
        ),
        ["all values"] = new(
            new Monitors(),
            [],
            """
            # Supported command value strings are:
            #   analog1 (1) analog2 (2) dvi1 (3) dvi2 (4) composite1 (5) composite2 (6) s-video1 (7)
            #   s-video2 (8) tuner1 (9) tuner2 (10) tuner3 (11) component1 (12) component2 (13)
            #   component3 (14) display-port1 (15) display-port2 (16) hdmi1 (17) hdmi2 (18)
            """
        ),
        ["no monitors"] = new(
            new Monitors(),
            [],
            """
            monitors:
            #
            """
        ),
        ["monitor with no adapter or serial"] = new(
            new Monitors()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = NH }
            },
            [
                new() { SelectedInput = 42, ValidInputs = new() { 42, 123 } }
            ],
            """
            monitors:
              - description: dEsCrIpTiOn 1
                attach:
                  code: input-select
                  value: 42
                detach:
                  code: input-select
                  value: 123

            """
        ),
        ["monitor with adapter but no serial"] = new(
            new Monitors()
            {
                new() { Description = "dEsCrIpTiOn 1", Adapter = "aDaPtEr 1", Handle = NH }
            },
            [
                new() { SelectedInput = 42, ValidInputs = new() { 42, 123 } }
            ],
            """
            monitors:
              - description: dEsCrIpTiOn 1
                adapter: aDaPtEr 1
                attach:
                  code: input-select
                  value: 42
                detach:
                  code: input-select
                  value: 123

            """
        ),
        ["monitor with serial but no adapter"] = new(
            new Monitors()
            {
                new() { Description = "dEsCrIpTiOn 1", SerialNumber = "sErIaL 1", Handle = NH }
            },
            [
                new() { SelectedInput = 42, ValidInputs = new() { 42, 123 } }
            ],
            """
            monitors:
              - description: dEsCrIpTiOn 1
                serial-number: sErIaL 1
                attach:
                  code: input-select
                  value: 42
                detach:
                  code: input-select
                  value: 123

            """
        ),
        ["monitor with adapter and serial"] = new(
            new Monitors()
            {
                new() { Description = "dEsCrIpTiOn 1", Adapter = "aDaPtEr 1", SerialNumber = "sErIaL 1", Handle = NH }
            },
            [
                new() { SelectedInput = 42, ValidInputs = new() { 42, 123 } }
            ],
            """
            monitors:
              - description: dEsCrIpTiOn 1
                adapter: aDaPtEr 1
                serial-number: sErIaL 1
                attach:
                  code: input-select
                  value: 42
                detach:
                  code: input-select
                  value: 123

            """
        ),
        ["inputs are enum values"] = new(
            new Monitors()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = NH }
            },
            [
                new() { SelectedInput = 0x0f, ValidInputs = new() { 0x0f, 0x12 } }
            ],
            """
            monitors:
              - description: dEsCrIpTiOn 1
                attach:
                  code: input-select
                  value: display-port1
                detach:
                  code: input-select
                  value: hdmi2

            """
        ),
        ["monitor with no inputs"] = new(
            new Monitors()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = NH }
            },
            [
                null
            ],
            """
            monitors:
            # - description: dEsCrIpTiOn 1
            #   attach:
            #     code: input-select
            #     value: # warning: no input sources found for this monitor
            #   detach:
            #     code: input-select
            #     value: # warning: no input sources found for this monitor

            """
        ),
        ["monitor with single input"] = new(
            new Monitors()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = NH }
            },
            [
                new() { SelectedInput = 42, ValidInputs = new() { 42 } }
            ],
            """
            monitors:
              - description: dEsCrIpTiOn 1
                attach:
                  code: input-select
                  value: 42
                detach:
                  code: input-select
                  value: 42    # warning: only one input source found for this monitor

            """
        ),
        ["monitor with two inputs"] = new(
            new Monitors()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = NH }
            },
            [
                new() { SelectedInput = 42, ValidInputs = new() { 42, 123 } }
            ],
            """
            monitors:
              - description: dEsCrIpTiOn 1
                attach:
                  code: input-select
                  value: 42
                detach:
                  code: input-select
                  value: 123

            """
        ),
        ["monitor with multiple inputs"] = new(
            new Monitors()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = NH }
            },
            [
                new() { SelectedInput = 42, ValidInputs = new() { 0x07, 42, 123, 0x11 } }
            ],
            """
            monitors:
              - description: dEsCrIpTiOn 1
                attach:
                  code: input-select
                  value: 42
                detach:
                  code: input-select
                  value: s-video1    # other options: 123 hdmi1

            """
        ),
        ["multiple monitors"] = new( // this test datum is used in multiple tests
            new Monitors()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = NH },
                new() { Description = "dEsCrIpTiOn 2", Adapter = "aDaPtEr 2", Handle = NH },
                new() { Description = "dEsCrIpTiOn 3", SerialNumber = "sErIaL 3", Handle = NH }
            },
            [
                new() { SelectedInput = 42, ValidInputs = new() { 0x0e, 42, 123, 0x03 } },
                null,
                new() { SelectedInput = 0x01, ValidInputs = new() { 0x01 } }
            ],
            """
            monitors:
              - description: dEsCrIpTiOn 1
                attach:
                  code: input-select
                  value: 42
                detach:
                  code: input-select
                  value: component3    # other options: 123 dvi1

            # - description: dEsCrIpTiOn 2
            #   adapter: aDaPtEr 2
            #   attach:
            #     code: input-select
            #     value: # warning: no input sources found for this monitor
            #   detach:
            #     code: input-select
            #     value: # warning: no input sources found for this monitor

              - description: dEsCrIpTiOn 3
                serial-number: sErIaL 3
                attach:
                  code: input-select
                  value: analog1
                detach:
                  code: input-select
                  value: analog1    # warning: only one input source found for this monitor

            """
        )
    };

    public static TheoryData<string> TestDataKeys => [.. TestData.Keys.AsEnumerable()];

    [Theory, MemberData(nameof(TestDataKeys))]
    public void Generate_CorrectText(string testDataKey)
    {
        var (monitors, monitorInputs, expectedFragment) = TestData[testDataKey];

        SetupForTemplate(monitors, monitorInputs);

        var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        var text = generator.Generate();

        Assert.Contains(expectedFragment.ReplaceLineEndings(), text);
    }

    [Fact]
    public void Generate_NoLineEndWhiteSpace()
    {
        var (monitors, monitorInputs, expectedFragment) = TestData["multiple monitors"];

        SetupForTemplate(monitors, monitorInputs);

        var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        var text = generator.Generate();

        // No lines should end in whitespace
        Assert.All(text.Split(Environment.NewLine), line => Assert.DoesNotMatch(@"\s$", line));
    }

    [Fact]
    public void Generate_StartAndEnd()
    {
        var (monitors, monitorInputs, expectedFragment) = TestData["multiple monitors"];

        SetupForTemplate(monitors, monitorInputs);

        var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        var text = generator.Generate();

        Assert.StartsWith("# ShadowKVM automatically switches", text);
        Assert.EndsWith($"version: 2{Environment.NewLine}", text);
    }

    [Fact]
    public void Generate_IsYaml()
    {
        var (monitors, monitorInputs, expectedFragment) = TestData["multiple monitors"];

        SetupForTemplate(monitors, monitorInputs);

        var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        var text = generator.Generate();

        // Load the config from a mock file system
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = text
        });

        using (var stream = fileSystem.File.OpenRead(@"x:\mOcKfS\config.yaml"))
        using (var input = new StreamReader(stream))
        {
            var deserializer = new DeserializerBuilder().Build();
            deserializer.Deserialize(input);
        }
    }

    [Fact]
    public void Generate_CanBeLoaded()
    {
        var (monitors, monitorInputs, expectedFragment) = TestData["multiple monitors"];

        SetupForTemplate(monitors, monitorInputs);

        var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        var text = generator.Generate();

        // Load the config from a mock file system
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = text
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Equal(2, configService.Config.Version);
        Assert.Equal(TriggerDeviceType.Keyboard, configService.Config.TriggerDevice.Class.Enum);
        Assert.Equal(LogEventLevel.Information, configService.Config.LogLevel);

        Assert.NotNull(configService.Config.Monitors);
        Assert.Collection(configService.Config.Monitors,
        monitor =>
        {
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);

            Assert.NotNull(monitor.Attach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Attach.Code.Enum);
            Assert.Equal(42, monitor.Attach.Value.Raw);

            Assert.NotNull(monitor.Detach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Detach.Code.Enum);
            Assert.Equal(VcpValueEnum.Component3, monitor.Detach.Value.Enum);
        },
        monitor =>
        {
            Assert.Equal("dEsCrIpTiOn 3", monitor.Description);
            Assert.Equal("sErIaL 3", monitor.SerialNumber);

            Assert.NotNull(monitor.Attach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Attach.Code.Enum);
            Assert.Equal(VcpValueEnum.Analog1, monitor.Attach.Value.Enum);

            Assert.NotNull(monitor.Detach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Detach.Code.Enum);
            Assert.Equal(VcpValueEnum.Analog1, monitor.Detach.Value.Enum);
        });
    }

    void SetupForTemplate(Monitors monitors, MonitorInputs?[] monitorInputs)
    {
        _monitorServiceMock
            .Setup(m => m.LoadMonitors())
            .Returns(monitors);

        var loadMonitorInputsInvocation = 0;
        MonitorInputs? inputs;

        _monitorInputServiceMock
            .Setup(m => m.TryLoadMonitorInputs(It.IsAny<Monitor>(), out inputs))
            .Returns(
                (Monitor monitor, out MonitorInputs? inputs) =>
                {
                    inputs = monitorInputs[loadMonitorInputsInvocation++];
                    return inputs != null;
                }
            );
    }
}
