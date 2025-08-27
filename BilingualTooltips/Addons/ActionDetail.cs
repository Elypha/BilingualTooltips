using BilingualTooltips.Modules;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Miosuke.Action;


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
        ResetActionNameTextNode(dispose: true);
    }


    private string actionNameTranslation = "";
    private string actionDescTranslation = "";
    private bool UpdateTranslations()
    {
        // get id
        var action = Service.GameGui.HoveredAction;
        if (action.ActionID == 0) return false;

        // update translations
        actionNameTranslation = plugin.Config.LanguageActionTooltipName != GameLanguage.Off ?
            SheetHelper.GetActionName(action, plugin.Config.LanguageActionTooltipName) :
            "";
        actionDescTranslation = plugin.Config.LanguageActionTooltipDescription != GameLanguage.Off ?
            SheetHelper.GetActionDescription(action, plugin.Config.LanguageActionTooltipDescription) :
            "";
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

    public unsafe void ResetActionNameTextNode(bool dispose = false)
    {
        AddonHelper.ResetTooltipNameTextNode(
            AddonHelper.ActionDetail.Name,
            AddonHelper.ActionDetail.TextNodeId.Name,
            AddonHelper.ActionDetail.TextNodeId.NameTranslation,
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
        if (plugin.Config.LanguageActionTooltipName != GameLanguage.Off) ResetActionNameTextNode();

        // skip if enable on hotkey only
        if (plugin.Config.TemporaryEnableOnly && !Hotkey.IsActive(plugin.Config.TemporaryEnableHotkey)) return;

        var numberArrayData = ((NumberArrayData**)requestedUpdateArgs.NumberArrayData)[AddonHelper.ActionDetail.NumberArrayIndex.Self];
        var stringArrayData = ((StringArrayData**)requestedUpdateArgs.StringArrayData)[AddonHelper.ActionDetail.StringArrayIndex.Self];
        if (UpdateTranslations())
        {
            if ((plugin.Config.LanguageActionTooltipDescription != GameLanguage.Off) && !string.IsNullOrEmpty(actionDescTranslation))
            {
                AddActionDescriptionTranslation(addonPtr, stringArrayData);
            }

            if ((plugin.Config.LanguageActionTooltipName != GameLanguage.Off) && !string.IsNullOrEmpty(actionNameTranslation))
            {
                AddActionNameTranslation(addonPtr);
            }
        }
    }


    private unsafe void AddActionNameTranslation(AtkUnitBase* addon)
    {
        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var actionNameNode = addon->GetTextNodeById(AddonHelper.ActionDetail.TextNodeId.Name);
        if (actionNameNode == null) return;

        var actionNameTranslationNode = AddonHelper.GetTextNodeById(addon, AddonHelper.ActionDetail.TextNodeId.NameTranslation);
        if (actionNameTranslationNode == null)
        {
            actionNameTranslationNode = AddonHelper.SetupTextNodeTooltip(addon, actionNameNode, insertNode, AddonHelper.ActionDetail.TextNodeId.NameTranslation);
        }
        actionNameTranslationNode->AtkResNode.ToggleVisibility(true);

        actionNameTranslationNode->SetWidth(300);
        actionNameTranslationNode->SetHeight(21);

        var lines = new SeString();
        lines.Payloads.Add(new UIForegroundPayload((ushort)plugin.Config.ActionNameColourKey));
        lines.Payloads.Add(new TextPayload($"{actionNameTranslation}"));
        lines.Payloads.Add(new UIForegroundPayload(0));
        actionNameTranslationNode->SetText(lines.Encode());
        actionNameTranslationNode->ResizeNodeForCurrentText();

        // the order needs to be kept as their 'original' positions are read dynamically
        actionNameTranslationNode->AtkResNode.SetPositionFloat(actionNameNode->AtkResNode.X, actionNameNode->AtkResNode.Y + plugin.Config.OffsetActionNameTranslation);
        actionNameNode->SetPositionFloat(actionNameNode->AtkResNode.X, actionNameNode->AtkResNode.Y + plugin.Config.OffsetActionNameOriginal);
    }


    private unsafe void AddActionDescriptionTranslation(AtkUnitBase* addon, StringArrayData* stringArrayData)
    {
        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var descriptionNode = addon->GetTextNodeById(AddonHelper.ActionDetail.TextNodeId.Description);
        if (descriptionNode == null) return;

        var addr = new nint(stringArrayData->StringArray[AddonHelper.ActionDetail.StringArrayIndex.Description]);
        var currentText = MemoryHelper.ReadSeStringNullTerminated(addr);

        if (currentText.Payloads.Count >= 3
            && currentText.Payloads[1] is TextPayload textPayload
            && textPayload.Text?.StartsWith(actionDescTranslation) == true)
            return;

        currentText.Payloads.Insert(0, new UIForegroundPayload((ushort)plugin.Config.ActionDescriptionColourKey));
        currentText.Payloads.Insert(1, new TextPayload($"{actionDescTranslation}\n\n"));
        currentText.Payloads.Insert(2, new UIForegroundPayload(0));
        stringArrayData->SetValue(AddonHelper.ActionDetail.StringArrayIndex.Description, currentText.Encode(), false, true, true);
    }
}
