using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;
using Serilog;

namespace ShadowKVM;

public interface ICapabilitiesParser
{
    abstract class Component
    {
    }

    class VcpComponent : Component
    {
        public required Dictionary<byte, List<byte>> Codes { get; set; }
    }

    VcpComponent? Parse(string input);
}

public class CapabilitiesParser(ILogger logger) : ICapabilitiesParser
{
    public ICapabilitiesParser.VcpComponent? Parse(string input)
    {
        var result = _capabilities.Parse(input);
        if (!result.Success)
        {
            logger.Warning("Failed to parse capabilities string: {Error}", result.Error);
            return null;
        }

        var components = (
            from component in result.Value
            where component != null && component is ICapabilitiesParser.VcpComponent
            select component as ICapabilitiesParser.VcpComponent
        ).ToArray();

        if (components.Length != 1)
        {
            logger.Warning("Expected exactly one VCP component in capabilities, but found {Count}", components.Length);
            return null;
        }

        return components[0];
    }

#pragma warning disable CS8602 // Dereference of a possibly null reference

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

    static readonly Parser<char, ICapabilitiesParser.Component?> _genericComponent =
        _genericAbbreviation
            .Then(_genericParameters)
            .Select<ICapabilitiesParser.Component?>(_ => null);

    static readonly Parser<char, List<byte>> _vcpValues =
        _byte.Many().Select(values => values.ToList());

    static readonly Parser<char, KeyValuePair<byte, List<byte>>> _vcpCode =
        Map(
            (code, values) => KeyValuePair.Create(code, values),
            _byte,
            _openParen
                .Then(_vcpValues).Before(_closeParen)
                .Or(Return(new List<byte>()))
        );

    static readonly Parser<char, Dictionary<byte, List<byte>>> _vcpCodes =
        _openParen
        .Then(_vcpCode.Many().Select(codes => new Dictionary<byte, List<byte>>(codes)))
        .Before(_closeParen);

    static readonly Parser<char, ICapabilitiesParser.VcpComponent> _vcpComponent =
        _vcpAbbreviation
            .Then(_vcpCodes).Select(codes => new ICapabilitiesParser.VcpComponent { Codes = codes });

    static readonly Parser<char, ICapabilitiesParser.Component?> _component =
        OneOf(_vcpComponent.Cast<ICapabilitiesParser.Component?>(), _genericComponent)
            .Labelled("component");

    static readonly Parser<char, List<ICapabilitiesParser.Component?>> _capabilities =
        SkipWhitespaces
            .Then(_openParen)
            .Then(_component.Many().Select(components => components.ToList()))
            .Before(_closeParen);

#pragma warning restore CS8602
}
