using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BilingualTooltips.Modules;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using Miosuke;
using Miosuke.Action;


namespace BilingualTooltips.Addons;

public class ItemDetailAddon
{
    private BilingualTooltipsPlugin plugin;

    public ItemDetailAddon(BilingualTooltipsPlugin plugin)
    {
        this.plugin = plugin;
    }

    public void Dispose()
    {
        ResetItemNameTextNode();
    }


    private string itemNameTranslation = "";
    private string itemDescTranslation = "";
    private bool UpdateTranslations()
    {
        // get id
        var itemId = Service.GameGui.HoveredItem;
        if (itemId == 0) return false;

        // update translations
        itemNameTranslation = SheetHelper.GetItemName(itemId, plugin.Config.LanguageItemTooltipName) ?? "";
        itemDescTranslation = SheetHelper.GetItemDescription(itemId, plugin.Config.LanguageItemTooltipDescription) ?? "";

        // update translations on the panel
        P.ItemTooltipPanel.NameJa = SheetHelper.GetItemName(itemId, GameLanguage.Japanese)!;
        P.ItemTooltipPanel.NameEn = SheetHelper.GetItemName(itemId, GameLanguage.English)!;
        P.ItemTooltipPanel.NameDe = SheetHelper.GetItemName(itemId, GameLanguage.German)!;
        P.ItemTooltipPanel.NameFr = SheetHelper.GetItemName(itemId, GameLanguage.French)!;
        P.ItemTooltipPanel.DescJa = SheetHelper.GetItemDescription(itemId, GameLanguage.Japanese)!;
        P.ItemTooltipPanel.DescEn = SheetHelper.GetItemDescription(itemId, GameLanguage.English)!;
        P.ItemTooltipPanel.DescDe = SheetHelper.GetItemDescription(itemId, GameLanguage.German)!;
        P.ItemTooltipPanel.DescFr = SheetHelper.GetItemDescription(itemId, GameLanguage.French)!;

        return true;
    }


    public unsafe void ResetItemNameTextNode()
    {
        var addon = Service.GameGui.GetAddonByName(AddonHelper.ItemDetail.Name);
        var addonPtr = (AtkUnitBase*)addon.Address;

        // remove translation if it exists
        var nameTranslationNode = AddonHelper.GetNodeByNodeId(addonPtr, AddonHelper.ItemDetail.TextNodeId.NameTranslation);
        if (nameTranslationNode != null)
        {
            if (nameTranslationNode->AtkResNode.IsVisible())
            {
                var insertNode = addonPtr->GetNodeById(2);
                if (insertNode == null) return;
                nameTranslationNode->AtkResNode.ToggleVisibility(false);
            }
        }

        // reset original name position
        var name_node = addonPtr->GetTextNodeById(AddonHelper.ItemDetail.TextNodeId.Name);
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
        if (plugin.Config.LanguageItemTooltipName != GameLanguage.Off) ResetItemNameTextNode();

        // skip if enable on hotkey only
        if (plugin.Config.TemporaryEnableOnly && !Hotkey.IsActive(plugin.Config.TemporaryEnableHotkey)) return;

        var numberArrayData = ((NumberArrayData**)requestedUpdateArgs.NumberArrayData)[AddonHelper.ItemDetail.NumberArrayIndex.Self];
        var stringArrayData = ((StringArrayData**)requestedUpdateArgs.StringArrayData)[AddonHelper.ItemDetail.StringArrayIndex.Self];
        if (UpdateTranslations())
        {
            if ((plugin.Config.LanguageItemTooltipDescription != GameLanguage.Off) && !string.IsNullOrEmpty(itemDescTranslation))
            {
                AddItemDescriptionTranslation(addon, stringArrayData);
            }

            if ((plugin.Config.LanguageItemTooltipName != GameLanguage.Off) && !string.IsNullOrEmpty(itemNameTranslation))
            {
                AddItemNameTranslation(addon);
            }
        }
    }


    private unsafe void AddItemNameTranslation(AtkUnitBase* addon)
    {
        var baseTextNode = addon->GetTextNodeById(AddonHelper.ItemDetail.TextNodeId.Name);
        if (baseTextNode == null) return;

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var textNode = AddonHelper.GetNodeByNodeId(addon, AddonHelper.ItemDetail.TextNodeId.NameTranslation);
        if (textNode == null)
        {
            AddonHelper.SetupTextNodeTooltip(addon, baseTextNode, insertNode, AddonHelper.ItemDetail.TextNodeId.NameTranslation);
        }
        textNode->AtkResNode.ToggleVisibility(true);

        var lines = new SeString();
        lines.Payloads.Add(new UIForegroundPayload((ushort)plugin.Config.ItemNameColourKey));
        lines.Payloads.Add(new TextPayload($"{itemNameTranslation}"));
        lines.Payloads.Add(new UIForegroundPayload(0));
        textNode->SetText(lines.Encode());

        textNode->ResizeNodeForCurrentText();
        textNode->SetWidth(300);
        textNode->SetHeight(21);

        var itemNameNode = addon->GetTextNodeById(AddonHelper.ItemDetail.TextNodeId.Name);

        var textNodeOffset = plugin.Config.OffsetItemNameTranslation;
        textNode->AtkResNode.SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + textNodeOffset);

        var itemNameOffset = plugin.Config.OffsetItemNameOriginal;
        itemNameNode->SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + itemNameOffset);
    }


    private unsafe void AddItemDescriptionTranslation(AtkUnitBase* addon, StringArrayData* stringArrayData)
    {
        var itemDescriptionNode = addon->GetTextNodeById(AddonHelper.ItemDetail.TextNodeId.Description);
        if (itemDescriptionNode == null) return;

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var addr = new nint(stringArrayData->StringArray[AddonHelper.ItemDetail.StringArrayIndex.Description]);
        var currentText = MemoryHelper.ReadSeStringNullTerminated(addr);
        var currentText2 = new ReadOnlySeStringSpan(stringArrayData->StringArray[AddonHelper.ItemDetail.StringArrayIndex.Description].Value);

        if (currentText.Payloads.Count >= 1 && currentText.Payloads[0] is UIForegroundPayload foregroundPayload && foregroundPayload.ColorKey == plugin.Config.ItemDescriptionColourKey)
        {
            return;
        }
        currentText.Payloads.Insert(0, new UIForegroundPayload((ushort)plugin.Config.ItemDescriptionColourKey));
        currentText.Payloads.Insert(1, new TextPayload($"{itemDescTranslation}\n\n"));
        currentText.Payloads.Insert(2, new UIForegroundPayload(0));

        stringArrayData->SetValue(AddonHelper.ItemDetail.StringArrayIndex.Description, currentText.Encode(), false, true, true);
    }

}
