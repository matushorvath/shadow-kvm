using Moq;
using Pidgin;
using Serilog;

namespace ShadowKVM.Tests;

// Capabilities string format:
// (prot(monitor)type(LCD)model(...)cmds(...)vcp(02 04 ... 14(05 08 0B 0C) ... 60(1B 11 12 ) ... FD)mswhql(1)asset_eep(40)mccs_ver(2.1))

public class CapabilitiesParserTests
{
    Mock<ILogger> _loggerMock = new();

    public enum Error { ParseError, VcpComponentCount };
    public record TestDatum(string capabilities, Dictionary<byte, List<byte>>? expected, Error error = Error.ParseError);

    public static Dictionary<string, TestDatum> TestData => new()
    {
        ["empty string"] = new("", null),
        ["blank string"] = new("     ", null),
        ["valid simple"] = new("(vcp(60(01)))", new() { [0x60] = new() { 0x01 } }),

        ["valid with whitespace around"] = new("   (vcp(60(01)))   ", new() { [0x60] = new() { 0x01 } }),
        ["valid with whitespace everywhere"] = new(
            "   (   vcp   (  05   60   (   01   02   03   )   )   )   ",
            new()
            {
                [0x05] = new(),
                [0x60] = new() { 0x01, 0x02, 0x03 }
            }
        ),
        ["valid with line ends"] = new(
            "   (   vcp  \n  (  05   60  \r (   01   02  \r\n  03   )   )  \n\r  )   \n",
            new()
            {
                [0x05] = new(),
                [0x60] = new() { 0x01, 0x02, 0x03 }
            }
        ),

        ["no components"] = new("()", null, Error.VcpComponentCount),
        ["no vcp"] = new("(mccs_ver(2.1)))", null, Error.VcpComponentCount),

        ["generic code, lower case"] = new("(xyz()vcp(60))", new() { [0x60] = new() }),
        ["generic code, underscore"] = new("(xy_z()vcp(60))", new() { [0x60] = new() }),

        ["generic value, lower case"] = new("(xyz(abc)vcp(60))", new() { [0x60] = new() }),
        ["generic value, upper case"] = new("(xyz(ABC)vcp(60))", new() { [0x60] = new() }),
        ["generic value, mixed case"] = new("(xyz(AbC)vcp(60))", new() { [0x60] = new() }),
        ["generic value, alphanumeric"] = new("(xyz(A4_b5C6)vcp(60))", new() { [0x60] = new() }),
        ["generic value, complex"] = new("(xyz(%^& x_123)vcp(60))", new() { [0x60] = new() }),
        ["generic value, with parenthesis"] = new("(abc(01(02(03)))vcp(60))", new() { [0x60] = new() }),

        ["vcp, no open paren"] = new("(vcp)", null),
        ["vcp, no close paren"] = new("(vcp", null),
        ["code, no close paren"] = new("(vcp(60)", null),
        ["value, no close paren"] = new("(vcp(60(01))", null),

        ["code, negative byte"] = new("(vcp(-1))", null),
        ["code, decimal byte"] = new("(vcp(123))", null),
        ["code, too large byte"] = new("(vcp(abc))", null),

        ["value, negative byte"] = new("(vcp(60(-1)))", null),
        ["value, decimal byte"] = new("(vcp(60(123)))", null),
        ["value, too large byte"] = new("(vcp(60(abc)))", null),

        ["code, non-hexa digit]"] = new("(vcp(z))", null),
        ["value, non-hexa hexa digit]"] = new("(vcp(23(w)))", null),
        ["code, lower/upper case digit]"] = new("(vcp(Ab))", new() { [0xab] = new() }),
        ["value, lower/upper case digit]"] = new("(vcp(23(Cd)))", new() { [0x23] = new() { 0xcd } }),

        ["no codes"] = new("(vcp)", null),
        ["no codes in parens"] = new("(vcp())", []),
        ["one code"] = new("(vcp(60))", new() { [0x60] = new() }),
        ["three codes"] = new("(vcp(60 70 80))", new() { [0x60] = new(), [0x70] = new(), [0x80] = new() }),

        ["no values"] = new("(vcp(60))", new() { [0x60] = new() }),
        ["no values in parens"] = new("(vcp(60()))", new() { [0x60] = new() }),
        ["one value"] = new("(vcp(60(42)))", new() { [0x60] = new() { 0x42 } }),
        ["three values"] = new("(vcp(60(41 42 43)))", new() { [0x60] = new() { 0x41, 0x42, 0x43 } }),

        ["complex"] = new(
            "(prot(monitor)type(LCD)vcp(02 04 14(05 08 0B 0C) 60(1B 11 12))mccs_ver(2.1))",
            new()
            {
                [0x02] = new(),
                [0x04] = new(),
                [0x14] = new() { 0x05, 0x08, 0x0b, 0x0c },
                [0x60] = new() { 0x1b, 0x11, 0x12 }
            }
        ),
    };

    public static TheoryData<string> TestDataKeys => [.. TestData.Keys.AsEnumerable()];

    [Theory, MemberData(nameof(TestDataKeys))]
    public void Parse(string testDataKey)
    {
        var (capabilities, expected, error) = TestData[testDataKey];

        var parser = new CapabilitiesParser(_loggerMock.Object);
        var component = parser.Parse(capabilities);

        Assert.Equal(expected, component?.Codes);

        if (expected == null)
        {
            if (error == Error.ParseError)
            {
                _loggerMock.Verify(m => m.Warning(
                    "Failed to parse capabilities string: {Error}", It.IsAny<ParseError<char>>()));
            }
            else
            {
                _loggerMock.Verify(m => m.Warning(
                    "Expected exactly one VCP component in capabilities, but found {Count}", It.IsAny<int>()));
            }
        }
    }
}
