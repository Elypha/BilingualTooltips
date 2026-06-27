namespace BilingualTooltips.Modules;

readonly record struct TooltipDetailAddonContract(
    string AddonName,
    GameUiAddonLocatorPolicy LocatorPolicy,
    uint NativeNameNodeId,
    uint NativeDescriptionNodeId,
    uint NativeDescriptionDividerNodeId,
    uint PluginNameTranslationNodeId,
    uint PluginDescriptionTranslationNodeId,
    uint PluginDescriptionTranslationDividerNodeId)
{
    private const uint SharedPluginNameTranslationNodeId = 1270;
    private const uint SharedPluginDescriptionTranslationNodeId = 1271;
    private const uint SharedPluginDescriptionTranslationDividerNodeId = 1272;

    public static TooltipDetailAddonContract ItemDetail { get; } = new(
        "ItemDetail",
        GameUiAddonLocatorPolicy.TopLevelAddonByName,
        33,
        42,
        41,
        SharedPluginNameTranslationNodeId,
        SharedPluginDescriptionTranslationNodeId,
        SharedPluginDescriptionTranslationDividerNodeId);

    public static TooltipDetailAddonContract ActionDetail { get; } = new(
        "ActionDetail",
        GameUiAddonLocatorPolicy.TopLevelAddonByName,
        5,
        19,
        18,
        SharedPluginNameTranslationNodeId,
        SharedPluginDescriptionTranslationNodeId,
        SharedPluginDescriptionTranslationDividerNodeId);
}
