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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace BilingualTooltips;

public class TooltipHandler
{
    private Plugin plugin;

    public TooltipHandler(Plugin plugin)
    {
        this.plugin = plugin;
    }

    public void Dispose()
    {
    }

    public ExcelSheet<Item> SheetItemJp = Service.Data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.Japanese)!;
    public ExcelSheet<Item> SheetItemEn = Service.Data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.English)!;
    public ExcelSheet<Item> SheetItemDe = Service.Data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.German)!;
    public ExcelSheet<Item> SheetItemFr = Service.Data.GetExcelSheet<Item>(Dalamud.Game.ClientLanguage.French)!;

    public const int ItemNameNodeId = 1270;
    public const int ItemDescriptionNodeId = 41;
    public string itemNameTranslation = "";
    public string itemDescriptionTranslation = "";


    public void StartHook()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "ItemDetail", ItemDetail_PreRequestedUpdate_Handler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "ItemDetail", ItemDetail_PostRequestedUpdate_Handler);

        // Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "ActionDetail", ActionDetailOnRequestedUpdate);
    }


    public void StopHook()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "ItemDetail", ItemDetail_PreRequestedUpdate_Handler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "ItemDetail", ItemDetail_PostRequestedUpdate_Handler);
        // Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "ActionDetail", ActionDetailOnRequestedUpdate);

        RemoveItemNameTranslationOnUnload(ItemNameNodeId);
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


    private void UpdateItemTooltipData()
    {
        // get item id
        var itemId = Service.GameGui.HoveredItem;
        if (itemId < 2000000)
        {
            itemId %= 500000;
        }
        switch (plugin.Config.TooltipLanguage)
        {
            case GameLanguage.Japanese:
                itemNameTranslation = SheetItemJp.GetRow((uint)itemId)?.Name ?? "Not found";
                itemDescriptionTranslation = SheetItemJp.GetRow((uint)itemId)?.Description ?? "Not found";
                break;
            case GameLanguage.English:
                itemNameTranslation = SheetItemEn.GetRow((uint)itemId)?.Name ?? "Not found";
                itemDescriptionTranslation = SheetItemEn.GetRow((uint)itemId)?.Description ?? "Not found";
                break;
            case GameLanguage.German:
                itemNameTranslation = SheetItemDe.GetRow((uint)itemId)?.Name ?? "Not found";
                itemDescriptionTranslation = SheetItemDe.GetRow((uint)itemId)?.Description ?? "Not found";
                break;
            case GameLanguage.French:
                itemNameTranslation = SheetItemFr.GetRow((uint)itemId)?.Name ?? "Not found";
                itemDescriptionTranslation = SheetItemFr.GetRow((uint)itemId)?.Description ?? "Not found";
                break;
        }
    }


    private unsafe void ItemDetail_PreRequestedUpdate_Handler(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRequestedUpdateArgs requestedUpdateArgs) return;

        var addon = (AtkUnitBase*)args.Addon;
        if (!addon->IsVisible) return;

        var numberArrayData = ((NumberArrayData**)requestedUpdateArgs.NumberArrayData)[29];
        var stringArrayData = ((StringArrayData**)requestedUpdateArgs.StringArrayData)[26];

        UpdateItemTooltipData();
        RemoveItemNameTranslation(addon, ItemNameNodeId);

        var name_node = addon->GetTextNodeById(32);
        float x, y;
        name_node->GetPositionFloat(&x, &y);
        name_node->SetPositionFloat(x, 20);
        var pos_y = name_node->AtkResNode.Y;

        AddItemDescriptionTranslation(addon, stringArrayData);
    }


    private unsafe void ItemDetail_PostRequestedUpdate_Handler(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRequestedUpdateArgs requestedUpdateArgs) return;

        var addon = (AtkUnitBase*)args.Addon;
        if (!addon->IsVisible) return;
        if (Service.GameGui.HoveredItem > uint.MaxValue) return;

        var numberArrayData = ((NumberArrayData**)requestedUpdateArgs.NumberArrayData)[29];
        var stringArrayData = ((StringArrayData**)requestedUpdateArgs.StringArrayData)[26];

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
        var addr = new nint(stringArrayData->StringArray[0]);
        var seString = MemoryHelper.ReadSeStringNullTerminated(addr);

        AddItemNameTranslation(addon);
    }


    private unsafe void AddItemNameTranslation(AtkUnitBase* addon)
    {
        var textNode = GetNodeByNodeId(addon, ItemNameNodeId);
        if (textNode != null) textNode->AtkResNode.ToggleVisibility(false);

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var baseTextNode = addon->GetTextNodeById(43);
        if (baseTextNode == null) return;

        if (textNode == null)
        {
            textNode = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
            if (textNode == null) return;
            textNode->AtkResNode.Type = NodeType.Text;
            textNode->AtkResNode.NodeId = ItemNameNodeId;


            textNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
            textNode->AtkResNode.DrawFlags = 0;
            textNode->AtkResNode.SetWidth(50);
            textNode->AtkResNode.SetHeight(20);

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
        var itemNameNode = addon->GetTextNodeById(32);
        textNode->AtkResNode.SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y - 2.5f);
    }


    private unsafe void RemoveItemNameTranslation(AtkUnitBase* addon, int nodeId)
    {
        var customNode = GetNodeByNodeId(addon, nodeId);

        if (customNode != null)
        {
            if (customNode->AtkResNode.IsVisible())
            {
                var insertNode = addon->GetNodeById(2);
                if (insertNode == null) return;
                customNode->AtkResNode.ToggleVisibility(false);
            }

        }
    }

    private unsafe void RemoveItemNameTranslationOnUnload(int nodeId)
    {
        var addon = Service.GameGui.GetAddonByName("ItemDetail");
        var addonPtr = (AtkUnitBase*)addon;
        RemoveItemNameTranslation(addonPtr, nodeId);

        var name_node = addonPtr->GetTextNodeById(32);
        float x, y;
        name_node->GetPositionFloat(&x, &y);
        name_node->SetPositionFloat(x, 14);
    }


    private unsafe void AddItemDescriptionTranslation(AtkUnitBase* addon, StringArrayData* stringArrayData)
    {
        var itemDescriptionNode = addon->GetTextNodeById(ItemDescriptionNodeId);
        if (itemDescriptionNode == null) return;

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var addr = new nint(stringArrayData->StringArray[13]);
        var currentText = MemoryHelper.ReadSeStringNullTerminated(addr);

        if (currentText.Payloads.Count >= 1 && currentText.Payloads[0] is UIForegroundPayload foregroundPayload && foregroundPayload.ColorKey == 3)
        {
            return;
        }
        currentText.Payloads.Insert(0, new UIForegroundPayload(3));
        currentText.Payloads.Insert(1, new TextPayload($"{itemDescriptionTranslation}\n\n"));
        currentText.Payloads.Insert(2, new UIForegroundPayload(0));

        stringArrayData->SetValue(13, currentText.Encode(), false, true, true);
    }
}
