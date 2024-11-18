using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using Miosuke.Action;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Excel;
using Miosuke;
using Dalamud.Game.Gui;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace BilingualTooltips.Modules;

public partial class TooltipHandler
{
    public ExcelSheet<Item> SheetNameItem = Service.Data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.Japanese);
    public ExcelSheet<Item> SheetDescItem = Service.Data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.Japanese);

    public string itemNameTranslation = "";
    public string itemDescriptionTranslation = "";
    public bool isShowItemGlamName = true;


    private void UpdateItemTooltipData()
    {
        // get item id
        var itemId = Service.GameGui.HoveredItem;
        if (itemId < 2000000)
        {
            itemId %= 500000;
        }

        itemNameTranslation = SheetNameItem.GetRow((uint)itemId).Name.ExtractText();
        itemDescriptionTranslation = SheetDescItem.GetRow((uint)itemId).Description.ExtractText();
    }

    private void SetupItemTooltipPanel()
    {
        // if (!Hotkey.IsActive(plugin.Config.ItemTooltipPanelHotkey)) return;

        // P.itemTooltipPanel.translations.Clear();

        // if (P.Config.ItemTooltipPanelText1 != GameLanguage.Off)
        // {
        //     P.itemTooltipPanel.translations.Add($"{P.Config.ItemTooltipPanelText1}: {itemNameTranslation}");
        // }
    }


    public unsafe void ResetItemTooltip()
    {
        var addon = Service.GameGui.GetAddonByName("ItemDetail");
        var addonPtr = (AtkUnitBase*)addon;

        // remove translation if it exists
        var nameTranslationNode = GetNodeByNodeId(addonPtr, (int)ItemDetailTextNode.NameTranslated);
        if (nameTranslationNode != null)
        {
            if (nameTranslationNode->AtkResNode.IsVisible())
            {
                var insertNode = addonPtr->GetNodeById(2);
                if (insertNode == null) return;
                nameTranslationNode->AtkResNode.ToggleVisibility(false);
            }
        }

        // reset original item name position
        var name_node = addonPtr->GetTextNodeById((uint)ItemDetailTextNode.Name);
        float x, y;
        name_node->GetPositionFloat(&x, &y);
        name_node->SetPositionFloat(x, 14);
    }


    private unsafe void ItemDetail_PreRequestedUpdate_Handler(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRequestedUpdateArgs requestedUpdateArgs) return;
        if (!plugin.Config.Enabled) return;

        var addon = (AtkUnitBase*)args.Addon;
        if (!addon->IsVisible) return;

        if (plugin.Config.LanguageItemTooltipName != GameLanguage.Off) ResetItemTooltip();
        if (plugin.Config.TemporaryEnableOnly && !Hotkey.IsActive(plugin.Config.TemporaryEnableHotkey)) return;

        var numberArrayData = ((NumberArrayData**)requestedUpdateArgs.NumberArrayData)[29];
        var stringArrayData = ((StringArrayData**)requestedUpdateArgs.StringArrayData)[26];
        UpdateItemTooltipData();

        SetupItemTooltipPanel();

        if (plugin.Config.LanguageItemTooltipDescription != GameLanguage.Off)
        {
            AddItemDescriptionTranslation(addon, stringArrayData);
        }

        // dump
        // for (var i = 0; i < stringArrayData->Size; i++)
        // {
        //     var addr = new nint(stringArrayData->StringArray[i]);
        //     var seString = MemoryHelper.ReadSeStringNullTerminated(addr);
        //     Service.PluginLog.Info($"{addon->NameString} str{i}: {seString.ToJson()}");
        // }
        // LUT
        // 0: item name
        // 2: item category
        // 13: item description
        // var normalName = MemoryHelper.ReadSeStringNullTerminated(new nint(stringArrayData->StringArray[0]));
        // var glamName = MemoryHelper.ReadSeStringNullTerminated(new nint(stringArrayData->StringArray[1]));

        if (plugin.Config.LanguageItemTooltipName == GameLanguage.Off) return;

        AddItemNameTranslation(addon);
    }


    private unsafe void AddItemNameTranslation(AtkUnitBase* addon)
    {
        var textNode = GetNodeByNodeId(addon, (int)ItemDetailTextNode.NameTranslated);
        if (textNode != null) textNode->AtkResNode.ToggleVisibility(false);

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var baseTextNode = addon->GetTextNodeById((uint)ItemDetailTextNode.Name);
        if (baseTextNode == null) return;

        if (textNode == null)
        {
            textNode = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
            if (textNode == null) return;
            textNode->AtkResNode.Type = NodeType.Text;
            textNode->AtkResNode.NodeId = (int)ItemDetailTextNode.NameTranslated;


            textNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
            textNode->AtkResNode.DrawFlags = 0;

            textNode->AtkResNode.Color.A = baseTextNode->AtkResNode.Color.A;
            textNode->AtkResNode.Color.R = baseTextNode->AtkResNode.Color.R;
            textNode->AtkResNode.Color.G = baseTextNode->AtkResNode.Color.G;
            textNode->AtkResNode.Color.B = baseTextNode->AtkResNode.Color.B;

            textNode->TextColor.A = baseTextNode->TextColor.A;
            textNode->TextColor.R = baseTextNode->TextColor.R;
            textNode->TextColor.G = baseTextNode->TextColor.G;
            textNode->TextColor.B = baseTextNode->TextColor.B;

            textNode->EdgeColor.A = baseTextNode->EdgeColor.A;
            textNode->EdgeColor.R = baseTextNode->EdgeColor.R;
            textNode->EdgeColor.G = baseTextNode->EdgeColor.G;
            textNode->EdgeColor.B = baseTextNode->EdgeColor.B;

            textNode->LineSpacing = 18;
            textNode->AlignmentFontType = 0x00;
            textNode->FontSize = 12;
            textNode->TextFlags = (byte)((TextFlags)baseTextNode->TextFlags | TextFlags.MultiLine | TextFlags.AutoAdjustNodeSize);
            textNode->TextFlags2 = 0;

            var prev = insertNode->PrevSiblingNode;
            textNode->AtkResNode.ParentNode = insertNode->ParentNode;

            insertNode->PrevSiblingNode = (AtkResNode*)textNode;

            if (prev != null) prev->NextSiblingNode = (AtkResNode*)textNode;

            textNode->AtkResNode.PrevSiblingNode = prev;
            textNode->AtkResNode.NextSiblingNode = insertNode;

            addon->UldManager.UpdateDrawNodeList();
        }

        textNode->AtkResNode.ToggleVisibility(true);

        var lines = new SeString();
        lines.Payloads.Add(new UIForegroundPayload(plugin.Config.ItemNameColourKey));
        lines.Payloads.Add(new TextPayload($"{itemNameTranslation}"));
        lines.Payloads.Add(new UIForegroundPayload(0));
        textNode->SetText(lines.Encode());

        textNode->ResizeNodeForCurrentText();
        textNode->SetWidth(300);
        textNode->SetHeight(21);
        var itemNameNode = addon->GetTextNodeById((uint)ItemDetailTextNode.Name);
        var textNodeOffset = plugin.Config.OffsetItemNameTranslation;
        textNode->AtkResNode.SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + textNodeOffset);
        var itemNameOffset = plugin.Config.OffsetItemNameOriginal;
        itemNameNode->SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + itemNameOffset);
    }


    private unsafe void AddItemDescriptionTranslation(AtkUnitBase* addon, StringArrayData* stringArrayData)
    {
        var itemDescriptionNode = addon->GetTextNodeById((uint)ItemDetailTextNode.Description);
        if (itemDescriptionNode == null) return;

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var addr = new nint(stringArrayData->StringArray[13]);
        var currentText = MemoryHelper.ReadSeStringNullTerminated(addr);

        if (currentText.Payloads.Count >= 1 && currentText.Payloads[0] is UIForegroundPayload foregroundPayload && foregroundPayload.ColorKey == plugin.Config.ItemDescriptionColourKey)
        {
            return;
        }
        currentText.Payloads.Insert(0, new UIForegroundPayload(plugin.Config.ItemDescriptionColourKey));
        currentText.Payloads.Insert(1, new TextPayload($"{itemDescriptionTranslation}\n\n"));
        currentText.Payloads.Insert(2, new UIForegroundPayload(0));

        stringArrayData->SetValue(13, currentText.Encode(), false, true, true);
    }

}
