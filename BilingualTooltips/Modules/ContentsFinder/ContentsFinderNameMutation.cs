using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Miosuke.GameUi;

namespace BilingualTooltips.Modules;

sealed unsafe class ContentsFinderNameMutation
{
    private readonly ContentsFinderNameAddonContract _addonContract;
    private NameLayoutSnapshot? _nameLayoutSnapshot;

    public ContentsFinderNameMutation(ContentsFinderNameAddonContract addonContract)
    {
        _addonContract = addonContract;
    }

    public bool Apply(
        AtkUnitBase* addon,
        string translation,
        ContentsFinderNameAppearance appearance)
    {
        if (addon == null || string.IsNullOrWhiteSpace(translation)) return false;

        var addonAddress = (nint)addon;
        if (_nameLayoutSnapshot is { } existingSnapshot && existingSnapshot.AddonAddress != addonAddress) return false;

        var nativeNameNode = addon->GetTextNodeById(_addonContract.NativeNameNodeId);
        if (nativeNameNode == null) return false;

        var translationNode = AddonNodes.CreateTextNodeFromSourceBefore(
            addon,
            nativeNameNode,
            (AtkResNode*)nativeNameNode,
            _addonContract.PluginNameTranslationNodeId,
            lineSpacing: 18,
            fontSize: 14,
            updateDrawNodeList: false);
        if (translationNode == null) return false;

        var nativeNameY = _nameLayoutSnapshot?.NativeNameY ?? nativeNameNode->AtkResNode.Y;
        _nameLayoutSnapshot = new NameLayoutSnapshot(addonAddress, nativeNameY);

        translationNode->AlignmentType = AlignmentType.Center;
        SetColouredText(translationNode, $"～ {translation} ～", appearance.ColourKey);
        translationNode->ResizeNodeForCurrentText();
        translationNode->SetWidth(_addonContract.TranslationWidth);
        translationNode->SetHeight(_addonContract.TranslationHeight);
        translationNode->AtkResNode.SetPositionFloat(
            nativeNameNode->AtkResNode.X,
            nativeNameY + appearance.TranslationOffset);
        nativeNameNode->AtkResNode.SetPositionFloat(
            nativeNameNode->AtkResNode.X,
            nativeNameY + appearance.NativeNameOffset);
        translationNode->AtkResNode.ToggleVisibility(true);
        AddonNodes.UpdateDrawNodeList(addon);
        return true;
    }

    public void Release(AtkUnitBase* addon)
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
            var nativeNameNode = addon->GetTextNodeById(_addonContract.NativeNameNodeId);
            if (nativeNameNode != null)
            {
                nativeNameNode->AtkResNode.SetPositionFloat(nativeNameNode->AtkResNode.X, snapshot.NativeNameY);
            }

            _nameLayoutSnapshot = null;
        }
    }

    private static void SetColouredText(AtkTextNode* node, string text, ushort colourKey)
    {
        var lines = new SeString();
        lines.Payloads.Add(new UIForegroundPayload(colourKey));
        lines.Payloads.Add(new TextPayload(text));
        lines.Payloads.Add(new UIForegroundPayload(0));
        node->SetText(lines.Encode());
    }

    private sealed record NameLayoutSnapshot(nint AddonAddress, float NativeNameY);
}
