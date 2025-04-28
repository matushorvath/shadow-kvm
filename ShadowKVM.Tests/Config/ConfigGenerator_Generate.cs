using Moq;

namespace ShadowKVM.Tests;

public class ConfigGenerator_GenerateTests
{
    public Mock<IMonitorService> _monitorServiceMock = new();
    public Mock<IMonitorInputService> _monitorInputServiceMock = new();
    public Mock<IProgress<ConfigGeneratorStatus>> _progressMock = new();

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
                new() { Description = "dEsCrIpTiOn", Handle = SafePhysicalMonitorHandle.Null }
            });

        _monitorInputServiceMock
            .Setup(m => m.TryLoadMonitorInputs(
                It.Is<Monitor>(m => m.Description == "dEsCrIpTiOn"),
                out It.Ref<MonitorInputs?>.IsAny))
            .Throws(new Exception("tRyLoAdMoNiToUtPuTs tHrOwS"));

        var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        var exception = Assert.Throws<Exception>(() => generator.Generate());

        Assert.Equal("tRyLoAdMoNiToUtPuTs tHrOwS", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Generate_Succeeds(bool withProgress)
    {
        _monitorServiceMock
            .Setup(m => m.LoadMonitors())
            .Returns(new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = SafePhysicalMonitorHandle.Null },
                new() { Description = "dEsCrIpTiOn 2", Handle = SafePhysicalMonitorHandle.Null }
            });

        MonitorInputs? inputs = new() { SelectedInput = 42, ValidInputs = new() { 17, 42, 123 } };
        _monitorInputServiceMock
            .Setup(m => m.TryLoadMonitorInputs(It.IsAny<Monitor>(), out inputs))
            .Returns(true);

        _progressMock
            .Setup(m => m.Report(It.IsAny<ConfigGeneratorStatus>()));

        var generator = new ConfigGenerator(_monitorServiceMock.Object, _monitorInputServiceMock.Object);
        var text = generator.Generate(withProgress ? _progressMock.Object : null);

        Assert.StartsWith("# ShadowKVM automatically switches", text);
        Assert.EndsWith($"version: 1{Environment.NewLine}", text);

        _monitorInputServiceMock.Verify(m => m.TryLoadMonitorInputs(It.Is<Monitor>(m => m.Description == "dEsCrIpTiOn 1"), out inputs));
        _monitorInputServiceMock.Verify(m => m.TryLoadMonitorInputs(It.Is<Monitor>(m => m.Description == "dEsCrIpTiOn 2"), out inputs));

        if (withProgress)
        {
            _progressMock.Verify(m => m.Report(It.Is<ConfigGeneratorStatus>(s => s.Current == 0 && s.Maximum == 2)));
            _progressMock.Verify(m => m.Report(It.Is<ConfigGeneratorStatus>(s => s.Current == 1 && s.Maximum == 2)));
            _progressMock.Verify(m => m.Report(It.Is<ConfigGeneratorStatus>(s => s.Current == 2 && s.Maximum == 2)));
        }
    }
}
