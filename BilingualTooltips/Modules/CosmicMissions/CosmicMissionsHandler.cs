using BilingualTooltips.Modules.Lookup;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace BilingualTooltips.Modules;

public unsafe class CosmicMissionsHandler
{
    private readonly BilingualTooltipsPlugin _plugin;
    private readonly CosmicMissionNameController _mission;
    private bool _disposed;

    public CosmicMissionsHandler(BilingualTooltipsPlugin plugin)
    {
        _plugin = plugin;
        _mission = new CosmicMissionNameController(
            CosmicMissionNameAddonContract.Mission,
            ShouldShowCosmicMissionName,
            ResolveSelectedMissionContent,
            GetCosmicMissionNameAppearance,
            LocateTopLevelMissionAddon,
            RecordCosmicMissionLookup);
    }

    public void StartHook()
    {
        _disposed = false;
        RegisterAddon(_mission);
    }

    public void StopHook()
    {
        UnregisterAddon(_mission);
    }

    public void Dispose()
    {
        _disposed = true;
        ReleaseAllCurrent();
    }

    public void ReleaseAllCurrent()
    {
        _mission.ReleaseCurrent();
    }

    private static void RegisterAddon(CosmicMissionNameController controller)
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, controller.AddonName, controller.PostSetupHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, controller.AddonName, controller.PostRefreshHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreHide, controller.AddonName, controller.CleanupHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, controller.AddonName, controller.CleanupHandler);
    }

    private static void UnregisterAddon(CosmicMissionNameController controller)
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, controller.AddonName, controller.PostSetupHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, controller.AddonName, controller.PostRefreshHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreHide, controller.AddonName, controller.CleanupHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, controller.AddonName, controller.CleanupHandler);
    }

    private bool ShouldShowCosmicMissionName() =>
        !_disposed && _plugin.Config.CosmicMissionNameLanguage != BttLanguage.Off;

    private CosmicMissionNameAppearance GetCosmicMissionNameAppearance() =>
        new(
            (ushort)_plugin.Config.CosmicMissionNameColourKey,
            _plugin.Config.OffsetCosmicMissionNameNative,
            _plugin.Config.OffsetCosmicMissionNameTranslation);

    private CosmicMissionNameContent? ResolveSelectedMissionContent()
    {
        var agent = AgentWKSMission.Instance();
        if (agent == null || agent->Data == null) return null;

        var missionUnitId = agent->Data->SelectedMissionUnitId;
        var sourceTitle = agent->Data->SelectedMissionTitle.ToString();
        return ResolveCosmicMissionContent(missionUnitId, sourceTitle);
    }

    private CosmicMissionNameContent? ResolveCosmicMissionContent(uint missionUnitId, string sourceTitle)
    {
        if (!GameTextResolver.TryGetCosmicMissionReference(missionUnitId, out var reference))
            return null;

        var key = BttLookupKey.Content(reference);
        var displayName = CleanMissionNameForDisplay(ResolveCurrentClientMissionName(reference, sourceTitle));
        if (string.IsNullOrWhiteSpace(displayName)) return null;

        if (!GameTextResolver.TryGetContentName(reference, _plugin.Config.CosmicMissionNameLanguage, out var translation))
            return null;

        translation = CleanMissionNameForDisplay(translation);
        if (string.IsNullOrWhiteSpace(translation)) return null;

        return new CosmicMissionNameContent(key, displayName, translation);
    }

    private static string ResolveCurrentClientMissionName(GameTextResolver.ContentReference reference, string fallback)
    {
        var language = BttLanguageExtensions.FromClientLanguage(Service.ClientState.ClientLanguage);
        if (GameTextResolver.TryGetContentName(reference, language, out var clientName)
            && !string.IsNullOrWhiteSpace(clientName))
        {
            return clientName.Trim();
        }

        fallback = fallback.Trim();
        return string.IsNullOrWhiteSpace(fallback) ? BttLookupDisplay.DisplayId(BttLookupKey.Content(reference)) : fallback;
    }

    private static string CleanMissionNameForDisplay(string text)
    {
        text = text.Trim();
        var firstTextIndex = 0;
        while (firstTextIndex < text.Length
            && (IsUiIconGlyph(text[firstTextIndex]) || char.IsWhiteSpace(text[firstTextIndex])))
        {
            firstTextIndex++;
        }

        return firstTextIndex == 0 ? text : text[firstTextIndex..].Trim();
    }

    private static bool IsUiIconGlyph(char value) =>
        value is >= '\uE000' and <= '\uF8FF';

    private void RecordCosmicMissionLookup(CosmicMissionNameContent content)
    {
        _plugin.RecordLookup(content.Key, content.DisplayName);
    }

    private static AtkUnitBase* LocateTopLevelMissionAddon() =>
        (AtkUnitBase*)Service.GameGui.GetAddonByName("WKSMission").Address;
}
