#if NETFRAMEWORK
namespace NServiceBus.Persistence.Sql.ScriptBuilder;

using System.Text.RegularExpressions;

static class StringExtensions
{
    // Since NETFRAMEWORK is Windows-only (ignoring mono), we can safely assume we need LF -> CRLF
    public static string ReplaceLineEndings(this string input) => Regex.Replace(input, "(?<!\r)\n", "\r\n");
}
#endif