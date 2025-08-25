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
using Miosuke.Action;
using Miosuke;
using Dalamud.Game.Gui;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Miosuke;
using BilingualTooltips.Modules;


namespace BilingualTooltips.Addons;

public class ActionDetailAddon
{
    private BilingualTooltipsPlugin plugin;

    public ActionDetailAddon(BilingualTooltipsPlugin plugin)
    {
        this.plugin = plugin;
    }

    public void Dispose()
    {
        ResetActionNameTextNode();
    }


    private string actionNameTranslation = "";
    private string actionDescTranslation = "";
    private bool UpdateTranslations()
    {
        // get id
        var action = Service.GameGui.HoveredAction;
        if (action.ActionID == 0) return false;

        // update translations
        actionNameTranslation = SheetHelper.GetActionName(action, plugin.Config.LanguageActionTooltipName) ?? "";
        actionDescTranslation = SheetHelper.GetActionDescription(action, plugin.Config.LanguageActionTooltipDescription) ?? "";
        var mnemonic = Service.ClientState.LocalPlayer?.ClassJob.Value.Abbreviation.ToString();

        // update translations on the panel
        P.ItemTooltipPanel.NameJa = SheetHelper.GetActionName(action, GameLanguage.Japanese)!;
        P.ItemTooltipPanel.NameEn = SheetHelper.GetActionName(action, GameLanguage.English)!;
        P.ItemTooltipPanel.NameDe = SheetHelper.GetActionName(action, GameLanguage.German)!;
        P.ItemTooltipPanel.NameFr = SheetHelper.GetActionName(action, GameLanguage.French)!;
        P.ItemTooltipPanel.DescJa = SheetHelper.GetActionDescription(action, GameLanguage.Japanese)!;
        P.ItemTooltipPanel.DescEn = SheetHelper.GetActionDescription(action, GameLanguage.English)!;
        P.ItemTooltipPanel.DescDe = SheetHelper.GetActionDescription(action, GameLanguage.German)!;
        P.ItemTooltipPanel.DescFr = SheetHelper.GetActionDescription(action, GameLanguage.French)!;

        return true;
    }


    public unsafe void ResetActionNameTextNode()
    {
        var addon = Service.GameGui.GetAddonByName(AddonHelper.ActionDetail.Name);
        var addonPtr = (AtkUnitBase*)addon.Address;

        // remove translation if it exists
        var customNode = AddonHelper.GetNodeByNodeId(addonPtr, AddonHelper.ActionDetail.TextNodeId.NameTranslation);
        if (customNode != null)
        {
            if (customNode->AtkResNode.IsVisible())
            {
                var insertNode = addonPtr->GetNodeById(2);
                if (insertNode == null) return;
                customNode->AtkResNode.ToggleVisibility(false);
            }
        }

        // reset original name position
        var name_node = addonPtr->GetTextNodeById(AddonHelper.ActionDetail.TextNodeId.Name);
        float x, y;
        name_node->GetPositionFloat(&x, &y);
        name_node->SetPositionFloat(x, 14);
    }

    public unsafe void PreRequestedUpdateHandler(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRequestedUpdateArgs requestedUpdateArgs) return;
        if (!plugin.Config.Enabled) return;

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (!addon->IsVisible) return;

        // reset before updating
        if (plugin.Config.LanguageActionTooltipName != GameLanguage.Off) ResetActionNameTextNode();

        // skip if enable on hotkey only
        if (plugin.Config.TemporaryEnableOnly && !Hotkey.IsActive(plugin.Config.TemporaryEnableHotkey)) return;

        var numberArrayData = ((NumberArrayData**)requestedUpdateArgs.NumberArrayData)[AddonHelper.ActionDetail.NumberArrayIndex.Self];
        var stringArrayData = ((StringArrayData**)requestedUpdateArgs.StringArrayData)[AddonHelper.ActionDetail.StringArrayIndex.Self];
        if (UpdateTranslations())
        {
            if ((plugin.Config.LanguageActionTooltipDescription != GameLanguage.Off) && !string.IsNullOrEmpty(actionDescTranslation))
            {
                AddActionDescriptionTranslation(addon, stringArrayData);
            }

            if ((plugin.Config.LanguageActionTooltipName != GameLanguage.Off) && !string.IsNullOrEmpty(actionNameTranslation))
            {
                AddActionNameTranslation(addon);
            }
        }
    }


    private unsafe void AddActionNameTranslation(AtkUnitBase* addon)
    {
        var baseTextNode = addon->GetTextNodeById(AddonHelper.ActionDetail.TextNodeId.Name);
        if (baseTextNode == null) return;

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var textNode = AddonHelper.GetNodeByNodeId(addon, AddonHelper.ActionDetail.TextNodeId.NameTranslation);
        if (textNode == null)
        {
            AddonHelper.SetupTextNodeTooltip(addon, baseTextNode, insertNode, AddonHelper.ActionDetail.TextNodeId.NameTranslation);
        }
        textNode->AtkResNode.ToggleVisibility(true);

        var lines = new SeString();
        lines.Payloads.Add(new UIForegroundPayload((ushort)plugin.Config.ActionNameColourKey));
        lines.Payloads.Add(new TextPayload($"{actionNameTranslation}"));
        lines.Payloads.Add(new UIForegroundPayload(0));
        textNode->SetText(lines.Encode());

        textNode->ResizeNodeForCurrentText();
        textNode->SetWidth(300);
        textNode->SetHeight(21);

        var itemNameNode = addon->GetTextNodeById(AddonHelper.ActionDetail.TextNodeId.Name);

        var textNodeOffset = plugin.Config.OffsetActionNameTranslation;
        textNode->AtkResNode.SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + textNodeOffset);

        var itemNameOffset = plugin.Config.OffsetActionNameOriginal;
        itemNameNode->SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + itemNameOffset);
    }


    private unsafe void AddActionDescriptionTranslation(AtkUnitBase* addon, StringArrayData* stringArrayData)
    {
        var descriptionNode = addon->GetTextNodeById(AddonHelper.ActionDetail.TextNodeId.Description);
        if (descriptionNode == null) return;

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var addr = new nint(stringArrayData->StringArray[AddonHelper.ActionDetail.StringArrayIndex.Description]);
        var currentText = MemoryHelper.ReadSeStringNullTerminated(addr);

        if (currentText.Payloads.Count >= 1 && currentText.Payloads[0] is UIForegroundPayload foregroundPayload && foregroundPayload.ColorKey == plugin.Config.ActionDescriptionColourKey)
        {
            return;
        }
        currentText.Payloads.Insert(0, new UIForegroundPayload((ushort)plugin.Config.ActionDescriptionColourKey));
        currentText.Payloads.Insert(1, new TextPayload($"{actionDescTranslation}\n\n"));
        currentText.Payloads.Insert(2, new UIForegroundPayload(0));

        stringArrayData->SetValue(AddonHelper.ActionDetail.StringArrayIndex.Description, currentText.Encode(), false, true, true);
    }
}
