using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Miosuke.GameUi;

namespace BilingualTooltips.Modules;

sealed unsafe class TooltipDetailMutation
{
    private const ushort NameMeasurementWidth = 1024;
    private const float TooltipDescriptionDividerBeforeGapLineFactor = 0.67f;
    private const float TooltipDescriptionDividerAfterGapLineFactor = 0.05f;
    private const float DescriptionCaptureScreenYTolerance = 0.5f;

    private readonly TooltipDetailAddonContract _addonContract;
    private NameLayoutSnapshot? _nameLayoutSnapshot;
    private DescriptionLayoutSnapshot? _descriptionLayoutSnapshot;

    public TooltipDetailMutation(TooltipDetailAddonContract addonContract)
    {
        _addonContract = addonContract;
    }

    public void Release(AtkUnitBase* addon)
    {
        ReleaseDescription(addon);
        ReleaseName(addon);
    }

    public void ReleaseName(AtkUnitBase* addon)
    {
        if (addon == null)
        {
            _nameLayoutSnapshot = null;
            return;
        }

        var translationNode = AddonNodes.FindTextNodeInNodeList(addon, _addonContract.PluginNameTranslationNodeId);
        if (translationNode != null)
        {
            if (translationNode->AtkResNode.IsVisible()) translationNode->AtkResNode.ToggleVisibility(false);

            AddonNodes.DestroyLinkedTextNode(addon, translationNode);
        }

        if (_nameLayoutSnapshot is { } snapshot && snapshot.AddonAddress == (nint)addon)
        {
            var nameNode = addon->GetTextNodeById(_addonContract.NativeNameNodeId);
            if (nameNode != null)
                nameNode->AtkResNode.SetPositionFloat(nameNode->AtkResNode.X, snapshot.NativeNameY);
        }

        _nameLayoutSnapshot = null;
    }

    public void ReleaseDescription(AtkUnitBase* addon)
    {
        if (addon == null)
        {
            _descriptionLayoutSnapshot = null;
            return;
        }

        if (_descriptionLayoutSnapshot is { } snapshot && snapshot.AddonAddress == (nint)addon)
            RestoreDescriptionLayout(addon, snapshot);

        _descriptionLayoutSnapshot = null;

        var dividerNode = AddonNodes.FindNodeInNodeList(addon, _addonContract.PluginDescriptionTranslationDividerNodeId);
        if (dividerNode != null)
        {
            if (dividerNode->IsVisible()) dividerNode->ToggleVisibility(false);

            AddonNodes.DestroyLinkedNode(addon, dividerNode);
        }

        var translationNode = AddonNodes.FindTextNodeInNodeList(addon, _addonContract.PluginDescriptionTranslationNodeId);
        if (translationNode == null) return;

        if (translationNode->AtkResNode.IsVisible()) translationNode->AtkResNode.ToggleVisibility(false);

        AddonNodes.DestroyLinkedTextNode(addon, translationNode);
    }

    public void ApplyName(
        AtkUnitBase* addon,
        string translation,
        ushort colourKey,
        float translationOffset,
        float nativeOffset,
        float maxLineWidth)
    {
        if (addon == null || string.IsNullOrEmpty(translation)) return;

        var nativeNameNode = addon->GetTextNodeById(_addonContract.NativeNameNodeId);
        if (nativeNameNode == null) return;

        if (_nameLayoutSnapshot == null || _nameLayoutSnapshot.AddonAddress != (nint)addon)
        {
            _nameLayoutSnapshot = new NameLayoutSnapshot((nint)addon, nativeNameNode->AtkResNode.Y);
        }

        var translationNode = AddonNodes.CreateTextNodeFromSourceBefore(
            addon,
            nativeNameNode,
            (AtkResNode*)nativeNameNode,
            _addonContract.PluginNameTranslationNodeId,
            updateDrawNodeList: false);
        if (translationNode == null) return;

        translationNode->AtkResNode.SetScaleX(1f);
        translationNode->SetWidth(NameMeasurementWidth);
        SetColouredText(translationNode, translation, colourKey);
        translationNode->ResizeNodeForCurrentText();

        if (maxLineWidth > 0f)
        {
            var widthRatio = translationNode->AtkResNode.Width / maxLineWidth;
            if (widthRatio > 1f)
            {
                translationNode->AtkResNode.SetScaleX(translationNode->AtkResNode.ScaleX / widthRatio);
                translationNode->SetWidth((ushort)maxLineWidth);
            }
        }

        var nativeY = _nameLayoutSnapshot.NativeNameY;
        translationNode->AtkResNode.SetPositionFloat(nativeNameNode->AtkResNode.X, nativeY + translationOffset);
        nativeNameNode->AtkResNode.SetPositionFloat(nativeNameNode->AtkResNode.X, nativeY + nativeOffset);
        translationNode->AtkResNode.ToggleVisibility(true);
        AddonNodes.UpdateDrawNodeList(addon);
    }

    public void ApplyDescription(
        AtkUnitBase* addon,
        string translation,
        ushort colourKey)
    {
        if (addon == null || string.IsNullOrEmpty(translation)) return;

        var descriptionNode = addon->GetTextNodeById(_addonContract.NativeDescriptionNodeId);
        if (descriptionNode == null) return;

        var dividerSourceNode = GetDescriptionDividerNode(
            descriptionNode,
            _addonContract.NativeDescriptionDividerNodeId);
        if (dividerSourceNode == null) return;

        var snapshot = CaptureDescriptionLayout(addon, descriptionNode);
        var translationNode = AddonNodes.CreateTextNodeFromSourceBefore(
            addon,
            descriptionNode,
            (AtkResNode*)descriptionNode,
            _addonContract.PluginDescriptionTranslationNodeId,
            descriptionNode->LineSpacing,
            descriptionNode->FontSize,
            updateDrawNodeList: false);
        if (translationNode == null) return;

        var dividerNode = CreateTooltipDividerNode(
            addon,
            dividerSourceNode,
            (AtkResNode*)descriptionNode,
            _addonContract.PluginDescriptionTranslationDividerNodeId,
            updateDrawNodeList: false);
        if (dividerNode == null)
        {
            AddonNodes.DestroyLinkedTextNode(addon, translationNode);
            return;
        }

        translationNode->AtkResNode.SetScaleX(1f);
        translationNode->SetWidth(descriptionNode->AtkResNode.Width);
        SetColouredText(translationNode, translation, colourKey);
        translationNode->ResizeNodeForCurrentText();
        translationNode->SetWidth(descriptionNode->AtkResNode.Width);
        translationNode->AtkResNode.SetPositionFloat(descriptionNode->AtkResNode.X, snapshot.DescriptionY);

        var dividerGap = GetDescriptionDividerGap(descriptionNode, dividerSourceNode);
        var dividerY = snapshot.DescriptionY
            + translationNode->AtkResNode.Height
            + GetDescriptionDividerBreathingGap(translationNode, TooltipDescriptionDividerBeforeGapLineFactor)
            + dividerGap;
        var dividerResNode = (AtkResNode*)dividerNode;
        dividerResNode->SetPositionFloat(dividerSourceNode->AtkResNode.X, dividerY);

        var shift = dividerY
            + GetScaledHeight(dividerResNode)
            + dividerGap
            + GetDescriptionDividerBreathingGap(translationNode, TooltipDescriptionDividerAfterGapLineFactor)
            - snapshot.DescriptionY;

        _descriptionLayoutSnapshot = snapshot;
        ApplyDescriptionLayout(addon, snapshot, shift);
        translationNode->AtkResNode.ToggleVisibility(true);
        dividerResNode->ToggleVisibility(true);
        AddonNodes.UpdateDrawNodeList(addon);
    }

    private static void SetColouredText(AtkTextNode* node, string text, ushort colourKey)
    {
        var lines = new SeString();
        lines.Payloads.Add(new UIForegroundPayload(colourKey));
        lines.Payloads.Add(new TextPayload(text));
        lines.Payloads.Add(new UIForegroundPayload(0));
        node->SetText(lines.Encode());
    }

    private static AtkNineGridNode* GetDescriptionDividerNode(
        AtkTextNode* descriptionNode,
        uint dividerNodeId)
    {
        if (descriptionNode == null || dividerNodeId == 0) return null;

        var descriptionResNode = (AtkResNode*)descriptionNode;
        var dividerNode = descriptionResNode->PrevSiblingNode;
        if (dividerNode == null
            || dividerNode->NodeId != dividerNodeId
            || dividerNode->Type != NodeType.NineGrid
            || dividerNode->ParentNode != descriptionResNode->ParentNode
            || dividerNode->NextSiblingNode != descriptionResNode
            || !dividerNode->IsVisible())
        {
            return null;
        }

        return (AtkNineGridNode*)dividerNode;
    }

    private static AtkNineGridNode* CreateTooltipDividerNode(
        AtkUnitBase* addon,
        AtkNineGridNode* sourceNode,
        AtkResNode* insertNode,
        uint nodeId,
        bool updateDrawNodeList = true) =>
        AddonNodes.CreateNineGridNodeFromSourceBefore(addon, sourceNode, insertNode, nodeId, updateDrawNodeList);

    private DescriptionLayoutSnapshot CaptureDescriptionLayout(AtkUnitBase* addon, AtkTextNode* descriptionNode)
    {
        var windowHeight = addon->WindowNode == null
            ? (ushort?)null
            : addon->WindowNode->AtkResNode.Height;
        var windowBackground = GetWindowBackground(addon);
        var windowBackgroundHeight = windowBackground == null
            ? (ushort?)null
            : windowBackground->Height;
        var descriptionY = descriptionNode->AtkResNode.Y;
        var descriptionScreenY = descriptionNode->AtkResNode.ScreenY;
        var snapshot = new DescriptionLayoutSnapshot((nint)addon, descriptionY, windowHeight, windowBackgroundHeight);
        var candidates = new List<nint>();
        var candidateAddresses = new HashSet<nint>();

        for (var i = 0; i < addon->UldManager.NodeListCount; i++)
        {
            var node = addon->UldManager.NodeList[i];
            if (node == null
                || IsPluginNode(node)
                || !node->IsVisible()
                || node->ScreenY + DescriptionCaptureScreenYTolerance < descriptionScreenY)
            {
                continue;
            }

            var address = (nint)node;
            candidates.Add(address);
            candidateAddresses.Add(address);
        }

        foreach (var candidate in candidates)
        {
            var node = (AtkResNode*)candidate;
            if (HasCapturedAncestor(node, candidateAddresses)) continue;

            snapshot.NodePositions.Add(new NodePosition(candidate, node->Y));
        }

        return snapshot;
    }

    private static void ApplyDescriptionLayout(AtkUnitBase* addon, DescriptionLayoutSnapshot snapshot, float shift)
    {
        if ((nint)addon != snapshot.AddonAddress) return;

        foreach (var nodePosition in snapshot.NodePositions)
        {
            var node = (AtkResNode*)nodePosition.Address;
            if (node == null) continue;

            node->SetPositionFloat(node->X, nodePosition.Y + shift);
        }

        var heightDelta = (ushort)MathF.Ceiling(MathF.Max(0f, shift));
        if (snapshot.WindowHeight.HasValue && addon->WindowNode != null)
        {
            addon->WindowNode->AtkResNode.SetHeight((ushort)(snapshot.WindowHeight.Value + heightDelta));
        }

        var windowBackground = GetWindowBackground(addon);
        if (snapshot.WindowBackgroundHeight.HasValue && windowBackground != null)
        {
            windowBackground->SetHeight((ushort)(snapshot.WindowBackgroundHeight.Value + heightDelta));
        }
    }

    private static void RestoreDescriptionLayout(AtkUnitBase* addon, DescriptionLayoutSnapshot snapshot)
    {
        foreach (var nodePosition in snapshot.NodePositions)
        {
            var node = (AtkResNode*)nodePosition.Address;
            if (node == null) continue;

            node->SetPositionFloat(node->X, nodePosition.Y);
        }

        if (snapshot.WindowHeight.HasValue && addon->WindowNode != null)
        {
            addon->WindowNode->AtkResNode.SetHeight(snapshot.WindowHeight.Value);
        }

        var windowBackground = GetWindowBackground(addon);
        if (snapshot.WindowBackgroundHeight.HasValue && windowBackground != null)
        {
            windowBackground->SetHeight(snapshot.WindowBackgroundHeight.Value);
        }
    }

    private static AtkResNode* GetWindowBackground(AtkUnitBase* addon) =>
        addon->WindowNode == null || addon->WindowNode->Component == null
            ? null
            : addon->WindowNode->Component->UldManager.SearchNodeById(2);

    private bool IsPluginNode(AtkResNode* node) =>
        node->NodeId == _addonContract.PluginNameTranslationNodeId
        || node->NodeId == _addonContract.PluginDescriptionTranslationNodeId
        || node->NodeId == _addonContract.PluginDescriptionTranslationDividerNodeId;

    private static bool HasCapturedAncestor(AtkResNode* node, HashSet<nint> capturedAddresses)
    {
        var parent = node->ParentNode;
        while (parent != null)
        {
            if (capturedAddresses.Contains((nint)parent)) return true;

            parent = parent->ParentNode;
        }

        return false;
    }

    private static float GetDescriptionDividerGap(AtkTextNode* descriptionNode, AtkNineGridNode* dividerNode) =>
        descriptionNode == null || dividerNode == null
            ? 0f
            : MathF.Max(0f, descriptionNode->AtkResNode.Y - (dividerNode->AtkResNode.Y + GetScaledHeight((AtkResNode*)dividerNode)));

    private static float GetDescriptionDividerBreathingGap(AtkTextNode* descriptionTranslationNode, float lineFactor) =>
        descriptionTranslationNode == null
            ? 0f
            : MathF.Ceiling(descriptionTranslationNode->LineSpacing * lineFactor);

    private static float GetScaledHeight(AtkResNode* node) =>
        node == null
            ? 0f
            : node->Height * MathF.Max(0f, node->ScaleY);

    private sealed record NameLayoutSnapshot(nint AddonAddress, float NativeNameY);

    private sealed class DescriptionLayoutSnapshot
    {
        public DescriptionLayoutSnapshot(
            nint addonAddress,
            float descriptionY,
            ushort? windowHeight,
            ushort? windowBackgroundHeight)
        {
            AddonAddress = addonAddress;
            DescriptionY = descriptionY;
            WindowHeight = windowHeight;
            WindowBackgroundHeight = windowBackgroundHeight;
        }

        public nint AddonAddress { get; }
        public float DescriptionY { get; }
        public ushort? WindowHeight { get; }
        public ushort? WindowBackgroundHeight { get; }
        public List<NodePosition> NodePositions { get; } = [];
    }

    private readonly record struct NodePosition(nint Address, float Y);
}
