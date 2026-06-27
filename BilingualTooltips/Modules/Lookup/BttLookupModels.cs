namespace BilingualTooltips.Modules.Lookup;

public enum BttLookupKind
{
    Unknown,
    Item,
    Action,
    Content,
    NpcDialogue,
}

public sealed record BttLookupKey
{
    public BttLookupKind Kind { get; init; }
    public ulong? ItemId { get; init; }
    public GameTextResolver.ActionReference? ActionReference { get; init; }
    public GameTextResolver.ContentReference? ContentReference { get; init; }
    public string DialogueKey { get; init; } = "";

    public static BttLookupKey Unknown { get; } = new();
    public static BttLookupKey Item(ulong itemId) => new() { Kind = BttLookupKind.Item, ItemId = itemId };
    public static BttLookupKey Action(GameTextResolver.ActionReference action) => new() { Kind = BttLookupKind.Action, ActionReference = action };
    public static BttLookupKey Content(GameTextResolver.ContentReference content) => new() { Kind = BttLookupKind.Content, ContentReference = content };
    public static BttLookupKey NpcDialogue(string key) => new() { Kind = BttLookupKind.NpcDialogue, DialogueKey = key };

    public bool IsValid => Kind switch
    {
        BttLookupKind.Item => ItemId is > 0,
        BttLookupKind.Action => ActionReference is { RowId: > 0 },
        BttLookupKind.Content => ContentReference is { Type: not GameTextResolver.ContentType.Unknown, RowId: > 0 },
        BttLookupKind.NpcDialogue => !string.IsNullOrWhiteSpace(DialogueKey),
        _ => false,
    };

    public bool TryGetItem(out ulong itemId)
    {
        itemId = 0;
        if (Kind != BttLookupKind.Item || ItemId is not { } value) return false;

        itemId = value;
        return itemId != 0;
    }

    public bool TryGetAction(out GameTextResolver.ActionReference action)
    {
        action = default;
        if (Kind != BttLookupKind.Action
            || ActionReference is not { } value
            || value.RowId == 0)
        {
            return false;
        }

        action = value;
        return true;
    }

    public bool TryGetContent(out GameTextResolver.ContentReference content)
    {
        content = default;
        if (Kind != BttLookupKind.Content
            || ContentReference is not { } value
            || value.Type == GameTextResolver.ContentType.Unknown
            || value.RowId == 0)
        {
            return false;
        }

        content = value;
        return true;
    }

    public bool TryGetNpcDialogue(out string key)
    {
        key = string.Empty;
        if (Kind != BttLookupKind.NpcDialogue || string.IsNullOrWhiteSpace(DialogueKey)) return false;

        key = DialogueKey;
        return true;
    }

    public override string ToString() => BttLookupDisplay.DisplayId(this);
}

public sealed record BttLookupResolvedItem(
    BttLookupKey Key,
    string KindLabel,
    string Title,
    string Subtitle,
    IReadOnlyList<BttLookupResolvedLine> Lines
);

public sealed record BttLookupResolvedLine(
    BttLanguage Language,
    string Text,
    bool Available
);

public sealed class BttLookupHistoryEntry
{
    public BttLookupKey Key { get; set; } = BttLookupKey.Unknown;
    public string DisplayName { get; set; } = "";
    public DateTimeOffset LastSeenAt { get; set; }
    public int Count { get; set; }
}

[Flags]
public enum BttLookupHistorySource
{
    None = 0,
    Item = 1,
    Action = 2,
    Content = 4,
    NpcDialogue = 8,
    All = Item | Action | Content | NpcDialogue,
}
