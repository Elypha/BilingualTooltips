using Dalamud.Interface.Components;
using Miosuke.Configuration;
using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public partial class ConfigWindow
{
    private void DrawGeneralTab(float padding)
    {
        var suffix = $"###{Name}[General]";
        using var layout = BeginSettingsLayout("General", 120f);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.4f * padding * ImGui.GetTextLineHeight()));
        DrawGeneralShowcase(suffix);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.8f * ImGui.GetTextLineHeight()));
        ImGui.TextColored(Ui.ColourCyan, "Appearance");

        var enableTheme = _plugin.Config.EnableTheme;
        using var table = BeginConfigTable("##BTTGeneralAppearance", layout);
        if (table)
        {
            SettingsRows.ManagedCheckbox(
                layout,
                "Use custom theme",
                $"{suffix}EnableTheme",
                ref enableTheme,
                onChanged: value =>
                {
                    _plugin.Config.EnableTheme = value;
                    _plugin.Config.Save();
                    if (value)
                    {
                        ImGuiThemeLoadCustomOrDefault();
                    }
                });
            ImGuiComponents.HelpMarker(
                "Enabled: all BilingualTooltips windows use the theme override below. If the override is empty, the bundled plugin theme is used instead.\n" +
                "Disabled: windows use your current Dalamud theme.");

            DrawCustomThemeRow(layout, suffix);
        }
    }

    private void DrawCustomThemeRow(AlignedSettingsLayout.Scope layout, string suffix)
    {
        layout.BeginRow("Theme override string", Ui.ColourWhiteDim);

        var theme = _plugin.Config.CustomTheme;
        var hadOverride = !string.Equals(theme, DefaultConfig.CustomTheme, StringComparison.Ordinal);
        var result = SettingsControls.DefaultedInputText(
            $"{suffix}CustomTheme",
            ref theme,
            DefaultConfig.CustomTheme,
            65536,
            "Bundled plugin theme is used",
            -1,
            managed: true);
        if (result.ValueChanged)
        {
            _plugin.Config.CustomTheme = theme;
            _plugin.Config.Save();
        }

        if (result.DeactivatedAfterEdit
            || (result.ValueChanged && hadOverride && string.Equals(theme, DefaultConfig.CustomTheme, StringComparison.Ordinal)))
        {
            ImGuiThemeLoadCustomOrDefault();
        }

        ImGuiComponents.HelpMarker(
            "Optional. Paste a Miosuke theme string here.\n" +
            "Leave empty to use the bundled plugin theme.");
    }
}
