using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel;
using Miosuke;
using Dalamud.Game.Gui;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace BilingualTooltips;

public partial class TooltipHandler
{
    private Plugin plugin;

    public TooltipHandler(Plugin plugin)
    {
        this.plugin = plugin;
    }

    public void Dispose()
    {
        ResetItemTooltip();
        ResetActionTooltip();
    }


    public void StartHook()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "ItemDetail", ItemDetail_PreRequestedUpdate_Handler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "ActionDetail", ActionDetail_PreRequestedUpdate_Handler);
    }


    public void StopHook()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "ItemDetail", ItemDetail_PreRequestedUpdate_Handler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "ActionDetail", ActionDetail_PreRequestedUpdate_Handler);
    }


    private unsafe AtkTextNode* GetNodeByNodeId(AtkUnitBase* addon, int nodeId)
    {
        AtkTextNode* customNode = null;
        for (var i = 0; i < addon->UldManager.NodeListCount; i++)
        {
            var node = addon->UldManager.NodeList[i];
            if (node == null || node->NodeId != nodeId)
                continue;
            return (AtkTextNode*)node;
        }
        return null;
    }
}
