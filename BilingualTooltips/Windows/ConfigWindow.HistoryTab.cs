using BilingualTooltips.Configuration;
using BilingualTooltips.Modules.Lookup;
using Miosuke.Configuration;
using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public partial class ConfigWindow
{
    private readonly HotkeyEditor _lookupHistoryHotkeyEditor = new();

    private void DrawHistoryTab(float padding)
    {
        var suffix = $"###{Name}[History]";
        using var layout = BeginSettingsLayout("History", 145f);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.4f * padding * ImGui.GetTextLineHeight()));
        DrawLookupHistorySettings(suffix, layout);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.8f * ImGui.GetTextLineHeight()));
        DrawLookupHistoryShowcaseSettings(suffix, layout);
    }

    private void DrawLookupHistorySettings(string suffix, AlignedSettingsLayout.Scope layout)
    {
        ImGui.TextColored(Ui.ColourCyan, "Lookup history");

        using var table = BeginConfigTable("##BTTLookupHistory", layout);
        if (!table) return;

        SettingsRows.ManagedCheckbox(
            layout,
            "Enable",
            $"{suffix}LookupHistoryEnabled",
            ref _plugin.Config.LookupHistoryEnabled,
            onChanged: _ =>
            {
                _plugin.Config.Save();
                RefreshGeneralShowcase(force: true);
            });

        DrawLookupHistoryActionsRow(suffix, layout);

        SettingsRows.DefaultedInputInt(
            layout,
            "Max entries",
            $"{suffix}LookupHistoryLimit",
            ref _plugin.Config.LookupHistoryLimit,
            DefaultConfig.LookupHistoryLimit,
            managed: true,
            onChanged: () =>
            {
                _plugin.Config.LookupHistoryLimit = Math.Clamp(
                    _plugin.Config.LookupHistoryLimit,
                    BilingualTooltipsConfig.LookupHistoryLimitMin,
                    BilingualTooltipsConfig.LookupHistoryLimitMax);
                _plugin.LookupHistory.Prune(_plugin.Config.LookupHistoryLimit);
                _plugin.Config.Save();
            });

        layout.BeginRow("Sources", Ui.ColourWhiteDim);
        DrawLookupHistorySourceCheckbox(BttLookupHistorySource.Item, suffix);
        ImGui.SameLine();
        DrawLookupHistorySourceCheckbox(BttLookupHistorySource.Action, suffix);
        ImGui.SameLine();
        DrawLookupHistorySourceCheckbox(BttLookupHistorySource.Content, suffix);
        ImGui.SameLine();
        DrawLookupHistorySourceCheckbox(BttLookupHistorySource.NpcDialogue, suffix);

        SettingsRows.ShortcutControls(
            layout,
            "Enable shortcut",
            $"{suffix}LookupHistoryHotkeyEnabled",
            ref _plugin.Config.LookupHistoryHotkeyEnabled,
            _lookupHistoryHotkeyEditor,
            $"{suffix}LookupHistoryHotkey",
            ref _plugin.Config.LookupHistoryHotkey,
            "Toggles the lookup history window and refreshes the selected entry.",
            onChanged: MioConfig.Save);
    }

    private void DrawLookupHistoryActionsRow(string suffix, AlignedSettingsLayout.Scope layout)
    {
        layout.BeginRow("Actions", Ui.ColourWhiteDim);
        if (ImGui.Button($"Open history{suffix}OpenHistory"))
        {
            _plugin.LookupHistoryWindow.IsOpen = true;
        }

        ImGui.SameLine();
        if (ImGui.Button($"Clear history{suffix}ClearHistory"))
        {
            _plugin.LookupHistory.Clear();
            RefreshGeneralShowcase(force: true);
        }
    }

    private void DrawLookupHistoryShowcaseSettings(string suffix, AlignedSettingsLayout.Scope layout)
    {
        ImGui.TextColored(Ui.ColourCyan, "General showcase");

        using var table = BeginConfigTable("##BTTLookupHistoryShowcase", layout);
        if (!table) return;

        if (!_plugin.Config.LookupHistoryEnabled)
        {
            ImGui.BeginDisabled();
        }

        SettingsRows.DefaultedCheckbox(
            layout,
            "Show on General",
            $"{suffix}LookupHistoryShowcaseEnabled",
            ref _plugin.Config.LookupHistoryShowcaseEnabled,
            DefaultConfig.LookupHistoryShowcaseEnabled,
            onChanged: MioConfig.Save);

        if (!_plugin.Config.LookupHistoryEnabled)
        {
            ImGui.EndDisabled();
        }
    }

    private void DrawLookupHistorySourceCheckbox(BttLookupHistorySource source, string suffix)
    {
        var label = BttLookupDisplay.SourceLabel(source);
        var enabled = _plugin.Config.LookupHistorySources.HasFlag(source);
        var defaultEnabled = DefaultConfig.LookupHistorySources.HasFlag(source);
        if (!SettingsControls.DefaultedCheckbox($"{label}{suffix}LookupHistorySource{source}", ref enabled, defaultEnabled).ValueChanged) return;

        if (enabled)
        {
            _plugin.Config.LookupHistorySources |= source;
        }
        else
        {
            _plugin.Config.LookupHistorySources &= ~source;
        }

        _plugin.Config.Save();
    }
}
