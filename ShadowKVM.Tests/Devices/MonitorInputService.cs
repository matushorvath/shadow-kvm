using System.Runtime.InteropServices;
using System.Text;
using Moq;
using Serilog;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class MonitorInputServiceTest
{
    internal Mock<IMonitorAPI> _monitorApiMock = new();
    internal Mock<ICapabilitiesParser> _capabilitiesParser = new();
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

        var service = new MonitorInputService(_monitorApiMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

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

        var service = new MonitorInputService(_monitorApiMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

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

        var service = new MonitorInputService(_monitorApiMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(_handle12345, out inputs));
        Assert.Null(inputs);
    }

    unsafe void SetPSTR(PSTR pstr, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        for (int i = 0; i < bytes.Length; i++)
        {
            pstr.Value[i] = bytes[i];
        }
        pstr.Value[bytes.Length] = 0;
    }

    [Fact]
    public void TryParseCapabilities_CapabilitiesParser_Throws()
    {
        string capabilities = "cApAbIlItIeS";

        uint length = (uint)capabilities.Length + 1;
        _monitorApiMock
            .Setup(m => m.GetCapabilitiesStringLength(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u),
                out length))
            .Returns(1);

        _monitorApiMock
            .Setup(m => m.CapabilitiesRequestAndCapabilitiesReply(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u),
                It.IsNotNull<PSTR>(),
                (uint)capabilities.Length + 1))
            .Returns(
                (SafeHandle hMonitor, PSTR pszASCIICapabilitiesString, uint dwCapabilitiesStringLengthInCharacters) =>
                {
                    SetPSTR(pszASCIICapabilitiesString, "cApAbIlItIeS");
                    return 1;
                }
            );

        _capabilitiesParser
            .Setup(m => m.Parse("cApAbIlItIeS"))
            .Throws(new Exception("pArSeErRoR"));

        var service = new MonitorInputService(_monitorApiMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        MonitorInputs? inputs = null;
        var exception = Assert.Throws<Exception>(() => service.TryLoadMonitorInputs(_handle12345, out inputs));

        Assert.Equal("pArSeErRoR", exception.Message);
        Assert.Null(inputs);
    }
}
