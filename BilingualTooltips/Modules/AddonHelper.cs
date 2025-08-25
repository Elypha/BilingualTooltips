using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game;
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
using Miosuke;


namespace BilingualTooltips.Modules;

public unsafe static class AddonHelper
{
    public static class ItemDetail
    {
        public const string Name = "ItemDetail";
        public static class StringArrayIndex
        {
            public const int Self = 27;
            public const int Description = 13;
        }

        public static class NumberArrayIndex
        {
            public const int Self = 30;
        }

        public static class TextNodeId
        {
            public const int Name = 33;
            public const int NameTranslation = 1270;
            public const int Description = 42;
        }
    }
    public static class ActionDetail
    {
        public const string Name = "ActionDetail";
        public static class StringArrayIndex
        {
            public const int Self = 29;
            public const int Description = 13;
        }

        public static class NumberArrayIndex
        {
            public const int Self = 32;
        }

        public static class TextNodeId
        {
            public const int Name = 5;
            public const int NameTranslation = 1270;
            public const int Description = 19;
        }
    }


    public unsafe static AtkTextNode* GetNodeByNodeId(AtkUnitBase* addon, int nodeId)
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

    public unsafe static void SetupTextNodeTooltip(AtkUnitBase* addon, AtkTextNode* baseTextNode, AtkResNode* insertNode, uint textNodeId, byte lineSpacing = 18, byte fontSize = 12)
    {
        var textNode = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
        if (textNode == null) return;
        textNode->AtkResNode.Type = NodeType.Text;
        textNode->AtkResNode.NodeId = textNodeId;


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

        textNode->LineSpacing = lineSpacing;
        textNode->AlignmentFontType = 0x00;
        textNode->FontSize = fontSize;
        textNode->TextFlags = baseTextNode->TextFlags | TextFlags.MultiLine | TextFlags.AutoAdjustNodeSize;
        // textNode->TextFlags2 = 0;

        var prev = insertNode->PrevSiblingNode;
        textNode->AtkResNode.ParentNode = insertNode->ParentNode;

        insertNode->PrevSiblingNode = (AtkResNode*)textNode;

        if (prev != null) prev->NextSiblingNode = (AtkResNode*)textNode;

        textNode->AtkResNode.PrevSiblingNode = prev;
        textNode->AtkResNode.NextSiblingNode = insertNode;

        addon->UldManager.UpdateDrawNodeList();
    }
}
