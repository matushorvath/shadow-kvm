using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;
using System.Text;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using System.Collections.Immutable;
using HandlebarsDotNet.Helpers;

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
        Inputs.Clear();

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

#pragma warning disable CS8602 // Dereference of a possibly null reference.

    // (prot(monitor)type(LCD)model(...)cmds(...)vcp(02 04 ... 14(05 08 0B 0C) ... 60(1B 11 12 ) ... FD)mswhql(1)asset_eep(40)mccs_ver(2.1))

    static Parser<char, T> Tok<T>(Parser<char, T> p) => Try(p).Before(SkipWhitespaces);
    static Parser<char, char> Tok(char value) => Tok(Char(value));
    static Parser<char, string> Tok(string value) => Tok(String(value));

    static readonly Parser<char, char> _openParen = Tok('(');
    static readonly Parser<char, char> _closeParen = Tok(')');

    static readonly Parser<char, char> _hexDigit = 
        OneOf(Digit, CIOneOf('a', 'b', 'c', 'd', 'e', 'f'));

    static readonly Parser<char, byte> _byte =
        Tok(Map(
            (ch1, ch2) => Convert.ToByte($"{ch1}{ch2}", 16),
            _hexDigit,
            _hexDigit
        ));

    abstract class Component
    {
    }

    class VcpCode
    {
        public required byte Code { get; set; }
        public required ImmutableArray<byte> Values { get; set; }
    }

    class VcpComponent : Component
    {
        public required ImmutableArray<VcpCode> Codes { get; set; }
    }

    static readonly Parser<char, string> _vcpAbbreviation = Tok("vcp");
    static readonly Parser<char, string> _genericAbbreviation = Tok(OneOf(Lowercase, Char('_')).ManyString());

    static readonly Parser<char, Unit> _genericParameter =
        Rec(() => OneOf(
            _genericParameters.IgnoreResult(),
            AnyCharExcept(')').IgnoreResult()
        ));

    static readonly Parser<char, Unit> _genericParameters =
        _openParen
        .Then(_genericParameter.Many().IgnoreResult())
        .Before(_closeParen);

    static readonly Parser<char, Component?> _genericComponent =
        _genericAbbreviation
            .Then(_genericParameters)
            .Select<Component?>(_ => null);

    static readonly Parser<char, ImmutableArray<byte>> _vcpValues =
        _byte.Many().Select(values => ImmutableArray.CreateRange(values));

    static readonly Parser<char, VcpCode> _vcpCode =
        Map(
            (code, values) => new VcpCode { Code = code, Values = values },
            _byte,
            _openParen
                .Then(_vcpValues).Before(_closeParen)
                .Or(Return(ImmutableArray<byte>.Empty))
        );

    static readonly Parser<char, ImmutableArray<VcpCode>> _vcpCodes =
        _openParen
        .Then(_vcpCode.Many().Select(codes => ImmutableArray.CreateRange(codes)))
        .Before(_closeParen);

    static readonly Parser<char, VcpComponent> _vcpComponent =
        _vcpAbbreviation
            .Then(_vcpCodes).Select(codes => new VcpComponent { Codes = codes });

    static readonly Parser<char, Component?> _component =
        OneOf(_vcpComponent.Cast<Component?>(), _genericComponent)
            .Labelled("component");

    static readonly Parser<char, ImmutableArray<Component?>> _parser =
        _openParen
            .Then(_component.Many().Select(components => ImmutableArray.CreateRange(components)))
            .Before(_closeParen);

#pragma warning restore CS8602

    void ParseCapabilities(string capabilities)
    {
        // TODO parse capabilities, check if 0x60 looks sane, store supported inputs in Inputs
        var result = _parser.ParseOrThrow(capabilities);
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
    public IList<byte> Inputs { get; } = new List<byte>();
    public byte? SelectedInput { get; private set; }
}
