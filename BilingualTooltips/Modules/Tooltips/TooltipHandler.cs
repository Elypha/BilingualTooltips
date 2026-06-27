using BilingualTooltips.Modules.Lookup;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Gui;
using Miosuke.UserActions;

namespace BilingualTooltips.Modules;

enum TooltipDetailAddon
{
    ItemDetail,
    ActionDetail,
}

public sealed class TooltipHandler
{
    private readonly BilingualTooltipsPlugin _plugin;
    private readonly TooltipDetailController _itemDetailController;
    private readonly TooltipDetailController _actionDetailController;

    public TooltipHandler(BilingualTooltipsPlugin plugin)
    {
        _plugin = plugin;
        _itemDetailController = new TooltipDetailController(
            TooltipDetailAddonContract.ItemDetail,
            ShouldShowTranslations,
            ResolveItemDetailContent,
            GetItemDetailAppearance);
        _actionDetailController = new TooltipDetailController(
            TooltipDetailAddonContract.ActionDetail,
            ShouldShowTranslations,
            ResolveActionDetailContent,
            GetActionDetailAppearance);
    }

    public void StartHook()
    {
        Register(_itemDetailController);
        Register(_actionDetailController);
    }

    public void StopHook()
    {
        Unregister(_itemDetailController);
        Unregister(_actionDetailController);
    }

    public void Dispose()
    {
        StopHook();
        _itemDetailController.ReleaseCurrent();
        _actionDetailController.ReleaseCurrent();
    }

    internal void ReleaseCurrent(TooltipDetailAddon addon, TooltipDetailParts parts = TooltipDetailParts.All)
    {
        ControllerFor(addon).ReleaseCurrent(parts);
    }

    internal void ReleaseAllCurrent()
    {
        _itemDetailController.ReleaseCurrent();
        _actionDetailController.ReleaseCurrent();
    }

    private TooltipDetailController ControllerFor(TooltipDetailAddon addon) => addon switch
    {
        TooltipDetailAddon.ItemDetail => _itemDetailController,
        TooltipDetailAddon.ActionDetail => _actionDetailController,
        _ => throw new ArgumentOutOfRangeException(nameof(addon), addon, null),
    };

    private static void Register(TooltipDetailController controller)
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, controller.AddonName, controller.PreRequestedUpdateHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, controller.AddonName, controller.PostRequestedUpdateHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreHide, controller.AddonName, controller.CleanupHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, controller.AddonName, controller.CleanupHandler);
    }

    private static void Unregister(TooltipDetailController controller)
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, controller.AddonName, controller.PreRequestedUpdateHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, controller.AddonName, controller.PostRequestedUpdateHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreHide, controller.AddonName, controller.CleanupHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, controller.AddonName, controller.CleanupHandler);
    }

    private bool ShouldShowTranslations() =>
        !_plugin.Config.TooltipShortcutEnabled
        || Hotkey.IsActive(_plugin.Config.TooltipShortcutHotkey);

    private TooltipDetailContent? ResolveItemDetailContent()
    {
        var nameLanguage = _plugin.Config.LanguageItemTooltipName;
        var descriptionLanguage = _plugin.Config.LanguageItemTooltipDescription;
        if (nameLanguage == BttLanguage.Off && descriptionLanguage == BttLanguage.Off) return null;

        var itemId = Service.GameGui.HoveredItem;
        if (itemId == 0) return null;

        var name = nameLanguage != BttLanguage.Off
            && GameTextResolver.TryGetItemTooltip(itemId, nameLanguage, out var nameText)
                ? nameText.Name
                : string.Empty;
        var description = descriptionLanguage != BttLanguage.Off
            && GameTextResolver.TryGetItemTooltip(itemId, descriptionLanguage, out var descriptionText)
                ? descriptionText.Description
                : string.Empty;

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description)) return null;

        _plugin.RecordLookup(BttLookupKey.Item(itemId), ResolveItemDisplayName(itemId));
        return new TooltipDetailContent(name, description);
    }

    private TooltipDetailContent? ResolveActionDetailContent()
    {
        var nameLanguage = _plugin.Config.LanguageActionTooltipName;
        var descriptionLanguage = _plugin.Config.LanguageActionTooltipDescription;
        if (nameLanguage == BttLanguage.Off && descriptionLanguage == BttLanguage.Off) return null;

        var action = Service.GameGui.HoveredAction;
        if (action.ActionId == 0) return null;

        var name = nameLanguage != BttLanguage.Off
            && GameTextResolver.TryGetActionTooltip(action, nameLanguage, out var nameText)
                ? nameText.Name
                : string.Empty;
        var description = descriptionLanguage != BttLanguage.Off
            && GameTextResolver.TryGetActionTooltip(action, descriptionLanguage, out var descriptionText)
                ? descriptionText.Description
                : string.Empty;

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description)) return null;

        if (GameTextResolver.TryGetActionReference(action, out var reference))
        {
            _plugin.RecordLookup(BttLookupKey.Action(reference), ResolveActionDisplayName(action));
        }

        return new TooltipDetailContent(name, description);
    }

    private TooltipDetailAppearance GetItemDetailAppearance() => new(
        (ushort)_plugin.Config.ItemNameColourKey,
        (ushort)_plugin.Config.ItemDescriptionColourKey,
        _plugin.Config.OffsetItemNameTranslation,
        _plugin.Config.OffsetItemNameNative,
        _plugin.Config.TooltipNameMaxLineWidth);

    private TooltipDetailAppearance GetActionDetailAppearance() => new(
        (ushort)_plugin.Config.ActionNameColourKey,
        (ushort)_plugin.Config.ActionDescriptionColourKey,
        _plugin.Config.OffsetActionNameTranslation,
        _plugin.Config.OffsetActionNameNative,
        _plugin.Config.TooltipNameMaxLineWidth);

    private static string ResolveItemDisplayName(ulong itemId)
    {
        var clientLanguage = BttLanguageExtensions.FromClientLanguage(Service.ClientState.ClientLanguage);
        if (GameTextResolver.TryGetItemTooltip(itemId, clientLanguage, out var clientText)
            && !string.IsNullOrWhiteSpace(clientText.Name))
            return clientText.Name;

        return GameTextResolver.TryGetItemTooltip(itemId, BttLanguage.English, out var englishText)
            && !string.IsNullOrWhiteSpace(englishText.Name)
                ? englishText.Name
                : "Item";
    }

    private static string ResolveActionDisplayName(HoveredAction action)
    {
        var clientLanguage = BttLanguageExtensions.FromClientLanguage(Service.ClientState.ClientLanguage);
        if (GameTextResolver.TryGetActionTooltip(action, clientLanguage, out var clientText)
            && !string.IsNullOrWhiteSpace(clientText.Name))
            return clientText.Name;

        return GameTextResolver.TryGetActionTooltip(action, BttLanguage.English, out var englishText)
            && !string.IsNullOrWhiteSpace(englishText.Name)
                ? englishText.Name
                : "Action";
    }
}
