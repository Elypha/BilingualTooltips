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

public partial class TooltipHandler
{
    private BilingualTooltipsPlugin plugin;

    public TooltipHandler(BilingualTooltipsPlugin plugin)
    {
        this.plugin = plugin;
    }


    public void Dispose()
    {
        ResetItemTooltip();
        ResetActionTooltip();
    }

    public enum ItemDetailTextNode
    {
        Name = 33,
        Description = 42,
        NameTranslated = 1270,
    }

    public enum ActionDescriptionNode
    {
        Name = 5,
        Description = 19,
        NameTranslated = 1270,
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
