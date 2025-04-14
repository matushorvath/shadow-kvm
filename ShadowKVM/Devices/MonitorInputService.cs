using System.Diagnostics.CodeAnalysis;
using System.Text;
using Serilog;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;

namespace ShadowKVM;

public class MonitorInputs
{
    public required List<byte> ValidInputs { get; set; }
    public required byte SelectedInput { get; set; }
}

public interface IMonitorInputService
{
    public bool TryLoadMonitorInputs(Monitor monitor, [NotNullWhen(true)] out MonitorInputs? monitorInputs);
    public bool TryLoadMonitorInputs(SafePhysicalMonitorHandle physicalMonitorHandle, [NotNullWhen(true)] out MonitorInputs? monitorInputs);
}

public class MonitorInputService(IWindowsAPI windowsAPI, ICapabilitiesParser capabilitiesParser, ILogger logger) : IMonitorInputService
{
    public bool TryLoadMonitorInputs(Monitor monitor, [NotNullWhen(true)] out MonitorInputs? monitorInputs)
    {
        return TryLoadMonitorInputs(monitor.Handle, out monitorInputs);
    }

    public bool TryLoadMonitorInputs(SafePhysicalMonitorHandle physicalMonitorHandle, [NotNullWhen(true)] out MonitorInputs? monitorInputs)
    {
        monitorInputs = default;

        List<byte>? validInputs;
        if (!TryLoadValidInputs(physicalMonitorHandle, out validInputs))
        {
            return false;
        }

        byte selectedInput;
        if (!TryLoadSelectedInput(physicalMonitorHandle, out selectedInput))
        {
            return false;
        }

        monitorInputs = new() { ValidInputs = validInputs, SelectedInput = selectedInput };
        return true;
    }

    unsafe bool TryLoadValidInputs(SafePhysicalMonitorHandle physicalMonitorHandle, [NotNullWhen(true)] out List<byte>? validInputs)
    {
        validInputs = default;

        int result;

        // Find out which inputs are supported by this monitor
        uint capabilitiesLength;

        result = windowsAPI.GetCapabilitiesStringLength(physicalMonitorHandle, out capabilitiesLength);
        if (result != 1 || capabilitiesLength == 0)
        {
            return false;
        }

        var capabilitiesBuffer = new byte[capabilitiesLength];
        fixed (byte* capabilitiesPtr = &capabilitiesBuffer[0])
        {
            result = windowsAPI.CapabilitiesRequestAndCapabilitiesReply(physicalMonitorHandle, new PSTR(capabilitiesPtr), capabilitiesLength);
            if (result != 1)
            {
                return false;
            }
        }

        var capabilities = Encoding.ASCII.GetString(capabilitiesBuffer, 0, (int)capabilitiesLength - 1);
        return TryParseCapabilities(capabilities, out validInputs);
    }

    bool TryParseCapabilities(string capabilities, [NotNullWhen(true)] out List<byte>? validInputs)
    {
        validInputs = default;

        var component = capabilitiesParser.Parse(capabilities);
        if (component == null)
        {
            return false;
        }

        List<byte>? inputs;
        if (!component.Codes.TryGetValue(0x60, out inputs))
        {
            logger.Warning("Monitor does not support selecting input source (VCP code 0x60)");
            return false;
        }
        if (inputs.Count == 0)
        {
            logger.Warning("Monitor does not define a list of supported input sources (VCP code 0x60)");
            return false;
        }

        validInputs = inputs;
        return true;
    }

    unsafe bool TryLoadSelectedInput(SafePhysicalMonitorHandle physicalMonitorHandle, out byte selectedInput)
    {
        selectedInput = default;

        // Find out which input is currently selected for this monitor
        var vct = new MC_VCP_CODE_TYPE();
        uint selectedInputUint;

        int result = windowsAPI.GetVCPFeatureAndVCPFeatureReply(physicalMonitorHandle, 0x60, ref vct, out selectedInputUint, out _);
        if (result != 1 || vct != MC_VCP_CODE_TYPE.MC_SET_PARAMETER)
        {
            return false;
        }

        selectedInput = (byte)(selectedInputUint & 0xff);
        return true;
    }
}
