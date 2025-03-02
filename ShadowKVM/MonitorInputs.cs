using System.Collections.Immutable;
using System.Text;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;

// TODO input naming service, translate value to input description

namespace ShadowKVM;

internal class MonitorInputs
{
    public void Load(Monitor monitor)
    {
        Load(monitor.Handle);
    }

    public void Load(SafePhysicalMonitorHandle physicalMonitorHandle)
    {
        // SupportsInputs will be set to false later, if we encounter any problems
        SupportsInputs = true;

        LoadCapabilities(physicalMonitorHandle);

        // Don't try to load input source if capabilities look suspicious
        if (SupportsInputs)
        {
            LoadInputSource(physicalMonitorHandle);
        }
    }

    unsafe void LoadCapabilities(SafePhysicalMonitorHandle physicalMonitorHandle)
    {
        ValidInputs.Clear();

        int result;

        // Find out which inputs are supported by this monitor
        uint capabilitiesLength;

        result = PInvoke.GetCapabilitiesStringLength(physicalMonitorHandle, out capabilitiesLength);
        if (result != 1)
        {
            SupportsInputs = false;
            return;
        }

        var capabilitiesBuffer = new byte[capabilitiesLength];
        fixed (byte* capabilitiesPtr = &capabilitiesBuffer[0])
        {
            result = PInvoke.CapabilitiesRequestAndCapabilitiesReply(physicalMonitorHandle, new PSTR(capabilitiesPtr), capabilitiesLength);
            if (result != 1)
            {
                SupportsInputs = false;
                return;
            }
        }

        var capabilities = Encoding.ASCII.GetString(capabilitiesBuffer);
        ParseCapabilities(capabilities);
    }

    void ParseCapabilities(string capabilities)
    {
        var component = CapabilitiesParser.Parse(capabilities);
        if (component == null)
        {
            SupportsInputs = false;
            return;
        }

        ImmutableArray<byte> inputs;
        if (!component.Codes.TryGetValue(0x60, out inputs))
        {
            // TODO log that code 0x60 was not found
            SupportsInputs = false;
            return;
        }
        if (inputs.Length == 0)
        {
            // TODO log no supported inputs found
            SupportsInputs = false;
            return;
        }

        ValidInputs.AddRange(inputs);
    }

    unsafe void LoadInputSource(SafePhysicalMonitorHandle physicalMonitorHandle)
    {
        SelectedInput = null;

        // Find out which input is currently selected for this monitor
        var vct = new MC_VCP_CODE_TYPE();
        uint selectedInput;

        int result = PInvoke.GetVCPFeatureAndVCPFeatureReply(physicalMonitorHandle, 0x60, &vct, out selectedInput, null);
        if (result != 1 || vct != MC_VCP_CODE_TYPE.MC_SET_PARAMETER)
        {
            SupportsInputs = false;
            return;
        }

        SelectedInput = (byte)(selectedInput & 0xff);
    }

    public bool SupportsInputs { get; private set; }
    public List<byte> ValidInputs { get; } = new List<byte>();
    public byte? SelectedInput { get; private set; }
}
