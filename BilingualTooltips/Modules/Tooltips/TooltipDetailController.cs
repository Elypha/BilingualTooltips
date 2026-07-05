using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace BilingualTooltips.Modules;

[Flags]
enum TooltipDetailParts
{
    Name = 1,
    Description = 2,
    All = Name | Description,
}

sealed unsafe class TooltipDetailController
{
    private readonly TooltipDetailAddonContract _addonContract;
    private readonly TooltipDetailMutation _mutation;
    private readonly Func<bool> _shouldShowTranslations;
    private readonly Func<TooltipDetailContent?> _resolveContent;
    private readonly Func<TooltipDetailAppearance> _getAppearance;
    private TooltipUpdateTransaction? _pendingUpdate;

    public TooltipDetailController(
        TooltipDetailAddonContract addonContract,
        Func<bool> shouldShowTranslations,
        Func<TooltipDetailContent?> resolveContent,
        Func<TooltipDetailAppearance> getAppearance)
    {
        _addonContract = addonContract;
        _mutation = new TooltipDetailMutation(addonContract);
        _shouldShowTranslations = shouldShowTranslations;
        _resolveContent = resolveContent;
        _getAppearance = getAppearance;
    }

    public string AddonName => _addonContract.AddonName;

    public void ReleaseCurrent(TooltipDetailParts parts = TooltipDetailParts.All)
    {
        _pendingUpdate = null;
        var addon = LocateTopLevelAddon();
        Release(addon, parts);
    }

    public void PreRequestedUpdateHandler(AddonEvent type, AddonArgs args)
    {
        _pendingUpdate = null;

        var addon = (AtkUnitBase*)args.Addon.Address;
        _mutation.Release(addon);

        if (addon == null
            || !addon->IsVisible
            || !_shouldShowTranslations()) return;

        var content = _resolveContent();
        if (content is not { IsEmpty: false } resolvedContent) return;

        _pendingUpdate = new TooltipUpdateTransaction((nint)addon, resolvedContent);
    }

    public void PostRequestedUpdateHandler(AddonEvent type, AddonArgs args)
    {
        try
        {
            var addon = (AtkUnitBase*)args.Addon.Address;
            if (addon == null
                || !addon->IsVisible) return;

            if (_pendingUpdate is not { } pendingUpdate
                || pendingUpdate.AddonAddress != (nint)addon) return;

            var appearance = _getAppearance();
            _mutation.ApplyName(
                addon,
                pendingUpdate.Content.Name,
                appearance.NameColourKey,
                appearance.NameTranslationOffset,
                appearance.NativeNameOffset,
                appearance.NameMaxLineWidth);
            _mutation.ApplyDescription(
                addon,
                pendingUpdate.Content.Description,
                appearance.DescriptionColourKey,
                appearance.DescriptionDividerGapBefore,
                appearance.DescriptionDividerGapAfter);
        }
        finally
        {
            _pendingUpdate = null;
        }
    }

    public void CleanupHandler(AddonEvent type, AddonArgs args)
    {
        _pendingUpdate = null;
        var addon = (AtkUnitBase*)args.Addon.Address;
        _mutation.Release(addon);
    }

    private void Release(AtkUnitBase* addon, TooltipDetailParts parts)
    {
        if (parts.HasFlag(TooltipDetailParts.Description)) _mutation.ReleaseDescription(addon);
        if (parts.HasFlag(TooltipDetailParts.Name)) _mutation.ReleaseName(addon);
    }

    private AtkUnitBase* LocateTopLevelAddon() =>
        (AtkUnitBase*)Service.GameGui.GetAddonByName(_addonContract.AddonName).Address;

    private readonly record struct TooltipUpdateTransaction(nint AddonAddress, TooltipDetailContent Content);
}

readonly record struct TooltipDetailContent(string Name, string Description)
{
    public bool IsEmpty => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Description);
}

readonly record struct TooltipDetailAppearance(
    ushort NameColourKey,
    ushort DescriptionColourKey,
    float NameTranslationOffset,
    float NativeNameOffset,
    float NameMaxLineWidth,
    float DescriptionDividerGapBefore,
    float DescriptionDividerGapAfter);
