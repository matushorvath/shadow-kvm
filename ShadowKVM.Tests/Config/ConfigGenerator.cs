using Moq;
using System.Windows;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class ConfigGeneratorTest
{
    internal Mock<IMonitorAPI> _monitorApiMock = new();
    internal Mock<IMonitorService> _monitorServiceMock = new();
    internal Mock<IMonitorInputService> _monitorInputServiceMock = new();

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
}
