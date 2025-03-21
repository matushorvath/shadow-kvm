using Moq;
using Serilog;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class MonitorInputServiceTest
{
    internal Mock<IMonitorAPI> _monitorApiMock = new();
    internal Mock<ILogger> _loggerApiMock = new();

    SafePhysicalMonitorHandle _handle12345;

    public MonitorInputServiceTest()
    {
        _handle12345 = new SafePhysicalMonitorHandle(_monitorApiMock.Object, (HANDLE)12345u, false);
    }

    [Fact]
    public void TryLoadValidInputs_GetCapabilitiesStringLength_ReturnsError()
    {
        uint length;
        _monitorApiMock
            .Setup(m => m.GetCapabilitiesStringLength(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u), out length))
            .Returns(0);

        var service = new MonitorInputService(_monitorApiMock.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(_handle12345, out inputs));
        Assert.Null(inputs);
    }

    [Fact]
    public void TryLoadValidInputs_GetCapabilitiesStringLength_ReturnsZeroLength()
    {
        uint length = 0;
        _monitorApiMock
            .Setup(m => m.GetCapabilitiesStringLength(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u), out length))
            .Returns(1);

        var service = new MonitorInputService(_monitorApiMock.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(_handle12345, out inputs));
        Assert.Null(inputs);
    }

    [Fact]
    public void TryLoadValidInputs_CapabilitiesRequestAndCapabilitiesReply_ReturnsError()
    {
        uint length = 42;
        _monitorApiMock
            .Setup(m => m.GetCapabilitiesStringLength(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u),
                out length))
            .Returns(1);

        _monitorApiMock
            .Setup(m => m.CapabilitiesRequestAndCapabilitiesReply(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u),
                It.IsNotNull<PSTR>(),
                42))
            .Returns(0);

        var service = new MonitorInputService(_monitorApiMock.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(_handle12345, out inputs));
        Assert.Null(inputs);
    }
}
