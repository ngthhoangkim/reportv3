using System.Text;
using System.Text.RegularExpressions;

namespace ReportV2.Rendering.Text;

internal static partial class RtfText
{
    public static string FromBytes(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return string.Empty;
        }

        var text = Encoding.UTF8.GetString(bytes);
        if (!text.Contains(@"{\rtf", StringComparison.OrdinalIgnoreCase))
        {
            return text.Trim();
        }

        text = UnicodeEscapeRegex().Replace(text, match =>
        {
            var code = int.Parse(match.Groups[1].Value);
            return char.ConvertFromUtf32(code < 0 ? code + 65536 : code);
        });
        text = ControlWordRegex().Replace(text, " ");
        text = text.Replace("{", "").Replace("}", "");
        text = EscapedCharRegex().Replace(text, "$1");
        text = WhitespaceRegex().Replace(text, " ");
        return text.Trim();
    }

    [GeneratedRegex(@"\\u(-?\d+)\??")]
    private static partial Regex UnicodeEscapeRegex();

    [GeneratedRegex(@"\\[a-zA-Z]+\d* ?")]
    private static partial Regex ControlWordRegex();

    [GeneratedRegex(@"\\([{}\\])")]
    private static partial Regex EscapedCharRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
