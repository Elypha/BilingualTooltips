using BilingualTooltips.Modules;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
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
        ResetItemNameTextNode(dispose: true);
    }


    private string itemNameTranslation = "";
    private string itemDescTranslation = "";
    private bool UpdateTranslations()
    {
        // get id
        var itemId = Service.GameGui.HoveredItem;
        if (itemId == 0) return false;

        try
        {
            // update translations
            itemNameTranslation = plugin.Config.LanguageItemTooltipName != GameLanguage.Off ?
                SheetHelper.GetItemName(itemId, plugin.Config.LanguageItemTooltipName) :
                "";
            itemDescTranslation = plugin.Config.LanguageItemTooltipDescription != GameLanguage.Off ?
                SheetHelper.GetItemDescription(itemId, plugin.Config.LanguageItemTooltipDescription) :
                "";

            // update translations on the panel
            P.ItemTooltipPanel.NameJa = SheetHelper.GetItemName(itemId, GameLanguage.Japanese)!;
            P.ItemTooltipPanel.NameEn = SheetHelper.GetItemName(itemId, GameLanguage.English)!;
            P.ItemTooltipPanel.NameDe = SheetHelper.GetItemName(itemId, GameLanguage.German)!;
            P.ItemTooltipPanel.NameFr = SheetHelper.GetItemName(itemId, GameLanguage.French)!;
            P.ItemTooltipPanel.DescJa = SheetHelper.GetItemDescription(itemId, GameLanguage.Japanese)!;
            P.ItemTooltipPanel.DescEn = SheetHelper.GetItemDescription(itemId, GameLanguage.English)!;
            P.ItemTooltipPanel.DescDe = SheetHelper.GetItemDescription(itemId, GameLanguage.German)!;
            P.ItemTooltipPanel.DescFr = SheetHelper.GetItemDescription(itemId, GameLanguage.French)!;
        }
        catch (NotImplementedException)
        {
            // Service.Log.Verbose($"ItemDetailAddon: Item not implemented, ID={itemId}");
            return false;
        }

        return true;
    }

    public unsafe void ResetItemNameTextNode(bool dispose = false)
    {
        AddonHelper.ResetTooltipNameTextNode(
            AddonHelper.ItemDetail.Name,
            AddonHelper.ItemDetail.TextNodeId.Name,
            AddonHelper.ItemDetail.TextNodeId.NameTranslation,
            dispose
        );
    }


    public unsafe void PreRequestedUpdateHandler(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRequestedUpdateArgs requestedUpdateArgs) return;
        if (!plugin.Config.Enabled) return;

        var addonPtr = (AtkUnitBase*)args.Addon.Address;
        if (addonPtr == null || !addonPtr->IsVisible) return;

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
                AddItemDescriptionTranslation(addonPtr, stringArrayData);
            }

            if ((plugin.Config.LanguageItemTooltipName != GameLanguage.Off) && !string.IsNullOrEmpty(itemNameTranslation))
            {
                AddItemNameTranslation(addonPtr);
            }
        }
    }


    private unsafe void AddItemNameTranslation(AtkUnitBase* addon)
    {
        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var itemNameNode = addon->GetTextNodeById(AddonHelper.ItemDetail.TextNodeId.Name);
        if (itemNameNode == null) return;

        var itemNameTranslationNode = AddonHelper.GetTextNodeById(addon, AddonHelper.ItemDetail.TextNodeId.NameTranslation);
        if (itemNameTranslationNode == null)
        {
            itemNameTranslationNode = AddonHelper.SetupTextNodeTooltip(addon, itemNameNode, insertNode, AddonHelper.ItemDetail.TextNodeId.NameTranslation);
        }
        itemNameTranslationNode->AtkResNode.ToggleVisibility(true);

        itemNameTranslationNode->SetWidth(300);
        itemNameTranslationNode->SetHeight(21);

        var lines = new SeString();
        lines.Payloads.Add(new UIForegroundPayload((ushort)plugin.Config.ItemNameColourKey));
        lines.Payloads.Add(new TextPayload($"{itemNameTranslation}"));
        lines.Payloads.Add(new UIForegroundPayload(0));
        itemNameTranslationNode->SetText(lines.Encode());
        itemNameTranslationNode->ResizeNodeForCurrentText();

        // the order needs to be kept as their 'original' positions are read dynamically
        itemNameTranslationNode->AtkResNode.SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + plugin.Config.OffsetItemNameTranslation);
        itemNameNode->SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + plugin.Config.OffsetItemNameOriginal);
    }


    private unsafe void AddItemDescriptionTranslation(AtkUnitBase* addon, StringArrayData* stringArrayData)
    {
        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var itemDescriptionNode = addon->GetTextNodeById(AddonHelper.ItemDetail.TextNodeId.Description);
        if (itemDescriptionNode == null) return;

        var addr = new nint(stringArrayData->StringArray[AddonHelper.ItemDetail.StringArrayIndex.Description]);
        var currentText = MemoryHelper.ReadSeStringNullTerminated(addr);

        if (currentText.Payloads.Count >= 3
            && currentText.Payloads[1] is TextPayload textPayload
            && textPayload.Text?.StartsWith(itemDescTranslation) == true)
            return;

        currentText.Payloads.Insert(0, new UIForegroundPayload((ushort)plugin.Config.ItemDescriptionColourKey));
        currentText.Payloads.Insert(1, new TextPayload($"{itemDescTranslation}\n\n"));
        currentText.Payloads.Insert(2, new UIForegroundPayload(0));
        stringArrayData->SetValue(AddonHelper.ItemDetail.StringArrayIndex.Description, currentText.Encode(), false, true, true);
    }

}
