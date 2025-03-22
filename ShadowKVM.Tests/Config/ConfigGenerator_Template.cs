using Moq;

namespace ShadowKVM.Tests;

public class ConfigGenerator_TemplateTest
{
    internal Mock<IMonitorService> _monitorServiceMock = new();
    internal Mock<IMonitorInputService> _monitorInputServiceMock = new();

    static SafePhysicalMonitorHandle NH = SafePhysicalMonitorHandle.Null; // short name for null handle

    public static List<object[]> TestData => new List<object[]>
    {
        new object[]
        {
            new Monitors
            {
                new Monitor { Device = "dEvIcE 1", Description = "dEsCrIpTiOn 1", Handle = NH },
                new Monitor { Device = "dEvIcE 2", Description = "dEsCrIpTiOn 2", Handle = NH },
            },
            new List<MonitorInputs>
            {
                new MonitorInputs { SelectedInput = 42, ValidInputs = new List<byte> { 17, 42, 123 } },
                new MonitorInputs { SelectedInput = 17, ValidInputs = new List<byte> { 17, 42, 123 } }
            },
            void (string text) =>
            {
                Assert.Contains("dEsCrIpTiOn 1", text);
                Assert.Contains("42", text);
                Assert.Contains("17", text);
                Assert.Contains("123", text);

                Assert.Contains("dEsCrIpTiOn 2", text);
                Assert.Contains("17", text);
                Assert.Contains("42", text);
                Assert.Contains("123", text);
            }
        }
    };

    [Theory]
    [MemberData(nameof(TestData))]
    internal void Generate_CorrectText(Monitors monitors, List<MonitorInputs> monitorInputs, Action<string> textInspector)
    {
        SetupForTemplate(monitors, monitorInputs);

        var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        var text = generator.Generate();

        AssertTextLooksGood(text);
        textInspector(text);
    }

    void SetupForTemplate(Monitors monitors, List<MonitorInputs> monitorInputs)
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
                    return true;
                }
            );
    }

    void AssertTextLooksGood(string text)
    {
        Assert.StartsWith("# ShadowKVM automatically switches", text);
        Assert.EndsWith("version: 1\n", text);

        // TODO make sure it's valid yaml, or even try to load it with ConfigService
    }
}
