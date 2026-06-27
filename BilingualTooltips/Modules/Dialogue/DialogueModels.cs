using BilingualTooltips.Modules.Dialogue.Data;

namespace BilingualTooltips.Modules.Dialogue;

public enum DialogueTranslationStatus
{
    Idle,
    Searching,
    Loading,
    NoData,
    Error,
    NotFound,
    Ok,
}

public sealed class DialogueMatch
{
    public string Key { get; init; } = "";
    public BttLanguage InputLanguage { get; init; }
    public BttLanguage TargetLanguage { get; init; }
    public string SourceText { get; init; } = "";
    public DialogueTemplate TargetTemplate { get; init; } = DialogueTemplate.Empty;
    public string MatchKind { get; init; } = "";
}

public sealed class DialogueTranslationState
{
    public DialogueTranslationStatus Status { get; init; } = DialogueTranslationStatus.Idle;
    public string SpeakerName { get; init; } = "";
    public string SourceText { get; init; } = "";
    public DialogueTemplate TargetTemplate { get; init; } = DialogueTemplate.Empty;
    public string TargetText { get; init; } = "";
    public string MatchKind { get; init; } = "";
    public string Key { get; init; } = "";
}
