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

public partial class ContentHandler
{
    public string ContentConfirmNameTranslation = "";
    public string ContentConfirmDescTranslation = "";

    public enum ContentConfirmTextNode
    {
        Name = 49,
        // Description = 42,
        NameTranslated = 1270,
    }

    public unsafe void ContentsFinderConfirmHandler(AddonEvent type, AddonArgs args)
    {
        // if (args is not AddonRequestedUpdateArgs requestedUpdateArgs) return;
        if (!plugin.Config.Enabled) return;

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (!addon->IsVisible) return;

        UpdateContentConfirmDetail();
    }

    public unsafe void UpdateContentConfirmDetail()
    {
        var addon = (AtkUnitBase*)Service.GameGui.GetAddonByName("ContentsFinderConfirm").Address;
        if (addon == null) return;
        // if (!addon->IsVisible) return;

        var originalNamePtr = addon->GetTextNodeById((uint)ContentConfirmTextNode.Name);
        if (originalNamePtr == null) return;
        var originalName = MemoryHelper.ReadSeString(&originalNamePtr->NodeText).TextValue;

        if (plugin.Config.ContentsFinderName != GameLanguage.Off)
        {
            ResetContentConfirm();

            ContentNameTranslation = SheetHelper.GetContentName(originalName, plugin.Config.ContentsFinderName) ?? "";
            if (ContentNameTranslation.StartsWith("the ")) ContentNameTranslation = "The " + ContentNameTranslation[4..];

            AddContentConfirmNameTranslation(addon);
        }
    }

    public unsafe void ResetContentConfirm()
    {
        var addon = (AtkUnitBase*)Service.GameGui.GetAddonByName("ContentsFinderConfirm").Address;

        // remove translation if it exists
        var nameTranslationNode = AddonHelper.GetTextNodeById(addon, (int)ContentConfirmTextNode.NameTranslated);
        if (nameTranslationNode != null)
        {
            if (nameTranslationNode->AtkResNode.IsVisible())
            {
                var insertNode = addon->GetNodeById(2);
                if (insertNode == null) return;
                nameTranslationNode->AtkResNode.ToggleVisibility(false);
            }
        }

        // reset original item name position
        var name_node = addon->GetTextNodeById((uint)ContentConfirmTextNode.Name);
        float x, y;
        name_node->GetPositionFloat(&x, &y);
        name_node->SetPositionFloat(x, 72);
    }

    private unsafe void AddContentConfirmNameTranslation(AtkUnitBase* addon)
    {
        var translationNode = AddonHelper.GetTextNodeById(addon, (int)ContentConfirmTextNode.NameTranslated);
        if (translationNode != null) translationNode->AtkResNode.ToggleVisibility(false);

        var insertNode = addon->GetNodeById(2);
        if (insertNode == null) return;

        var baseTextNode = addon->GetTextNodeById((uint)ContentConfirmTextNode.Name);
        if (baseTextNode == null) return;

        if (translationNode == null)
        {
            translationNode = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
            if (translationNode == null) return;
            translationNode->AtkResNode.Type = NodeType.Text;
            translationNode->AtkResNode.NodeId = (int)ContentConfirmTextNode.NameTranslated;


            translationNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
            translationNode->AtkResNode.DrawFlags = 0;

            translationNode->AtkResNode.Color.A = baseTextNode->AtkResNode.Color.A;
            translationNode->AtkResNode.Color.R = baseTextNode->AtkResNode.Color.R;
            translationNode->AtkResNode.Color.G = baseTextNode->AtkResNode.Color.G;
            translationNode->AtkResNode.Color.B = baseTextNode->AtkResNode.Color.B;

            translationNode->TextColor.A = baseTextNode->TextColor.A;
            translationNode->TextColor.R = baseTextNode->TextColor.R;
            translationNode->TextColor.G = baseTextNode->TextColor.G;
            translationNode->TextColor.B = baseTextNode->TextColor.B;

            translationNode->EdgeColor.A = baseTextNode->EdgeColor.A;
            translationNode->EdgeColor.R = baseTextNode->EdgeColor.R;
            translationNode->EdgeColor.G = baseTextNode->EdgeColor.G;
            translationNode->EdgeColor.B = baseTextNode->EdgeColor.B;

            translationNode->LineSpacing = 18;
            translationNode->AlignmentFontType = 0x00;
            translationNode->AlignmentType = AlignmentType.Center;
            translationNode->FontSize = 14;
            translationNode->TextFlags = baseTextNode->TextFlags | TextFlags.MultiLine | TextFlags.AutoAdjustNodeSize;
            // translationNode->TextFlags2 = 0;

            var prev = insertNode->PrevSiblingNode;
            translationNode->AtkResNode.ParentNode = insertNode->ParentNode;

            insertNode->PrevSiblingNode = (AtkResNode*)translationNode;

            if (prev != null) prev->NextSiblingNode = (AtkResNode*)translationNode;

            translationNode->AtkResNode.PrevSiblingNode = prev;
            translationNode->AtkResNode.NextSiblingNode = insertNode;

            addon->UldManager.UpdateDrawNodeList();
        }

        translationNode->AtkResNode.ToggleVisibility(true);

        var lines = new SeString();
        lines.Payloads.Add(new UIForegroundPayload((ushort)plugin.Config.ContentNameColourKey));
        lines.Payloads.Add(new TextPayload($"～ {ContentNameTranslation} ～"));
        lines.Payloads.Add(new UIForegroundPayload(0));
        translationNode->SetText(lines.Encode());

        translationNode->ResizeNodeForCurrentText();
        translationNode->SetWidth(318);
        translationNode->SetHeight(44);
        var originalNode = addon->GetTextNodeById((uint)ContentConfirmTextNode.Name);
        var originalNodeOffset = plugin.Config.OffsetContentNameTranslation;
        translationNode->AtkResNode.SetPositionFloat(originalNode->AtkResNode.X, originalNode->AtkResNode.Y + originalNodeOffset);
        var translationNodeOffset = plugin.Config.OffsetContentNameOriginal;
        originalNode->SetPositionFloat(originalNode->AtkResNode.X, originalNode->AtkResNode.Y + translationNodeOffset);
    }
}
