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


namespace BilingualTooltips;

public partial class TooltipHandler
{
    public ExcelSheet<Lumina.Excel.Sheets.Action> SheetNameAction = Service.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>(Dalamud.Game.ClientLanguage.Japanese)!;
    public ExcelSheet<GeneralAction> SheetNameGeneralAction = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.Japanese)!;
    public ExcelSheet<Trait> SheetNameTrait = Service.Data.GetExcelSheet<Trait>(Dalamud.Game.ClientLanguage.Japanese)!;
    public ExcelSheet<ActionTransient> SheetDescActionTransient = Service.Data.GetExcelSheet<ActionTransient>(Dalamud.Game.ClientLanguage.Japanese)!;
    public ExcelSheet<GeneralAction> SheetDescGeneralAction = Service.Data.GetExcelSheet<GeneralAction>(Dalamud.Game.ClientLanguage.Japanese)!;
    public ExcelSheet<TraitTransient> SheetDescTraitTransient = Service.Data.GetExcelSheet<TraitTransient>(Dalamud.Game.ClientLanguage.Japanese)!;

    public string actionNameTranslation = "";
    public string actionDescriptionTranslation = "";


    private bool UpdateActionTooltipData()
    {
        var hover = Service.GameGui.HoveredAction;
        if (hover.ActionID == 0) return false;

        var mnemonic = Service.ClientState.LocalPlayer?.ClassJob.Value.Abbreviation.ToString();
        // var actionName = "";
        // var actionDescription = "";

        if (hover.ActionKind == HoverActionKind.Action)
        {
            actionNameTranslation = SheetNameAction.GetRow(hover.ActionID).Name.ExtractText();
            actionDescriptionTranslation = SheetDescActionTransient.GetRow(hover.ActionID).Description.ExtractText();
            return true;
        }
        else if (hover.ActionKind == HoverActionKind.GeneralAction)
        {
            actionNameTranslation = SheetNameGeneralAction.GetRow(hover.ActionID).Name.ExtractText();
            actionDescriptionTranslation = SheetDescGeneralAction.GetRow(hover.ActionID).Description.ExtractText();
            return true;
        }
        else if (hover.ActionKind == HoverActionKind.Trait)
        {
            actionNameTranslation = SheetNameTrait.GetRow(hover.ActionID).Name.ExtractText();
            actionDescriptionTranslation = SheetDescTraitTransient.GetRow(hover.ActionID).Description.ExtractText();
            return true;
        }
        else if ((uint)hover.ActionKind == 34 || (uint)hover.ActionKind == 39)
        {
            // 34 for minions, 39 for mounts
            actionNameTranslation = "";
            actionDescriptionTranslation = "";
            return false;
        }
        else if (hover.ActionKind == HoverActionKind.MainCommand || hover.ActionKind == HoverActionKind.ExtraCommand)
        {
            // others not related
            actionNameTranslation = "";
            actionDescriptionTranslation = "";
            return false;
        }
        else
        {
            Service.Log.Warning($"Unsupported action kind for {hover.ActionID} [{hover.ActionKind}]");
            return false;
        }
    }

    public unsafe void ResetActionTooltip()
    {
        var addon = Service.GameGui.GetAddonByName("ActionDetail");
        var addonPtr = (AtkUnitBase*)addon;

        // remove translation if it exists
        var customNode = GetNodeByNodeId(addonPtr, (int)ActionDescriptionNode.NameTranslated);
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
        var name_node = addonPtr->GetTextNodeById((uint)ActionDescriptionNode.Name);
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
        if (UpdateActionTooltipData())
        {
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
    }


    private unsafe void AddActionNameTranslation(AtkUnitBase* addon)
    {
        var textNode = GetNodeByNodeId(addon, (int)ActionDescriptionNode.NameTranslated);
        if (textNode != null) textNode->AtkResNode.ToggleVisibility(false);

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var baseTextNode = addon->GetTextNodeById((uint)ActionDescriptionNode.Name);
        if (baseTextNode == null) return;

        if (textNode == null)
        {
            textNode = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
            if (textNode == null) return;
            textNode->AtkResNode.Type = NodeType.Text;
            textNode->AtkResNode.NodeId = (int)ActionDescriptionNode.NameTranslated;


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
        textNode->SetWidth(300);
        textNode->SetHeight(21);
        var itemNameNode = addon->GetTextNodeById((uint)ActionDescriptionNode.Name);
        var textNodeOffset = plugin.Config.OffsetActionNameTranslation;
        textNode->AtkResNode.SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + textNodeOffset);

        var itemNameOffset = plugin.Config.OffsetActionNameOriginal;
        itemNameNode->SetPositionFloat(itemNameNode->AtkResNode.X, itemNameNode->AtkResNode.Y + itemNameOffset);

    }


    private unsafe void AddActionDescriptionTranslation(AtkUnitBase* addon, StringArrayData* stringArrayData)
    {
        var descriptionNode = addon->GetTextNodeById((uint)ActionDescriptionNode.Description);
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
