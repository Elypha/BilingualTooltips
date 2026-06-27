using BilingualTooltips.Modules.Lookup;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace BilingualTooltips.Modules;

sealed unsafe class ContentsFinderNameController
{
    private readonly ContentsFinderNameAddonContract _addonContract;
    private readonly ContentsFinderNameMutation _mutation;
    private readonly Func<bool> _shouldShowTranslation;
    private readonly Func<ContentsFinderNameContent?> _resolveContent;
    private readonly Func<ContentsFinderNameAppearance> _getAppearance;
    private readonly AddonLocator _locateAddon;
    private readonly Action<ContentsFinderNameContent> _onApplied;
    private AppliedState? _appliedState;

    public ContentsFinderNameController(
        ContentsFinderNameAddonContract addonContract,
        Func<bool> shouldShowTranslation,
        Func<ContentsFinderNameContent?> resolveContent,
        Func<ContentsFinderNameAppearance> getAppearance,
        AddonLocator locateAddon,
        Action<ContentsFinderNameContent> onApplied)
    {
        _addonContract = addonContract;
        _mutation = new ContentsFinderNameMutation(addonContract);
        _shouldShowTranslation = shouldShowTranslation;
        _resolveContent = resolveContent;
        _getAppearance = getAppearance;
        _locateAddon = locateAddon;
        _onApplied = onApplied;
    }

    public string AddonName => _addonContract.AddonName;

    public void ApplyCurrent() => Apply(_locateAddon());
    public void PostSetupHandler(AddonEvent type, AddonArgs args) => Apply((AtkUnitBase*)args.Addon.Address);
    public void CleanupHandler(AddonEvent type, AddonArgs args) => Release((AtkUnitBase*)args.Addon.Address);
    public void ReleaseCurrent() => Release(_locateAddon());

    private void Apply(AtkUnitBase* addon)
    {
        if (addon == null || !addon->IsVisible || !_shouldShowTranslation())
        {
            Release(addon);
            return;
        }

        var content = _resolveContent();
        if (content is not { IsEmpty: false } resolvedContent)
        {
            Release(addon);
            return;
        }

        var addonAddress = (nint)addon;
        if (_appliedState is { } state
            && state.AddonAddress == addonAddress
            && state.ContentKey == resolvedContent.Key
            && state.Translation == resolvedContent.Translation)
        {
            return;
        }

        Release(addon);
        if (!_mutation.Apply(addon, resolvedContent.Translation, _getAppearance())) return;

        _appliedState = new AppliedState(addonAddress, resolvedContent.Key, resolvedContent.Translation);
        _onApplied(resolvedContent);
    }

    private void Release(AtkUnitBase* addon)
    {
        _mutation.Release(addon);
        if (addon == null || _appliedState is null || _appliedState.AddonAddress == (nint)addon)
        {
            _appliedState = null;
        }
    }

    private sealed record AppliedState(nint AddonAddress, BttLookupKey ContentKey, string Translation);

    public delegate AtkUnitBase* AddonLocator();
}

readonly record struct ContentsFinderNameContent(
    BttLookupKey Key,
    string DisplayName,
    string Translation)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Translation);
}

readonly record struct ContentsFinderNameAppearance(
    ushort ColourKey,
    float NativeNameOffset,
    float TranslationOffset);
