using BilingualTooltips.Modules.Dialogue;
using BilingualTooltips.Modules.Lookup;
using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public partial class ConfigWindow
{
    private string _generalShowcaseDisplayName = "";
    private BttLookupResolvedItem? _generalShowcaseItem;
    private BttLanguage? _generalShowcaseSecondaryLanguage;
    private float _generalShowcaseMeasuredHeight;
    private int _generalShowcaseDialogueGeneration = -1;

    private void RefreshGeneralShowcase(bool force = false)
    {
        if (!_plugin.Config.LookupHistoryEnabled || !_plugin.Config.LookupHistoryShowcaseEnabled)
        {
            ResetGeneralShowcase();
            return;
        }

        if (!force && _generalShowcaseItem != null) return;

        var entries = _plugin.LookupHistory.Entries;
        if (entries.Count == 0)
        {
            ResetGeneralShowcase();
            return;
        }

        var entry = entries[Random.Shared.Next(entries.Count)];
        _generalShowcaseDisplayName = entry.DisplayName;
        _generalShowcaseItem = _plugin.LookupResolver.Resolve(entry.Key);
        _generalShowcaseDialogueGeneration = entry.Key.Kind == BttLookupKind.NpcDialogue
            ? _plugin.DialogueStoreProvider.Generation
            : -1;
        _generalShowcaseSecondaryLanguage = SelectGeneralShowcaseSecondaryLanguage(_generalShowcaseItem);
        _generalShowcaseMeasuredHeight = 0;
    }

    private void DrawGeneralShowcase(string suffix)
    {
        if (!_plugin.Config.LookupHistoryEnabled || !_plugin.Config.LookupHistoryShowcaseEnabled) return;

        if (_generalShowcaseItem == null)
        {
            RefreshGeneralShowcase();
        }

        if (_generalShowcaseItem == null) return;

        RefreshGeneralShowcaseDialogueItemIfChanged();

        const float paddingX = 10f;
        const float paddingY = 8f;
        const float rounding = 6f;
        var lineHeight = ImGui.GetTextLineHeightWithSpacing();
        var panelHeight = _generalShowcaseMeasuredHeight > 0
            ? _generalShowcaseMeasuredHeight
            : (lineHeight * 6f) + (paddingY * 2);

        using var styles = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, rounding)
            .Push(ImGuiStyleVar.WindowPadding, new Vector2(paddingX, paddingY));
        using var colours = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0f, 0f, 0f, 0f))
            .Push(ImGuiCol.Border, new Vector4(1f, 1f, 1f, 0.18f));
        using var panel = ImRaii.Child(
            $"##{suffix}HistoryShowcasePanel",
            new Vector2(-1, panelHeight),
            true,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!panel) return;

        var contentStartY = ImGui.GetCursorPosY();
        DrawGeneralShowcaseHeader(suffix);

        foreach (var line in GeneralShowcaseLines(_generalShowcaseItem))
        {
            LanguageTextBlock.Draw(
                line.Available ? null : "Unavailable",
                line.Text,
                LanguageTextBlock.LanguageColour(line.Language),
                line.Language.DisplayName(),
                line.Available ? Ui.ColourWhite : Ui.ColourWhite3);
        }

        UpdateGeneralShowcaseMeasuredHeight(contentStartY, paddingY);
    }

    private void UpdateGeneralShowcaseMeasuredHeight(float contentStartY, float paddingY)
    {
        const float minimumHeight = 72f;
        var contentHeight = ImGui.GetCursorPosY() - contentStartY;
        _generalShowcaseMeasuredHeight = MathF.Ceiling(Math.Max(minimumHeight, contentHeight + (paddingY * 2)));
    }

    private void DrawGeneralShowcaseHeader(string suffix)
    {
        const float lineWidth = 5f;
        const float indent = 14f;
        const float lineOffset = 2f;
        const float buttonWidth = 112f;
        var item = _generalShowcaseItem;
        if (item == null) return;

        var style = ImGui.GetStyle();
        var start = ImGui.GetCursorScreenPos();
        var cursorX = ImGui.GetCursorPosX();
        var cursorY = ImGui.GetCursorPosY();
        var buttonX = MathF.Max(
            cursorX + indent + 120f,
            ImGui.GetWindowContentRegionMax().X - buttonWidth);

        ImGui.SetCursorPos(new Vector2(buttonX, cursorY));
        if (ImGui.Button($"Show Another{suffix}ShowAnotherHistoryShowcaseSample", new Vector2(buttonWidth, 0)))
        {
            RefreshGeneralShowcase(force: true);
            item = _generalShowcaseItem;
            if (item == null) return;
        }

        ImGui.SetCursorPos(new Vector2(cursorX + indent, cursorY));
        ImGui.BeginGroup();
        ImGui.TextColored(Ui.ColourCyan, item.KindLabel);
        ImGui.SameLine();
        var wrapX = MathF.Max(cursorX + indent + 120f, buttonX - style.ItemSpacing.X);
        ImGui.PushTextWrapPos(wrapX);
        ImGui.TextUnformatted(_generalShowcaseDisplayName);
        var id = BttLookupDisplay.DisplayId(item.Key);
        if (!string.IsNullOrWhiteSpace(id))
        {
            ImGui.TextDisabled(id);
        }

        ImGui.PopTextWrapPos();
        ImGui.EndGroup();

        var end = ImGui.GetItemRectMax();
        var lineMin = new Vector2(start.X + lineOffset, start.Y + 1f);
        var lineMax = new Vector2(lineMin.X + lineWidth, end.Y - 1f);
        ImGui.GetWindowDrawList().AddRectFilled(
            lineMin,
            lineMax,
            ImGui.GetColorU32(Ui.ColourCyan),
            lineWidth * 0.45f);
        if (ImGui.IsMouseHoveringRect(lineMin - new Vector2(1f, 1f), lineMax + new Vector2(3f, 1f), false))
        {
            ImGui.SetTooltip("History sample");
        }

        ImGui.SetCursorPosX(cursorX);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.45f * ImGui.GetTextLineHeight()));
    }

    private IEnumerable<BttLookupResolvedLine> GeneralShowcaseLines(BttLookupResolvedItem item)
    {
        var clientLanguage = BttLanguageExtensions.FromClientLanguage(Service.ClientState.ClientLanguage);
        var clientLine = item.Lines.FirstOrDefault(line => line.Language == clientLanguage);
        if (clientLine != null)
        {
            yield return clientLine;
        }

        if (_generalShowcaseSecondaryLanguage is not { } secondaryLanguage)
        {
            yield break;
        }

        var secondaryLine = item.Lines.FirstOrDefault(line => line.Language == secondaryLanguage);
        if (secondaryLine != null)
        {
            yield return secondaryLine;
        }
    }

    private static BttLanguage? SelectGeneralShowcaseSecondaryLanguage(BttLookupResolvedItem item)
    {
        var clientLanguage = BttLanguageExtensions.FromClientLanguage(Service.ClientState.ClientLanguage);
        var candidates = item.Lines
            .Where(line => line.Language != clientLanguage
                && line.Available
                && !string.IsNullOrWhiteSpace(line.Text))
            .Select(static line => line.Language)
            .ToArray();

        return candidates.Length == 0
            ? null
            : candidates[Random.Shared.Next(candidates.Length)];
    }

    private void RefreshGeneralShowcaseDialogueItemIfChanged()
    {
        var item = _generalShowcaseItem;
        if (item?.Key.Kind != BttLookupKind.NpcDialogue) return;

        var generation = _plugin.DialogueStoreProvider.Generation;
        if (_generalShowcaseDialogueGeneration == generation) return;

        _generalShowcaseItem = _plugin.LookupResolver.Resolve(item.Key);
        _generalShowcaseDialogueGeneration = generation;
        _generalShowcaseSecondaryLanguage = SelectGeneralShowcaseSecondaryLanguage(_generalShowcaseItem);
        _generalShowcaseMeasuredHeight = 0;
    }

    private void UpdateGeneralShowcaseDialogueRequirement()
    {
        if (_plugin.Config is { LookupHistoryEnabled: true, LookupHistoryShowcaseEnabled: true }
            && _plugin.LookupHistory.Entries.Any(static entry => entry.Key.Kind == BttLookupKind.NpcDialogue))
        {
            _plugin.DialogueStoreProvider.SetRequirement(
                DialogueStoreProvider.GeneralShowcaseConsumer,
                DialogueShardRequirement.ForLookupHistory());
            return;
        }

        ClearGeneralShowcaseDialogueRequirement();
    }

    private void ResetGeneralShowcase()
    {
        _generalShowcaseDisplayName = "";
        _generalShowcaseItem = null;
        _generalShowcaseSecondaryLanguage = null;
        _generalShowcaseMeasuredHeight = 0;
        _generalShowcaseDialogueGeneration = -1;
        ClearGeneralShowcaseDialogueRequirement();
    }

    private void ClearGeneralShowcaseDialogueRequirement()
    {
        _plugin.DialogueStoreProvider.ClearRequirement(DialogueStoreProvider.GeneralShowcaseConsumer);
    }
}
