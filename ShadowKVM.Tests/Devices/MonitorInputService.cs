using System.Runtime.InteropServices;
using System.Text;
using Moq;
using Serilog;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class MonitorInputServiceTest
{
    public Mock<IWindowsAPI> _windowsAPIMock = new();
    public Mock<ICapabilitiesParser> _capabilitiesParser = new();
    public Mock<ILogger> _loggerApiMock = new();

    SafePhysicalMonitorHandle _handle12345;

    public MonitorInputServiceTest()
    {
        _handle12345 = new SafePhysicalMonitorHandle(_windowsAPIMock.Object, (HANDLE)12345u, false);
    }

    [Fact]
    public void Constructor_FromMonitor()
    {
        uint length;
        _windowsAPIMock
            .Setup(m => m.GetCapabilitiesStringLength(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u), out length))
            .Returns(0)
            .Verifiable();

        var service = new MonitorInputService(_windowsAPIMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        var monitor = new Monitor { Handle = _handle12345, Description = "" };
        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(monitor, out inputs));

        _windowsAPIMock.Verify();
    }

    [Fact]
    public void TryLoadValidInputs_GetCapabilitiesStringLength_ReturnsError()
    {
        uint length;
        _windowsAPIMock
            .Setup(m => m.GetCapabilitiesStringLength(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u), out length))
            .Returns(0);

        var service = new MonitorInputService(_windowsAPIMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(_handle12345, out inputs));
        Assert.Null(inputs);
    }

    [Fact]
    public void TryLoadValidInputs_GetCapabilitiesStringLength_ReturnsZeroLength()
    {
        uint length = 0;
        _windowsAPIMock
            .Setup(m => m.GetCapabilitiesStringLength(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u), out length))
            .Returns(1);

        var service = new MonitorInputService(_windowsAPIMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(_handle12345, out inputs));
        Assert.Null(inputs);
    }

    [Fact]
    public void TryLoadValidInputs_CapabilitiesRequestAndCapabilitiesReply_ReturnsError()
    {
        uint length = 42;
        _windowsAPIMock
            .Setup(m => m.GetCapabilitiesStringLength(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u),
                out length))
            .Returns(1);

        _windowsAPIMock
            .Setup(m => m.CapabilitiesRequestAndCapabilitiesReply(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u),
                It.IsNotNull<PSTR>(),
                42))
            .Returns(0);

        var service = new MonitorInputService(_windowsAPIMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

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

    void SetupForCapabilitiesParser(string capabilities)
    {
        uint length = (uint)capabilities.Length + 1;
        _windowsAPIMock
            .Setup(m => m.GetCapabilitiesStringLength(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u),
                out length))
            .Returns(1);

        _windowsAPIMock
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
    }

    [Fact]
    public void TryParseCapabilities_CapabilitiesParser_Throws()
    {
        SetupForCapabilitiesParser("cApAbIlItIeS");

        _capabilitiesParser
            .Setup(m => m.Parse("cApAbIlItIeS"))
            .Throws(new Exception("pArSeErRoR"));

        var service = new MonitorInputService(_windowsAPIMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        MonitorInputs? inputs = null;
        var exception = Assert.Throws<Exception>(() => service.TryLoadMonitorInputs(_handle12345, out inputs));

        Assert.Equal("pArSeErRoR", exception.Message);
        Assert.Null(inputs);
    }

    [Fact]
    public void TryParseCapabilities_CapabilitiesParser_ReturnsNull()
    {
        SetupForCapabilitiesParser("cApAbIlItIeS");

        _capabilitiesParser
            .Setup(m => m.Parse("cApAbIlItIeS"))
            .Returns((ICapabilitiesParser.VcpComponent?)null);

        var service = new MonitorInputService(_windowsAPIMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(_handle12345, out inputs));
        Assert.Null(inputs);
    }

    [Fact]
    public void TryParseCapabilities_NoInputSelectCode()
    {
        SetupForCapabilitiesParser("cApAbIlItIeS");

        _capabilitiesParser
            .Setup(m => m.Parse("cApAbIlItIeS"))
            .Returns(new ICapabilitiesParser.VcpComponent { Codes = new() });

        var service = new MonitorInputService(_windowsAPIMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(_handle12345, out inputs));
        Assert.Null(inputs);

        _loggerApiMock.Verify(m => m.Warning("Monitor does not support selecting input source (VCP code 0x60)"));
    }

    [Fact]
    public void TryParseCapabilities_NoInputSelectInputs()
    {
        SetupForCapabilitiesParser("cApAbIlItIeS");

        _capabilitiesParser
            .Setup(m => m.Parse("cApAbIlItIeS"))
            .Returns(new ICapabilitiesParser.VcpComponent
            {
                Codes = new()
                {
                    [0x60] = new()
                }
            });

        var service = new MonitorInputService(_windowsAPIMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(_handle12345, out inputs));
        Assert.Null(inputs);

        _loggerApiMock.Verify(m => m.Warning("Monitor does not define a list of supported input sources (VCP code 0x60)"));
    }

    [Fact]
    public void TryLoadSelectedInput_GetVCPFeatureAndVCPFeatureReply_ReturnsError()
    {
        SetupForCapabilitiesParser("cApAbIlItIeS");

        _capabilitiesParser
            .Setup(m => m.Parse("cApAbIlItIeS"))
            .Returns(new ICapabilitiesParser.VcpComponent
            {
                Codes = new()
                {
                    [0x60] = new() { 0x42 }
                }
            });

        uint pdwCurrentValue;
        uint pdwMaximumValue;

        _windowsAPIMock
            .Setup(m => m.GetVCPFeatureAndVCPFeatureReply(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u),
                0x60,
                ref It.Ref<MC_VCP_CODE_TYPE>.IsAny,
                out pdwCurrentValue,
                out pdwMaximumValue))
            .Returns(0);

        var service = new MonitorInputService(_windowsAPIMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(_handle12345, out inputs));
        Assert.Null(inputs);
    }

    [Fact]
    public void TryLoadSelectedInput_GetVCPFeatureAndVCPFeatureReply_ReturnsBadVct()
    {
        SetupForCapabilitiesParser("cApAbIlItIeS");

        _capabilitiesParser
            .Setup(m => m.Parse("cApAbIlItIeS"))
            .Returns(new ICapabilitiesParser.VcpComponent
            {
                Codes = new()
                {
                    [0x60] = new() { 0x42 }
                }
            });

        uint pdwCurrentValue;
        uint pdwMaximumValue;

        _windowsAPIMock
            .Setup(m => m.GetVCPFeatureAndVCPFeatureReply(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u),
                0x60,
                ref It.Ref<MC_VCP_CODE_TYPE>.IsAny,
                out pdwCurrentValue,
                out pdwMaximumValue))
            .Returns(
                (SafeHandle hMonitor, uint bVCPCode, ref MC_VCP_CODE_TYPE pvct, out uint pdwCurrentValue, out uint pdwMaximumValue) =>
                {
                    pvct = MC_VCP_CODE_TYPE.MC_MOMENTARY;
                    pdwCurrentValue = 0;
                    pdwMaximumValue = 0;
                    return 1;
                }
            );

        var service = new MonitorInputService(_windowsAPIMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.False(service.TryLoadMonitorInputs(_handle12345, out inputs));
        Assert.Null(inputs);
    }

    [Fact]
    public void LoadMonitorInputs_Successful()
    {
        _windowsAPIMock.Reset();

        SetupForCapabilitiesParser("cApAbIlItIeS");

        _capabilitiesParser
            .Setup(m => m.Parse("cApAbIlItIeS"))
            .Returns(new ICapabilitiesParser.VcpComponent
            {
                Codes = new()
                {
                    [0x60] = new() { 0x42, 0x69, 0xfe }
                }
            });

        uint pdwCurrentValue;
        uint pdwMaximumValue;

        _windowsAPIMock
            .Setup(m => m.GetVCPFeatureAndVCPFeatureReply(
                It.Is<SafePhysicalMonitorHandle>(h => h.DangerousGetHandle() == 12345u),
                0x60,
                ref It.Ref<MC_VCP_CODE_TYPE>.IsAny,
                out pdwCurrentValue,
                out pdwMaximumValue))
            .Returns(
                (SafeHandle hMonitor, uint bVCPCode, ref MC_VCP_CODE_TYPE pvct, out uint pdwCurrentValue, out uint pdwMaximumValue) =>
                {
                    pvct = MC_VCP_CODE_TYPE.MC_SET_PARAMETER;
                    pdwCurrentValue = 0x69;
                    pdwMaximumValue = 0xff;
                    return 1;
                }
            );

        var service = new MonitorInputService(_windowsAPIMock.Object, _capabilitiesParser.Object, _loggerApiMock.Object);

        MonitorInputs? inputs;
        Assert.True(service.TryLoadMonitorInputs(_handle12345, out inputs));

        Assert.NotNull(inputs);
        Assert.Equal(0x69, inputs.SelectedInput);
        Assert.Collection(inputs.ValidInputs,
            input => Assert.Equal(0x42, input),
            input => Assert.Equal(0x69, input),
            input => Assert.Equal(0xfe, input)
        );
    }
}
