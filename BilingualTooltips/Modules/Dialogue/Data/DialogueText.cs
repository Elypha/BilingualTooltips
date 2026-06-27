namespace BilingualTooltips.Modules.Dialogue.Data;

internal static class DialogueText
{
    public static string NormaliseRenderedText(string text) => text
        .Replace("\r\n", "\n")
        .Replace('\r', '\n')
        .Replace('\u00a0', ' ')
        .Trim();

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
