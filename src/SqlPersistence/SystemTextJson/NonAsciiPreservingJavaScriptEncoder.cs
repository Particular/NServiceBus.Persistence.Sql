using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;

// PostgreSQL 10-12 reject \u escapes above U+007F in jsonb when the database encoding is not UTF-8, so only Newtonsoft's escapes above ASCII are written.
sealed class NonAsciiPreservingJavaScriptEncoder : JavaScriptEncoder
{
    const char HighSurrogateStart = '\uD800';
    const char LowSurrogateEnd = '\uDFFF';

    static readonly SearchValues<char> CharactersToEscape = CreateCharactersToEscape();

    public static NonAsciiPreservingJavaScriptEncoder Instance { get; } = new();

    NonAsciiPreservingJavaScriptEncoder()
    {
    }

    public override int MaxOutputCharactersPerInputCharacter => 6;

    public override bool WillEncode(int unicodeScalar) => IsEscaped(unicodeScalar);

    static bool IsEscaped(int unicodeScalar) => unicodeScalar is < 0x20 or '"' or '\\' or '<' or '>' or '&' or '\'' or '+' or '`' or 0x85 or 0x2028 or 0x2029;

    public override unsafe int FindFirstCharacterToEncode(char* text, int textLength) =>
        FindFirstCharacterToEncode(new ReadOnlySpan<char>(text, textLength));

    public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten) =>
        TryEncode(unicodeScalar, new Span<char>(buffer, bufferLength), out numberOfCharactersWritten);

    internal static int FindFirstCharacterToEncode(ReadOnlySpan<char> text)
    {
        var offset = 0;
        while (true)
        {
            var found = text[offset..].IndexOfAny(CharactersToEscape);
            if (found < 0)
            {
                return -1;
            }

            var index = offset + found;
            var next = index + 1;
            if (char.IsHighSurrogate(text[index]) && next < text.Length && char.IsLowSurrogate(text[next]))
            {
                offset = next + 1;
                continue;
            }

            return index;
        }
    }

    static bool TryEncode(int unicodeScalar, Span<char> destination, out int written)
    {
        // The base class asks for the replacement character of an invalid sequence without checking WillEncode.
        if (!IsEscaped(unicodeScalar))
        {
            var rune = Rune.IsValid(unicodeScalar) ? new Rune(unicodeScalar) : Rune.ReplacementChar;
            return rune.TryEncodeToUtf16(destination, out written);
        }

        var shortForm = unicodeScalar switch
        {
            '"' => '"',
            '\\' => '\\',
            '\n' => 'n',
            '\r' => 'r',
            '\t' => 't',
            '\b' => 'b',
            '\f' => 'f',
            _ => '\0'
        };

        written = 0;
        if (shortForm != '\0')
        {
            if (destination.Length < 2)
            {
                return false;
            }

            destination[0] = '\\';
            destination[1] = shortForm;
            written = 2;
            return true;
        }

        if (destination.Length < 6)
        {
            return false;
        }

        const string hex = "0123456789ABCDEF";
        destination[0] = '\\';
        destination[1] = 'u';
        destination[2] = hex[(unicodeScalar >> 12) & 0xF];
        destination[3] = hex[(unicodeScalar >> 8) & 0xF];
        destination[4] = hex[(unicodeScalar >> 4) & 0xF];
        destination[5] = hex[unicodeScalar & 0xF];
        written = 6;
        return true;
    }

    static SearchValues<char> CreateCharactersToEscape()
    {
        var characters = new List<char>();
        for (var c = '\0'; c < 0x20; c++)
        {
            characters.Add(c);
        }

        characters.AddRange("\"\\<>&'+`\u0085\u2028\u2029");
        for (var c = HighSurrogateStart; c <= LowSurrogateEnd; c++)
        {
            characters.Add(c);
        }

        return SearchValues.Create(characters.ToArray());
    }
}
