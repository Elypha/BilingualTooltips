using Dalamud.Game.Gui;

namespace BilingualTooltips.Modules.Lookup;

public static class BttLookupDisplay
{
    private const string UnknownKindLabel = "Unknown";
    private const string ItemKindLabel = "Item";
    private const string ActionKindLabel = "Action";
    private const string ContentKindLabel = "Game UI";
    private const string NpcDialogueKindLabel = "NPC dialogue";

    public const string UnsupportedLookupKindMessage = "Unsupported lookup kind.";
    public const string InvalidItemKeyMessage = "Invalid item key.";
    public const string InvalidActionKeyMessage = "Invalid action key.";
    public const string InvalidContentKeyMessage = "Invalid or unknown content key.";
    public const string InvalidNpcDialogueKeyMessage = "Invalid dialogue key.";

    public const string GameTextSourceUnavailableMessage = "Game text source is not available yet.";
    public const string GameUiTextSourceUnavailableMessage = "Game UI text source is not available yet.";
    public const string DialogueDataSourceUnavailableMessage = "Dialogue data source is not available yet.";
    public const string DialogueDataLoadingMessage = "Dialogue data is loading.";
    public const string NoDialoguePackageInstalledMessage = "No dialogue package is installed.";
    public const string NoTextInLanguageMessage = "No text in this language.";
    public const string NoContentTextInLanguageMessage = "No content text in this language.";
    public const string FailedToReadLanguageMessage = "Failed to read this language.";

    public static string DisplayId(BttLookupKey key) => key.Kind switch
    {
        BttLookupKind.Item when key.ItemId is { } itemId => $"Item#{itemId}",
        BttLookupKind.Action when key.ActionReference is { } action => $"{action.Kind}#{action.RowId}",
        BttLookupKind.Content when key.ContentReference is { } content => $"{content.Type}#{content.RowId}",
        BttLookupKind.NpcDialogue => key.DialogueKey,
        _ => "",
    };

    public static string KindLabel(BttLookupKey key) => key.Kind switch
    {
        BttLookupKind.Item => ItemKindLabel,
        BttLookupKind.Action when key.ActionReference is { } action => KindLabel(action),
        BttLookupKind.Action => ActionKindLabel,
        BttLookupKind.Content when key.ContentReference is { } content => KindLabel(content.Type),
        BttLookupKind.Content => ContentKindLabel,
        BttLookupKind.NpcDialogue => NpcDialogueKindLabel,
        _ => UnknownKindLabel,
    };

    public static string SourceLabel(BttLookupHistorySource source) => source switch
    {
        BttLookupHistorySource.Item => "Items",
        BttLookupHistorySource.Action => "Actions",
        BttLookupHistorySource.Content => ContentKindLabel,
        BttLookupHistorySource.NpcDialogue => NpcDialogueKindLabel,
        _ => UnknownKindLabel,
    };

    private static string KindLabel(GameTextResolver.ActionReference action) => action.Kind switch
    {
        DetailKind.Companion => "Minion",
        DetailKind.Mount => "Mount",
        _ => ActionKindLabel,
    };

    private static string KindLabel(GameTextResolver.ContentType type) => type switch
    {
        GameTextResolver.ContentType.Duty => "Duty",
        GameTextResolver.ContentType.Roulette => "Roulette",
        GameTextResolver.ContentType.CosmicMission => "Cosmic mission",
        _ => ContentKindLabel,
    };

    public static string FallbackTitle(BttLookupKey key) => key.Kind switch
    {
        BttLookupKind.Item when key.ItemId is { } itemId => $"Item #{itemId}",
        BttLookupKind.Action when key.ActionReference is { } action => $"{KindLabel(action)} #{action.RowId}",
        _ => DisplayId(key),
    };

    public static string BestTitle(IReadOnlyList<BttLookupResolvedLine> lines, string fallback)
    {
        var line = lines.FirstOrDefault(static line => line.Available && !string.IsNullOrWhiteSpace(line.Text));
        return line == null
            ? fallback
            : line.Text
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', 2)[0]
                .Trim();
    }

    public static string JoinNameAndDescription(string name, string description)
    {
        name = name.Trim();
        description = description.Trim();
        return (name, description) switch
        {
            ({ Length: > 0 }, { Length: > 0 }) => $"{name}\n{description}",
            ({ Length: > 0 }, _) => name,
            (_, { Length: > 0 }) => description,
            _ => "",
        };
    }

    public static BttLookupResolvedItem Unavailable(BttLookupKey key, string message)
    {
        var kindLabel = KindLabel(key);
        var id = DisplayId(key);
        return new BttLookupResolvedItem(
            key,
            kindLabel,
            string.IsNullOrWhiteSpace(id) ? kindLabel : id,
            id,
            [.. BttLanguageExtensions.AllLanguages.Select(language => new BttLookupResolvedLine(language, message, false))]);
    }
}
