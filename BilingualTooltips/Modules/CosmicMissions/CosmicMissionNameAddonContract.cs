namespace BilingualTooltips.Modules;

readonly record struct CosmicMissionNameAddonContract(
    string AddonName,
    GameUiAddonLocatorPolicy LocatorPolicy,
    uint NativeNameNodeId,
    uint PluginNameTranslationNodeId,
    ushort TranslationHeight)
{
    private const uint SharedPluginNameTranslationNodeId = 1270;

    public static CosmicMissionNameAddonContract Mission { get; } = new(
        "WKSMission",
        GameUiAddonLocatorPolicy.TopLevelAddonByName,
        43,
        SharedPluginNameTranslationNodeId,
        44);
}
