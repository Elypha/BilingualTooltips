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
using Miosuke.Action;
using Miosuke;
using Dalamud.Game.Gui;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace BilingualTooltips;

public partial class TooltipHandler
{
    public ExcelSheet<Action> SheetActionJp = Service.Data.GetExcelSheet<Action>(Dalamud.Game.ClientLanguage.Japanese)!;
    public ExcelSheet<Action> SheetActionEn = Service.Data.GetExcelSheet<Action>(Dalamud.Game.ClientLanguage.English)!;
    public ExcelSheet<Action> SheetActionDe = Service.Data.GetExcelSheet<Action>(Dalamud.Game.ClientLanguage.German)!;
    public ExcelSheet<Action> SheetActionFr = Service.Data.GetExcelSheet<Action>(Dalamud.Game.ClientLanguage.French)!;
    public ExcelSheet<ActionTransient> SheetActionTransientJp = Service.Data.GetExcelSheet<ActionTransient>(Dalamud.Game.ClientLanguage.Japanese)!;
    public ExcelSheet<ActionTransient> SheetActionTransientEn = Service.Data.GetExcelSheet<ActionTransient>(Dalamud.Game.ClientLanguage.English)!;
    public ExcelSheet<ActionTransient> SheetActionTransientDe = Service.Data.GetExcelSheet<ActionTransient>(Dalamud.Game.ClientLanguage.German)!;
    public ExcelSheet<ActionTransient> SheetActionTransientFr = Service.Data.GetExcelSheet<ActionTransient>(Dalamud.Game.ClientLanguage.French)!;
    public ExcelSheet<Trait> SheetTraitJp = Service.Data.GetExcelSheet<Trait>(Dalamud.Game.ClientLanguage.Japanese)!;
    public ExcelSheet<Trait> SheetTraitEn = Service.Data.GetExcelSheet<Trait>(Dalamud.Game.ClientLanguage.English)!;
    public ExcelSheet<Trait> SheetTraitDe = Service.Data.GetExcelSheet<Trait>(Dalamud.Game.ClientLanguage.German)!;
    public ExcelSheet<Trait> SheetTraitFr = Service.Data.GetExcelSheet<Trait>(Dalamud.Game.ClientLanguage.French)!;
    public ExcelSheet<TraitTransient> SheetTraitTransientJp = Service.Data.GetExcelSheet<TraitTransient>(Dalamud.Game.ClientLanguage.Japanese)!;
    public ExcelSheet<TraitTransient> SheetTraitTransientEn = Service.Data.GetExcelSheet<TraitTransient>(Dalamud.Game.ClientLanguage.English)!;
    public ExcelSheet<TraitTransient> SheetTraitTransientDe = Service.Data.GetExcelSheet<TraitTransient>(Dalamud.Game.ClientLanguage.German)!;
    public ExcelSheet<TraitTransient> SheetTraitTransientFr = Service.Data.GetExcelSheet<TraitTransient>(Dalamud.Game.ClientLanguage.French)!;
    public ExcelSheet<GeneralAction> SheetGeneralActionJp = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.Japanese)!;
    public ExcelSheet<GeneralAction> SheetGeneralActionEn = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.English)!;
    public ExcelSheet<GeneralAction> SheetGeneralActionDe = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.German)!;
    public ExcelSheet<GeneralAction> SheetGeneralActionFr = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.French)!;

    public ExcelSheet<Action> SheetAction;

    public const int NewActionNameNodeId = 1270;
    public const int ActionDescriptionNodeId = 19;
    public string actionNameTranslation = "";
    public string actionDescriptionTranslation = "";


    private void UpdateActionTooltipData()
    {
        var hover = Service.GameGui.HoveredAction;
        if (hover.ActionID == 0) return;

        var mnemonic = Service.ClientState.LocalPlayer?.ClassJob.GameData?.Abbreviation.ToString();
        // var actionName = "";
        // var actionDescription = "";

        if (hover.ActionKind == HoverActionKind.Action)
        {
            switch (plugin.Config.LanguageActionTooltipName)
            {
                case GameLanguage.Japanese:
                    actionNameTranslation = SheetAction.GetRow(hover.ActionID)?.Name ?? "null";
                    break;
                case GameLanguage.English:
                    actionNameTranslation = SheetAction.GetRow(hover.ActionID)?.Name ?? "null";
                    break;
                case GameLanguage.German:
                    actionNameTranslation = SheetAction.GetRow(hover.ActionID)?.Name ?? "null";
                    break;
                case GameLanguage.French:
                    actionNameTranslation = SheetAction.GetRow(hover.ActionID)?.Name ?? "null";
                    break;
            }
            switch (plugin.Config.LanguageActionTooltipDescription)
            {
                case GameLanguage.Japanese:
                    actionDescriptionTranslation = SheetActionTransientJp.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
                case GameLanguage.English:
                    actionDescriptionTranslation = SheetActionTransientEn.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
                case GameLanguage.German:
                    actionDescriptionTranslation = SheetActionTransientDe.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
                case GameLanguage.French:
                    actionDescriptionTranslation = SheetActionTransientFr.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
            }
        }
        else if (hover.ActionKind == HoverActionKind.GeneralAction)
        {
            switch (plugin.Config.LanguageActionTooltipName)
            {
                case GameLanguage.Japanese:
                    actionNameTranslation = SheetGeneralActionJp.GetRow(hover.ActionID)?.Name ?? "Not found";
                    break;
                case GameLanguage.English:
                    actionNameTranslation = SheetGeneralActionEn.GetRow(hover.ActionID)?.Name ?? "Not found";
                    break;
                case GameLanguage.German:
                    actionNameTranslation = SheetGeneralActionDe.GetRow(hover.ActionID)?.Name ?? "Not found";
                    break;
                case GameLanguage.French:
                    actionNameTranslation = SheetGeneralActionFr.GetRow(hover.ActionID)?.Name ?? "Not found";
                    break;
            }
            switch (plugin.Config.LanguageActionTooltipDescription)
            {
                case GameLanguage.Japanese:
                    actionDescriptionTranslation = SheetGeneralActionJp.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
                case GameLanguage.English:
                    actionDescriptionTranslation = SheetGeneralActionEn.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
                case GameLanguage.German:
                    actionDescriptionTranslation = SheetGeneralActionDe.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
                case GameLanguage.French:
                    actionDescriptionTranslation = SheetGeneralActionFr.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
            }
        }
        else if (hover.ActionKind == HoverActionKind.Trait)
        {
            switch (plugin.Config.LanguageActionTooltipName)
            {
                case GameLanguage.Japanese:
                    actionNameTranslation = SheetTraitJp.GetRow(hover.ActionID)?.Name ?? "Not found";
                    break;
                case GameLanguage.English:
                    actionNameTranslation = SheetTraitEn.GetRow(hover.ActionID)?.Name ?? "Not found";
                    break;
                case GameLanguage.German:
                    actionNameTranslation = SheetTraitDe.GetRow(hover.ActionID)?.Name ?? "Not found";
                    break;
                case GameLanguage.French:
                    actionNameTranslation = SheetTraitFr.GetRow(hover.ActionID)?.Name ?? "Not found";
                    break;
            }
            switch (plugin.Config.LanguageActionTooltipDescription)
            {
                case GameLanguage.Japanese:
                    actionDescriptionTranslation = SheetTraitTransientJp.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
                case GameLanguage.English:
                    actionDescriptionTranslation = SheetTraitTransientEn.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
                case GameLanguage.German:
                    actionDescriptionTranslation = SheetTraitTransientDe.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
                case GameLanguage.French:
                    actionDescriptionTranslation = SheetTraitTransientFr.GetRow(hover.ActionID)?.Description ?? "Not found";
                    break;
            }
        }
        else if ((uint)hover.ActionKind == 34 || (uint)hover.ActionKind == 39)
        {
            // 34 for minions, 39 for mounts
            actionNameTranslation = "";
            actionDescriptionTranslation = "";
        }
        else
        {
            Service.Log.Debug($"Unsupported action kind for {hover.ActionID} [{hover.ActionKind}]");
        }


    }

    public unsafe void ResetActionTooltip()
    {
        var addon = Service.GameGui.GetAddonByName("ActionDetail");
        var addonPtr = (AtkUnitBase*)addon;

        // remove translation if it exists
        var customNode = GetNodeByNodeId(addonPtr, NewActionNameNodeId);
        if (customNode != null)
        {
            if (customNode->AtkResNode.IsVisible())
            {
                var insertNode = addonPtr->GetNodeById(2);
                if (insertNode == null) return;
                customNode->AtkResNode.ToggleVisibility(false);
            }
        }

        // reset original item name position
        var name_node = addonPtr->GetTextNodeById(5);
        float x, y;
        name_node->GetPositionFloat(&x, &y);
        name_node->SetPositionFloat(x, 14);
    }

    private unsafe void ActionDetail_PreRequestedUpdate_Handler(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRequestedUpdateArgs requestedUpdateArgs) return;
        if (!plugin.Config.Enabled) return;

        var addon = (AtkUnitBase*)args.Addon;
        if (!addon->IsVisible) return;

        if (plugin.Config.LanguageActionTooltipName != GameLanguage.Off) ResetActionTooltip();
        if (plugin.Config.TemporaryEnableOnly && !Hotkey.IsActive(plugin.Config.TemporaryEnableHotkey)) return;

        var numberArrayData = ((NumberArrayData**)requestedUpdateArgs.NumberArrayData)[31];
        var stringArrayData = ((StringArrayData**)requestedUpdateArgs.StringArrayData)[28];
        UpdateActionTooltipData();

        if (plugin.Config.LanguageActionTooltipDescription != GameLanguage.Off)
        {
            AddActionDescriptionTranslation(addon, stringArrayData);
        }

        // dump
        // for (var i = 0; i < stringArrayData->Size; i++)
        // {
        //     var addr = new nint(stringArrayData->StringArray[i]);
        //     var seString = MemoryHelper.ReadSeStringNullTerminated(addr);
        //     Service.Log.Info($"{addon->NameString} str{i}: {seString.ToJson()}");
        // }
        // LUT
        // 0: action name
        // 13: action description

        if (plugin.Config.LanguageActionTooltipName == GameLanguage.Off) return;

        AddActionNameTranslation(addon);
    }


    private unsafe void AddActionNameTranslation(AtkUnitBase* addon)
    {
        var textNode = GetNodeByNodeId(addon, NewActionNameNodeId);
        if (textNode != null) textNode->AtkResNode.ToggleVisibility(false);

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var baseTextNode = addon->GetTextNodeById(5);
        if (baseTextNode == null) return;

        if (textNode == null)
        {
            textNode = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
            if (textNode == null) return;
            textNode->AtkResNode.Type = NodeType.Text;
            textNode->AtkResNode.NodeId = NewActionNameNodeId;


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
        lines.Payloads.Add(new UIForegroundPayload(plugin.Config.ActionNameColourKey));
        lines.Payloads.Add(new TextPayload($"{actionNameTranslation}"));
        lines.Payloads.Add(new UIForegroundPayload(0));
        textNode->SetText(lines.Encode());

        textNode->ResizeNodeForCurrentText();
        textNode->SetWidth(200);
        textNode->SetHeight(21);
        var itemNameNode = addon->GetTextNodeById(5);
        var textNodeOffset = plugin.Config.OffsetActionNameTranslation;
        textNode->AtkResNode.SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + textNodeOffset);

        var itemNameOffset = plugin.Config.OffsetActionNameOriginal;
        itemNameNode->SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + itemNameOffset);

    }


    private unsafe void AddActionDescriptionTranslation(AtkUnitBase* addon, StringArrayData* stringArrayData)
    {
        var descriptionNode = addon->GetTextNodeById(ActionDescriptionNodeId);
        if (descriptionNode == null) return;

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var addr = new nint(stringArrayData->StringArray[13]);
        var currentText = MemoryHelper.ReadSeStringNullTerminated(addr);

        if (currentText.Payloads.Count >= 1 && currentText.Payloads[0] is UIForegroundPayload foregroundPayload && foregroundPayload.ColorKey == plugin.Config.ActionDescriptionColourKey)
        {
            return;
        }
        currentText.Payloads.Insert(0, new UIForegroundPayload(plugin.Config.ActionDescriptionColourKey));
        currentText.Payloads.Insert(1, new TextPayload($"{actionDescriptionTranslation}\n\n"));
        currentText.Payloads.Insert(2, new UIForegroundPayload(0));

        stringArrayData->SetValue(13, currentText.Encode(), false, true, true);
    }
}
