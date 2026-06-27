using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace BilingualTooltips.Modules;

public unsafe partial class ContentsFinderHandler
{
    private ushort _contentsFinderAddonId;

    private void ContentsFinderRefreshHandler(AddonEvent type, AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null || !addon->IsVisible)
        {
            ReleaseJournalDetail();
            _contentsFinderAddonId = 0;
            return;
        }

        _contentsFinderAddonId = addon->Id;
        _journalDetail.ApplyCurrent();
    }

    private void ContentsFinderCleanupHandler(AddonEvent type, AddonArgs args)
    {
        ReleaseJournalDetail();
        _contentsFinderAddonId = 0;
    }

    private void JournalDetailCleanupHandler(AddonEvent type, AddonArgs args)
    {
        _journalDetail.CleanupHandler(type, args);
    }

    private ContentsFinderNameContent? ResolveJournalDetailContent() =>
        GameTextResolver.TryGetSelectedContentReference(out var reference)
            ? ResolveContentName(reference, ResolveCurrentClientContentName(reference))
            : null;

    private AtkUnitBase* LocateParentScopedJournalDetailAddon()
    {
        if (_contentsFinderAddonId == 0) return null;

        var manager = (AtkUnitManager*)RaptureAtkUnitManager.Instance();
        if (manager == null) return null;

        var unitList = &manager->AllLoadedUnitsList;
        for (var i = 0; i < unitList->Count; i++)
        {
            var addon = unitList->Entries[i].Value;
            if (addon == null
                || addon->ParentId != _contentsFinderAddonId
                || addon->NameString != _journalDetail.AddonName)
            {
                continue;
            }

            return addon;
        }

        return null;
    }
}
