using Moq;
using System.Windows;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class ConfigGeneratorTest
{
    internal Mock<IMonitorAPI> _monitorApiMock = new();
    internal Mock<IMonitorService> _monitorServiceMock = new();
    internal Mock<IMonitorInputService> _monitorInputServiceMock = new();
    internal Mock<IProgress<ConfigGeneratorStatus>> _progressMock = new();

    [Fact]
    public void Generate_LoadMonitorsThrows()
    {
        _monitorServiceMock
            .Setup(m => m.LoadMonitors())
            .Throws(new Exception("lOaDmOnItOrS tHrOwS"));

        var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        var exception = Assert.Throws<Exception>(() => generator.Generate());

        Assert.Equal("lOaDmOnItOrS tHrOwS", exception.Message);
    }

    [Fact]
    public void Generate_TryLoadMonitorInputsThrows()
    {
        _monitorServiceMock
            .Setup(m => m.LoadMonitors())
            .Returns(new Monitors
            {
                new Monitor { Device = "dEvIcE", Description = "dEsCrIpTiOn", Handle = SafePhysicalMonitorHandle.Null }
            });

        _monitorInputServiceMock
            .Setup(m => m.TryLoadMonitorInputs(
                It.Is<Monitor>(m => m.Device == "dEvIcE"),
                out It.Ref<MonitorInputs?>.IsAny))
            .Throws(new Exception("tRyLoAdMoNiToUtPuTs tHrOwS"));

        var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        var exception = Assert.Throws<Exception>(() => generator.Generate());

        Assert.Equal("tRyLoAdMoNiToUtPuTs tHrOwS", exception.Message);
    }

    [Fact]
    public void Generate_WithProgress()
    {
        _monitorServiceMock
            .Setup(m => m.LoadMonitors())
            .Returns(new Monitors
            {
                new Monitor { Device = "dEvIcE", Description = "dEsCrIpTiOn", Handle = SafePhysicalMonitorHandle.Null }
            });

        MonitorInputs? inputs = new MonitorInputs { SelectedInput = 42, ValidInputs = new List<byte> { 17, 42, 123 } };
        _monitorInputServiceMock
            .Setup(m => m.TryLoadMonitorInputs(It.Is<Monitor>(m => m.Device == "dEvIcE"), out inputs))
            .Returns(true);

        _progressMock
            .Setup(m => m.Report(It.IsAny<ConfigGeneratorStatus>()));

        var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        var text = generator.Generate(_progressMock.Object);

        Assert.StartsWith("# ShadowKVM automatically switches", text);
        Assert.EndsWith("version: 1\n", text);

        _progressMock.Verify(m => m.Report(It.Is<ConfigGeneratorStatus>(s => s.Current == 0 && s.Maximum == 1)));
        _progressMock.Verify(m => m.Report(It.Is<ConfigGeneratorStatus>(s => s.Current == 1 && s.Maximum == 1)));
    }
}
