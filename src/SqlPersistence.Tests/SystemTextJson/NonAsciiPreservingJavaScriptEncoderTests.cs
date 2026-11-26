using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using NUnit.Framework;

[TestFixture]
public class NonAsciiPreservingJavaScriptEncoderTests
{
    static readonly JsonSerializerOptions Options = new() { Encoder = NonAsciiPreservingJavaScriptEncoder.Instance };

    static readonly char[] EscapedAscii = "\"\\<>&'+`".ToCharArray();

    [Test]
    public void EscapesExactlyTheExpectedBasicMultilingualPlaneCodePoints()
    {
        var unexpected = new List<string>();
        for (var codePoint = 0; codePoint <= 0xFFFF; codePoint++)
        {
            if (char.IsSurrogate((char)codePoint))
            {
                continue;
            }

            var character = (char)codePoint;
            var json = Serialize(character.ToString());
            var isEscaped = json != "\"" + character + "\"";
            var shouldBeEscaped = character < 0x20 || EscapedAscii.Contains(character) || character is '\u0085' or '\u2028' or '\u2029';

            if (isEscaped != shouldBeEscaped)
            {
                unexpected.Add($"U+{codePoint:X4}: {json}");
            }
        }

        Assert.That(unexpected, Is.Empty);
    }

    [Test]
    public void EscapesNonAsciiCodePointsLikeNewtonsoft()
    {
        var newtonsoft = new List<int>();
        var ours = new List<int>();
        for (var codePoint = 0x80; codePoint <= 0xFFFF; codePoint++)
        {
            if (char.IsSurrogate((char)codePoint))
            {
                continue;
            }

            var text = ((char)codePoint).ToString();
            if (JsonConvert.SerializeObject(text) != "\"" + text + "\"")
            {
                newtonsoft.Add(codePoint);
            }

            if (Serialize(text) != "\"" + text + "\"")
            {
                ours.Add(codePoint);
            }
        }

        Assert.That(newtonsoft, Is.EqualTo(new[] { 0x85, 0x2028, 0x2029 }));
        Assert.That(ours, Is.EqualTo(newtonsoft));
    }

    [TestCase('\0', "\\u0000")]
    [TestCase('\u0001', "\\u0001")]
    [TestCase('\u001f', "\\u001F")]
    [TestCase('\b', "\\b")]
    [TestCase('\t', "\\t")]
    [TestCase('\n', "\\n")]
    [TestCase('\f', "\\f")]
    [TestCase('\r', "\\r")]
    [TestCase('"', "\\\"")]
    [TestCase('\\', "\\\\")]
    [TestCase('<', "\\u003C")]
    [TestCase('>', "\\u003E")]
    [TestCase('&', "\\u0026")]
    [TestCase('\'', "\\u0027")]
    [TestCase('+', "\\u002B")]
    [TestCase('`', "\\u0060")]
    [TestCase('\u0085', "\\u0085")]
    [TestCase('\u2028', "\\u2028")]
    [TestCase('\u2029', "\\u2029")]
    public void WritesEscapesWithShortFormsAndUppercaseHex(char value, string expected)
    {
        Assert.That(Serialize(value.ToString()), Is.EqualTo("\"" + expected + "\""));
    }

    [TestCase("\u00a0")]
    [TestCase("\u0080\u009f")]
    [TestCase("\u3000")]
    [TestCase("a\ufeffb")]
    [TestCase("\u007f")]
    [TestCase("\ufffd")]
    [TestCase("\u0378")]
    [TestCase("\ue000")]
    [TestCase("\uffff")]
    [TestCase("Z\u00fcrich \u65e5\u672c\u8a9e")]
    public void WritesOtherCharactersRaw(string value)
    {
        Assert.That(Serialize(value), Is.EqualTo("\"" + value + "\""));
    }

    [TestCaseSource(nameof(LoneSurrogateCases))]
    public void WritesLoneSurrogatesAsReplacementCharacterAndKeepsEverythingElse(string value, string expected)
    {
        Assert.That(Serialize(value), Is.EqualTo("\"" + expected + "\""));
    }

    // Lone surrogates cannot be stored in attribute arguments, so the values are built here.
    static IEnumerable<TestCaseData> LoneSurrogateCases()
    {
        const char high = '\ud800';
        const char low = '\udc00';
        const string emoji = "\U0001F600";
        const char replacement = '\ufffd';

        yield return new TestCaseData(high.ToString(), replacement.ToString());
        yield return new TestCaseData('\udbff'.ToString(), replacement.ToString());
        yield return new TestCaseData(low.ToString(), replacement.ToString());
        yield return new TestCaseData('\udfff'.ToString(), replacement.ToString());
        yield return new TestCaseData(high + "a", replacement + "a");
        yield return new TestCaseData("a" + low, "a" + replacement);
        yield return new TestCaseData(high.ToString() + high, replacement.ToString() + replacement);
        yield return new TestCaseData(low.ToString() + high, replacement.ToString() + replacement);
        yield return new TestCaseData(high + emoji, replacement + emoji);
        yield return new TestCaseData(emoji + low, emoji + replacement);
        yield return new TestCaseData(high + "<" + emoji + low + "&" + emoji + high, replacement + "\\u003C" + emoji + replacement + "\\u0026" + emoji + replacement);
        yield return new TestCaseData("<" + emoji + high + "a\"", "\\u003C" + emoji + replacement + "a\\\"");
        yield return new TestCaseData(emoji + "\n" + low + emoji, emoji + "\\n" + replacement + emoji);
        yield return new TestCaseData("\"" + high + low + "\"", "\\\"" + "\U00010000" + "\\\"");
    }

    [Test]
    public void WritesValidSurrogatePairsRaw()
    {
        const string value = "\U0001F600 family \U0001F468\u200d\U0001F469\u200d\U0001F467 \U00010000\U0010FFFF";

        Assert.That(Serialize(value), Is.EqualTo("\"" + value + "\""));
    }

    [Test]
    public void EncodedOutputIsValidUtf8ForEveryLoneSurrogate()
    {
        var strict = new UTF8Encoding(false, throwOnInvalidBytes: true);
        for (var codePoint = 0xD800; codePoint <= 0xDFFF; codePoint++)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes("x" + (char)codePoint + "y", Options);

            Assert.That(() => strict.GetString(bytes), Throws.Nothing, $"U+{codePoint:X4}");
            Assert.That(strict.GetString(bytes), Is.EqualTo("\"x\ufffdy\""));
        }
    }

    [Test]
    public void WritesPropertyNamesWithTheSameRules()
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, int> { ["a\u00a0<\ud800"] = 1 }, Options);

        Assert.That(json, Is.EqualTo("{\"a\u00a0\\u003C\ufffd\":1}"));
    }

    [Test]
    public void FindsTheFirstCharacterToEncodeInUtf8()
    {
        var utf8 = Encoding.UTF8.GetBytes("\u00e4\u00a0\U0001F600abc<");

        Assert.That(NonAsciiPreservingJavaScriptEncoder.Instance.FindFirstCharacterToEncodeUtf8(utf8), Is.EqualTo(utf8.Length - 1));
    }

    [TestCase("", -1)]
    [TestCase("abc", -1)]
    [TestCase("abc\u00a0\U0001F600\u00e4", -1)]
    [TestCase("ab<", 2)]
    [TestCase("\U0001F600\U0001F600<", 4)]
    [TestCase("\U0001F600\u2028", 2)]
    public void FindsTheFirstCharacterToEncode(string text, int expected)
    {
        Assert.That(NonAsciiPreservingJavaScriptEncoder.FindFirstCharacterToEncode(text), Is.EqualTo(expected));
    }

    [Test]
    public void FindsTheFirstCharacterToEncodeInLongTexts()
    {
        var text = string.Concat(Enumerable.Repeat("\u00e4\U0001F600abcdefghijklmnopqrstuvwxyz", 100)) + "\"";

        Assert.That(NonAsciiPreservingJavaScriptEncoder.FindFirstCharacterToEncode(text), Is.EqualTo(text.Length - 1));
    }

    [TestCaseSource(nameof(LoneSurrogatePositions))]
    public void FindsLoneSurrogates(string text, int expected)
    {
        Assert.That(NonAsciiPreservingJavaScriptEncoder.FindFirstCharacterToEncode(text), Is.EqualTo(expected));
    }

    static IEnumerable<TestCaseData> LoneSurrogatePositions()
    {
        const string emoji = "\U0001F600";

        yield return new TestCaseData(emoji + '\ud800', 2);
        yield return new TestCaseData('\udc00' + emoji, 0);
        yield return new TestCaseData("a" + '\ud83d', 1);
        yield return new TestCaseData('\ud83d' + emoji, 0);
        yield return new TestCaseData(emoji + '\ude00', 2);
    }

    [TestCase('"', "\\\"")]
    [TestCase('<', "\\u003C")]
    [TestCase(0x2028, "\\u2028")]
    [TestCase(0xE4, "ä")]
    [TestCase(0x1F600, "\U0001F600")]
    public void EncodesIntoADestinationOfExactlyTheRequiredLength(int scalar, string expected)
    {
        var destination = new char[expected.Length];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(NonAsciiPreservingJavaScriptEncoder.TryEncode(scalar, destination, out var written), Is.True);
            Assert.That(written, Is.EqualTo(expected.Length));
            Assert.That(new string(destination), Is.EqualTo(expected));
        }
    }

    [TestCase('"', 1)]
    [TestCase('<', 5)]
    [TestCase(0x2028, 5)]
    [TestCase(0xE4, 0)]
    [TestCase(0x1F600, 1)]
    public void DoesNotWriteIntoATooSmallDestination(int scalar, int length)
    {
        var destination = new char[length + 2];
        Array.Fill(destination, 'x');

        using (Assert.EnterMultipleScope())
        {
            Assert.That(NonAsciiPreservingJavaScriptEncoder.TryEncode(scalar, destination.AsSpan(0, length), out var written), Is.False);
            Assert.That(written, Is.Zero);
            Assert.That(destination[length..], Is.All.EqualTo('x'));
        }
    }

    static string Serialize(string value) => JsonSerializer.Serialize(value, Options);
}
