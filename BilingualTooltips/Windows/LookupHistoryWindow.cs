using System.Globalization;
using BilingualTooltips.Modules.Dialogue;
using BilingualTooltips.Modules.Lookup;
using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public sealed class LookupHistoryWindow : Window, IDisposable
{
    private readonly BilingualTooltipsPlugin _plugin;

    private BttLookupKey? _selectedKey;
    private BttLookupKey? _resolvedKey;
    private BttLookupResolvedItem? _resolvedItem;
    private int _resolvedDialogueGeneration = -1;
    private bool _copyMode;

    public LookupHistoryWindow(BilingualTooltipsPlugin plugin) : base("Lookup History")
    {
        _plugin = plugin;
        Size = new Vector2(860, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose()
    {
        ClearDialogueRequirement();
    }

    public void OnHotkeyTriggered()
    {
        if (!_plugin.Config.LookupHistoryHotkeyEnabled) return;

        IsOpen = !IsOpen;
        if (IsOpen)
        {
            UpdateDialogueRequirement();
            RefreshSelected();
        }
        else
        {
            ClearDialogueRequirement();
        }
    }

    public override void OnOpen()
    {
        EnsureSelectedEntry();
        UpdateDialogueRequirement();
        RefreshSelected();
    }

    public override void OnClose()
    {
        ClearDialogueRequirement();
    }

    public override void Draw()
    {
        UpdateDialogueRequirement();
        DrawToolbar();
        ImGui.Separator();

        var availableHeight = ImGui.GetContentRegionAvail().Y;
        using var table = ImRaii.Table(
            "##LookupHistoryLayout",
            2,
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV,
            new Vector2(-1, availableHeight));
        if (!table) return;

        ImGui.TableSetupColumn("History", ImGuiTableColumnFlags.WidthFixed, 260f);
        ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        DrawHistoryList();
        ImGui.TableNextColumn();
        DrawSelectedDetails();
    }

    private void DrawToolbar()
    {
        if (ImGui.Button("Refresh"))
        {
            UpdateDialogueRequirement();
            RefreshSelected();
        }

        ImGui.SameLine();
        ImGui.Checkbox("Copy mode", ref _copyMode);

        ImGui.SameLine();
        if (ImGui.Button("Clear history"))
        {
            _plugin.LookupHistory.Clear();
            _selectedKey = null;
            _resolvedKey = null;
            _resolvedItem = null;
            _resolvedDialogueGeneration = -1;
            ClearDialogueRequirement();
        }

        ImGui.SameLine();
        ImGui.TextDisabled($"{_plugin.LookupHistory.Entries.Count}/{_plugin.Config.LookupHistoryLimit}");
    }

    private void DrawHistoryList()
    {
        var entries = _plugin.LookupHistory.Entries;
        if (entries.Count == 0)
        {
            ImGui.TextWrapped("No lookup history yet.");
            return;
        }

        EnsureSelectedEntry();

        using var child = ImRaii.Child("##LookupHistoryEntries", new Vector2(-1, -1), false);
        if (!child) return;

        foreach (var entry in entries)
        {
            DrawHistoryEntry(entry);
        }
    }

    private void DrawHistoryEntry(BttLookupHistoryEntry entry)
    {
        var selected = entry.Key == _selectedKey;
        var displayName = entry.DisplayName;
        var displayId = BttLookupDisplay.DisplayId(entry.Key);
        var countText = $" ({entry.Count})";
        var start = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var lineHeight = ImGui.GetTextLineHeight();
        var style = ImGui.GetStyle();
        var padding = new Vector2(4f, 2f);
        var rowHeight = (lineHeight * 2f) + style.ItemSpacing.Y + (padding.Y * 2f);

        ImGui.InvisibleButton($"##LookupHistoryEntry:{entry.Key}", new Vector2(width, rowHeight));
        var clicked = ImGui.IsItemClicked();
        var hovered = ImGui.IsItemHovered();
        if (clicked)
        {
            _selectedKey = entry.Key;
            RefreshSelected();
        }

        var end = start + new Vector2(width, rowHeight);
        var drawList = ImGui.GetWindowDrawList();
        if (selected || hovered)
        {
            var colour = ImGui.GetColorU32(selected ? ImGuiCol.Header : ImGuiCol.HeaderHovered);
            drawList.AddRectFilled(start, end, colour, 2f);
        }

        var textPosition = start + padding;
        var textColour = ImGui.GetColorU32(ImGuiCol.Text);
        var disabledColour = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        drawList.AddText(textPosition, textColour, displayName);
        drawList.AddText(
            textPosition + new Vector2(ImGui.CalcTextSize(displayName).X + 4f, 0f),
            disabledColour,
            countText);
        drawList.AddText(
            textPosition + new Vector2(0f, lineHeight + style.ItemSpacing.Y),
            disabledColour,
            displayId);

        if (hovered)
        {
            ImGui.SetTooltip($"{displayName}\n{displayId}");
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.25f * ImGui.GetTextLineHeight()));
    }

    private void DrawSelectedDetails()
    {
        EnsureSelectedEntry();
        if (_selectedKey == null)
        {
            ImGui.TextWrapped("Select a history entry to review it.");
            return;
        }

        if (_resolvedItem == null || _resolvedKey != _selectedKey)
        {
            RefreshSelected();
        }
        else if (_selectedKey.Kind == BttLookupKind.NpcDialogue
                 && _resolvedDialogueGeneration != _plugin.DialogueStoreProvider.Generation)
        {
            RefreshSelected();
        }

        if (_resolvedItem == null)
        {
            ImGui.TextWrapped("Unable to resolve this history entry.");
            return;
        }

        var selectedEntry = SelectedEntry();
        if (selectedEntry == null)
        {
            ImGui.TextWrapped("Selected history entry is no longer available.");
            return;
        }

        DrawSelectedHeader(_resolvedItem, selectedEntry);

        using var child = ImRaii.Child("##LookupHistoryDetails", new Vector2(-1, -1), false);
        if (!child) return;

        foreach (var line in _resolvedItem.Lines)
        {
            LanguageTextBlock.Draw(
                line.Available ? null : "Unavailable",
                line.Text,
                LanguageTextBlock.LanguageColour(line.Language),
                line.Language.DisplayName(),
                line.Available ? Ui.ColourWhite : Ui.ColourWhite3);

            if (_copyMode && line.Available)
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 14f);
                if (ImGui.SmallButton($"Copy {line.Language.DisplayName()}##{_resolvedItem.Key}:{line.Language}"))
                {
                    ImGui.SetClipboardText(line.Text);
                }

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.2f * ImGui.GetTextLineHeight()));
            }
        }
    }

    private void DrawSelectedHeader(BttLookupResolvedItem item, BttLookupHistoryEntry entry)
    {
        var displayName = entry.DisplayName;
        var timeText = entry.LastSeenAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        var startY = ImGui.GetCursorPosY();
        ImGui.TextColored(Ui.ColourCyan, item.KindLabel);
        ImGui.SameLine();
        ImGui.TextWrapped(displayName);
        var afterTitlePosition = ImGui.GetCursorPos();

        if (!string.IsNullOrWhiteSpace(timeText))
        {
            var timeWidth = ImGui.CalcTextSize(timeText).X;
            var rightX = ImGui.GetWindowContentRegionMax().X - timeWidth;
            if (rightX > ImGui.GetCursorPosX())
            {
                ImGui.SetCursorPos(new Vector2(rightX, startY));
                ImGui.TextDisabled(timeText);
            }
        }

        ImGui.SetCursorPos(afterTitlePosition);
        var id = BttLookupDisplay.DisplayId(item.Key);
        if (!string.IsNullOrWhiteSpace(id))
        {
            ImGui.TextDisabled(id);
        }

        if (_copyMode)
        {
            if (ImGui.Button("Copy all"))
            {
                ImGui.SetClipboardText(BuildCopyText(item));
            }
        }

        ImGui.Separator();
    }

    private void EnsureSelectedEntry()
    {
        var entries = _plugin.LookupHistory.Entries;
        if (entries.Count == 0)
        {
            _selectedKey = null;
            return;
        }

        if (_selectedKey != null && entries.Any(entry => entry.Key == _selectedKey)) return;

        _selectedKey = entries[0].Key;
    }

    private BttLookupHistoryEntry? SelectedEntry() => _selectedKey == null
        ? null
        : _plugin.LookupHistory.Entries.FirstOrDefault(entry => entry.Key == _selectedKey);

    private void RefreshSelected()
    {
        if (_selectedKey == null)
        {
            _resolvedKey = null;
            _resolvedItem = null;
            _resolvedDialogueGeneration = -1;
            return;
        }

        _resolvedItem = _plugin.LookupResolver.Resolve(_selectedKey);
        _resolvedKey = _selectedKey;
        _resolvedDialogueGeneration = _selectedKey.Kind == BttLookupKind.NpcDialogue
            ? _plugin.DialogueStoreProvider.Generation
            : -1;
    }

    private void UpdateDialogueRequirement()
    {
        if (_plugin.LookupHistory.Entries.Any(static entry => entry.Key.Kind == BttLookupKind.NpcDialogue))
        {
            _plugin.DialogueStoreProvider.SetRequirement(
                DialogueStoreProvider.LookupHistoryConsumer,
                DialogueShardRequirement.ForLookupHistory());
            return;
        }

        ClearDialogueRequirement();
    }

    private void ClearDialogueRequirement()
    {
        _plugin.DialogueStoreProvider.ClearRequirement(DialogueStoreProvider.LookupHistoryConsumer);
    }

    private static string BuildCopyText(BttLookupResolvedItem item)
    {
        var builder = new StringBuilder();
        builder.AppendLine(item.KindLabel);
        if (!string.IsNullOrWhiteSpace(item.Subtitle))
        {
            builder.AppendLine(item.Subtitle);
        }

        foreach (var line in item.Lines.Where(static line => line.Available))
        {
            builder.AppendLine();
            builder.AppendLine(line.Language.DisplayName());
            builder.AppendLine(line.Text);
        }

        return builder.ToString().TrimEnd();
    }
}
