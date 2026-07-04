using System.Text;

namespace BilingualTooltips.Modules.Dialogue.Data;

internal static class DialogueText
{
    public static string NormaliseRenderedText(string text) => text
        .Replace("\r\n", "\n")
        .Replace('\r', '\n')
        .Replace('\u00a0', ' ') // NBSP to space
        .Trim();

    public static string NormaliseMatchText(string text) =>
        NormaliseRenderedText(text)
            // alphabetic writing systems have dynamic line breaks added at runtime
            // character-based languages (ja, zh) doesn't have the issue
            .Replace('\n', ' ')
            .CollapseAsciiWhitespace();

    private static string CollapseAsciiWhitespace(this string text)
    {
        var output = new StringBuilder(text.Length);
        var previousWasSpace = false;
        foreach (var character in text)
        {
            if (character is ' ' or '\t')
            {
                if (previousWasSpace) continue;
                output.Append(' ');
                previousWasSpace = true;
                continue;
            }

            output.Append(character);
            previousWasSpace = false;
        }

        return output.ToString().Trim();
    }

    public static bool OrderedTokenMatch(string text, IReadOnlyList<string> tokens)
    {
        var index = 0;
        foreach (var token in tokens)
        {
            var next = text.IndexOf(token, index, StringComparison.Ordinal);
            if (next < 0) return false;
            index = next + token.Length;
        }

        return true;
    }
}
