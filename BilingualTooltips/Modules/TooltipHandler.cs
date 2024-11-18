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
using Dalamud.Game;


namespace BilingualTooltips;

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


    public void StartHook()
    {
        UpdateSheetItemName(plugin.Config.LanguageItemTooltipName);
        UpdateSheetItemDescription(plugin.Config.LanguageItemTooltipDescription);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "ItemDetail", ItemDetail_PreRequestedUpdate_Handler);

        UpdateSheetActionName(plugin.Config.LanguageActionTooltipName);
        UpdateSheetActionDescription(plugin.Config.LanguageActionTooltipDescription);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "ActionDetail", ActionDetail_PreRequestedUpdate_Handler);
    }


    public void StopHook()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "ItemDetail", ItemDetail_PreRequestedUpdate_Handler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "ActionDetail", ActionDetail_PreRequestedUpdate_Handler);
    }


    public void UpdateSheetItemName(GameLanguage lang)
    {
        plugin.TooltipHandler.SheetNameItem = lang switch
        {
            GameLanguage.Japanese => Service.Data.GetExcelSheet<Item>(ClientLanguage.Japanese)!,
            GameLanguage.English => Service.Data.GetExcelSheet<Item>(ClientLanguage.English)!,
            GameLanguage.German => Service.Data.GetExcelSheet<Item>(ClientLanguage.German)!,
            GameLanguage.French => Service.Data.GetExcelSheet<Item>(ClientLanguage.French)!,
            _ => Service.Data.GetExcelSheet<Item>(ClientLanguage.Japanese)!,
        };
    }

    public void UpdateSheetItemDescription(GameLanguage lang)
    {
        plugin.TooltipHandler.SheetDescItem = lang switch
        {
            GameLanguage.Japanese => Service.Data.GetExcelSheet<Item>(ClientLanguage.Japanese)!,
            GameLanguage.English => Service.Data.GetExcelSheet<Item>(ClientLanguage.English)!,
            GameLanguage.German => Service.Data.GetExcelSheet<Item>(ClientLanguage.German)!,
            GameLanguage.French => Service.Data.GetExcelSheet<Item>(ClientLanguage.French)!,
            _ => Service.Data.GetExcelSheet<Item>(ClientLanguage.Japanese)!,
        };
    }

    public void UpdateSheetActionName(GameLanguage lang)
    {
        plugin.TooltipHandler.SheetNameAction = lang switch
        {
            GameLanguage.Japanese => Service.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>(ClientLanguage.Japanese)!,
            GameLanguage.English => Service.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>(ClientLanguage.English)!,
            GameLanguage.German => Service.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>(ClientLanguage.German)!,
            GameLanguage.French => Service.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>(ClientLanguage.French)!,
            _ => Service.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>(ClientLanguage.Japanese)!,
        };
        plugin.TooltipHandler.SheetNameGeneralAction = lang switch
        {
            GameLanguage.Japanese => Service.Data.GetExcelSheet<GeneralAction>(ClientLanguage.Japanese)!,
            GameLanguage.English => Service.Data.GetExcelSheet<GeneralAction>(ClientLanguage.English)!,
            GameLanguage.German => Service.Data.GetExcelSheet<GeneralAction>(ClientLanguage.German)!,
            GameLanguage.French => Service.Data.GetExcelSheet<GeneralAction>(ClientLanguage.French)!,
            _ => Service.Data.GetExcelSheet<GeneralAction>(ClientLanguage.Japanese)!,
        };
        plugin.TooltipHandler.SheetNameTrait = lang switch
        {
            GameLanguage.Japanese => Service.Data.GetExcelSheet<Trait>(ClientLanguage.Japanese)!,
            GameLanguage.English => Service.Data.GetExcelSheet<Trait>(ClientLanguage.English)!,
            GameLanguage.German => Service.Data.GetExcelSheet<Trait>(ClientLanguage.German)!,
            GameLanguage.French => Service.Data.GetExcelSheet<Trait>(ClientLanguage.French)!,
            _ => Service.Data.GetExcelSheet<Trait>(ClientLanguage.Japanese)!,
        };
    }

    public void UpdateSheetActionDescription(GameLanguage lang)
    {
        plugin.TooltipHandler.SheetDescActionTransient = lang switch
        {
            GameLanguage.Japanese => Service.Data.GetExcelSheet<ActionTransient>(ClientLanguage.Japanese)!,
            GameLanguage.English => Service.Data.GetExcelSheet<ActionTransient>(ClientLanguage.English)!,
            GameLanguage.German => Service.Data.GetExcelSheet<ActionTransient>(ClientLanguage.German)!,
            GameLanguage.French => Service.Data.GetExcelSheet<ActionTransient>(ClientLanguage.French)!,
            _ => Service.Data.GetExcelSheet<ActionTransient>(ClientLanguage.Japanese)!,
        };
        plugin.TooltipHandler.SheetDescGeneralAction = lang switch
        {
            GameLanguage.Japanese => Service.Data.GetExcelSheet<GeneralAction>(ClientLanguage.Japanese)!,
            GameLanguage.English => Service.Data.GetExcelSheet<GeneralAction>(ClientLanguage.English)!,
            GameLanguage.German => Service.Data.GetExcelSheet<GeneralAction>(ClientLanguage.German)!,
            GameLanguage.French => Service.Data.GetExcelSheet<GeneralAction>(ClientLanguage.French)!,
            _ => Service.Data.GetExcelSheet<GeneralAction>(ClientLanguage.Japanese)!,
        };
        plugin.TooltipHandler.SheetDescTraitTransient = lang switch
        {
            GameLanguage.Japanese => Service.Data.GetExcelSheet<TraitTransient>(ClientLanguage.Japanese)!,
            GameLanguage.English => Service.Data.GetExcelSheet<TraitTransient>(ClientLanguage.English)!,
            GameLanguage.German => Service.Data.GetExcelSheet<TraitTransient>(ClientLanguage.German)!,
            GameLanguage.French => Service.Data.GetExcelSheet<TraitTransient>(ClientLanguage.French)!,
            _ => Service.Data.GetExcelSheet<TraitTransient>(ClientLanguage.Japanese)!,
        };
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
