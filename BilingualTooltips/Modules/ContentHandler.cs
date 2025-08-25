using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Excel;
using Miosuke;
using Dalamud.Game.Gui;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game;


namespace BilingualTooltips.Modules;

public partial class ContentHandler
{
    private BilingualTooltipsPlugin plugin;

    public ContentHandler(BilingualTooltipsPlugin plugin)
    {
        this.plugin = plugin;
    }


    public void Dispose()
    {
    }


    public void StartHook()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "ContentsFinder", ContentsFinderHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ContentsFinderConfirm", ContentsFinderConfirmHandler);
    }


    public void StopHook()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "ContentsFinder", ContentsFinderHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "ContentsFinderConfirm", ContentsFinderConfirmHandler);
    }
}
