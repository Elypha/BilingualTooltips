using BilingualTooltips.Modules.Lookup;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace BilingualTooltips.Modules;

public unsafe partial class ContentsFinderHandler
{
    private readonly object _contentsFinderConfirmIdentityLock = new();
    private ContentsFinderConfirmIdentity? _contentsFinderConfirmIdentity;

    private void ContentsFinderConfirmPostSetupHandler(AddonEvent type, AddonArgs args)
    {
        _contentsFinderConfirm.PostSetupHandler(type, args);
    }

    private void ContentsFinderConfirmCleanupHandler(AddonEvent type, AddonArgs args)
    {
        _contentsFinderConfirm.CleanupHandler(type, args);
        ClearContentsFinderConfirmIdentity();
    }

    private void ClientStateOnCfPop(ContentFinderCondition condition)
    {
        if (_disposed || !GameTextResolver.TryGetContentFinderConditionReference(condition.RowId, out var reference))
            return;

        var displayName = condition.Name.ExtractText();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = BttLookupDisplay.DisplayId(BttLookupKey.Content(reference));
        }

        lock (_contentsFinderConfirmIdentityLock)
        {
            _contentsFinderConfirmIdentity = new ContentsFinderConfirmIdentity(reference, displayName);
        }

        _ = Service.Framework.RunOnTick(ApplyContentsFinderConfirmFromCfPop, delayTicks: 1);
    }

    private void ApplyContentsFinderConfirmFromCfPop()
    {
        if (_disposed) return;

        _contentsFinderConfirm.ApplyCurrent();
    }

    private ContentsFinderNameContent? ResolveContentsFinderConfirmContent() =>
        GetContentsFinderConfirmIdentity() is { } identity
            ? ResolveContentName(identity.Reference, identity.DisplayName)
            : null;

    private ContentsFinderConfirmIdentity? GetContentsFinderConfirmIdentity()
    {
        lock (_contentsFinderConfirmIdentityLock)
        {
            return _contentsFinderConfirmIdentity;
        }
    }

    private void ClearContentsFinderConfirmIdentity()
    {
        lock (_contentsFinderConfirmIdentityLock)
        {
            _contentsFinderConfirmIdentity = null;
        }
    }

    private static AtkUnitBase* LocateTopLevelContentsFinderConfirmAddon() =>
        (AtkUnitBase*)Service.GameGui.GetAddonByName("ContentsFinderConfirm").Address;

    private sealed record ContentsFinderConfirmIdentity(GameTextResolver.ContentReference Reference, string DisplayName);
}
