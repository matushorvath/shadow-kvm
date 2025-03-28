using Moq;
using Serilog;

namespace ShadowKVM.Tests;

// TODO CapabilitiesParserTests
//
// Parser.Parse returns result.Success == false
// returns just null component, just not VcpComponent, null, Component and VcpComponent (null and Component are ignored)
// returns multiple VcpComponents, no VcpComponents
//
// grammar:
// no valid component, component, vcpcomponent
// vcp component: vcp codes
// vcp codes: no open paren, no close paren, no codes, vcp one code, vcp multiple codes
// vcp code: byte (invalid byte <0, >255), no open paren, no close paren
// values: no values, one value, multiple values
// generic component: upper, lower case, underscore
// generic parameters: none, one, multiple, crazy values (any except ')')
// byte from one digit, two digits, non-hexa digits, lower/upper case hexa digits

public class CapabilitiesParserTests
{
    Mock<ILogger> _loggerMock = new();

    public record TestDatum(string capabilities, Dictionary<byte, List<byte>>? expected);

    public static Dictionary<string, TestDatum> TestData => new()
    {
        ["no monitors"] = new(
            "(prot(monitor)type(LCD)vcp(02 04 14(05 08 0B 0C) 60(1B 11 12))mccs_ver(2.1))",
            new()
            {
                [0x02] = new(),
                [0x04] = new(),
                [0x14] = new() { 0x05, 0x08, 0x0b, 0x0c },
                [0x60] = new() { 0x1b, 0x11, 0x12 }
            }
        )
    };

    public static TheoryData<string> TestDataKeys => [.. TestData.Keys.AsEnumerable()];

    [Theory, MemberData(nameof(TestDataKeys))]
    public void CapabilitiesParse(string testDataKey)
    {
        var (capabilities, expected) = TestData[testDataKey];

        var parser = new CapabilitiesParser(_loggerMock.Object);
        var component = parser.Parse(capabilities);

        Assert.Equal(expected, component?.Codes);
    }
}
