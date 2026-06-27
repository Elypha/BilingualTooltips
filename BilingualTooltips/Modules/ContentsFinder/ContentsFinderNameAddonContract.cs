namespace BilingualTooltips.Modules;

readonly record struct ContentsFinderNameAddonContract(
    string AddonName,
    GameUiAddonLocatorPolicy LocatorPolicy,
    uint NativeNameNodeId,
    uint PluginNameTranslationNodeId,
    ushort TranslationWidth,
    ushort TranslationHeight)
{
    private const uint SharedPluginNameTranslationNodeId = 1270;

    public static ContentsFinderNameAddonContract JournalDetail { get; } = new(
        "JournalDetail",
        GameUiAddonLocatorPolicy.ChildAddonByParentIdAndName,
        38,
        SharedPluginNameTranslationNodeId,
        340,
        50);

    public static ContentsFinderNameAddonContract ContentsFinderConfirm { get; } = new(
        "ContentsFinderConfirm",
        GameUiAddonLocatorPolicy.TopLevelAddonByName,
        49,
        SharedPluginNameTranslationNodeId,
        318,
        44);
}
