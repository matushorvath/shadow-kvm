using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;
using Serilog;
using System.Collections.Immutable;

// Capabilities string format:
// (prot(monitor)type(LCD)model(...)cmds(...)vcp(02 04 ... 14(05 08 0B 0C) ... 60(1B 11 12 ) ... FD)mswhql(1)asset_eep(40)mccs_ver(2.1))

namespace ShadowKVM;

internal static class CapabilitiesParser
{
    internal abstract class Component
    {
    }

    internal class VcpComponent : Component
    {
        public required ImmutableDictionary<byte, ImmutableArray<byte>> Codes { get; set; }
    }

    public static VcpComponent? Parse(string input)
    {
        var result = _capabilities.Parse(input);
        if (!result.Success)
        {
            Log.Warning("Failed to parse capabilities string: {Error}", result.Error);
            return null;
        }

        var components = (
            from component in result.Value
            where component != null && component is VcpComponent
            select component as VcpComponent
        ).ToArray();

        if (components.Length != 1)
        {
            Log.Warning("Expected exactly one VCP component in capabilities, but found {Count}", components.Length);
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

    static readonly Parser<char, Component?> _genericComponent =
        _genericAbbreviation
            .Then(_genericParameters)
            .Select<Component?>(_ => null);

    static readonly Parser<char, ImmutableArray<byte>> _vcpValues =
        _byte.Many().Select(values => ImmutableArray.CreateRange(values));

    static readonly Parser<char, KeyValuePair<byte, ImmutableArray<byte>>> _vcpCode =
        Map(
            (code, values) => KeyValuePair.Create(code, values),
            _byte,
            _openParen
                .Then(_vcpValues).Before(_closeParen)
                .Or(Return(ImmutableArray<byte>.Empty))
        );

    static readonly Parser<char, ImmutableDictionary<byte, ImmutableArray<byte>>> _vcpCodes =
        _openParen
        .Then(_vcpCode.Many().Select(codes => ImmutableDictionary.CreateRange(codes)))
        .Before(_closeParen);

    static readonly Parser<char, VcpComponent> _vcpComponent =
        _vcpAbbreviation
            .Then(_vcpCodes).Select(codes => new VcpComponent { Codes = codes });

    static readonly Parser<char, Component?> _component =
        OneOf(_vcpComponent.Cast<Component?>(), _genericComponent)
            .Labelled("component");

    static readonly Parser<char, ImmutableArray<Component?>> _capabilities =
        _openParen
            .Then(_component.Many().Select(components => ImmutableArray.CreateRange(components)))
            .Before(_closeParen);

#pragma warning restore CS8602
}
