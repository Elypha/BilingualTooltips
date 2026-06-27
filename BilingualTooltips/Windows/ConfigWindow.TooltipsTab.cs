using BilingualTooltips.Modules;
using Dalamud.Interface.Components;
using Miosuke.Configuration;
using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public partial class ConfigWindow
{
    private readonly HotkeyEditor _tooltipShortcutHotkeyEditor = new();

    private void DrawTooltipsTab(float padding)
    {
        var suffix = $"###{Name}[Tooltips]";
        using var layout = BeginSettingsLayout("Tooltips", 170f);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.4f * padding * ImGui.GetTextLineHeight()));
        DrawItemTooltipSettings(suffix, layout);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.8f * ImGui.GetTextLineHeight()));
        DrawActionTooltipSettings(suffix, layout);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.8f * ImGui.GetTextLineHeight()));
        DrawTooltipBehaviourSettings(suffix, layout);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.8f * ImGui.GetTextLineHeight()));
        DrawTooltipAppearanceSettings(suffix, layout);
    }

    private void DrawTooltipBehaviourSettings(string suffix, AlignedSettingsLayout.Scope layout)
    {
        ImGui.TextColored(Ui.ColourCyan, "Behaviour");

        using var table = BeginConfigTable("##BTTTooltipBehaviour", layout);
        if (!table) return;

        SettingsRows.ShortcutControls(
            layout,
            "Enable shortcut",
            $"{suffix}TooltipShortcutEnabled",
            ref _plugin.Config.TooltipShortcutEnabled,
            _tooltipShortcutHotkeyEditor,
            $"{suffix}TooltipShortcutHotkey",
            ref _plugin.Config.TooltipShortcutHotkey,
            "When enabled, tooltip translations are shown only while this shortcut is held.",
            onChanged: () =>
            {
                if (_plugin.Config.TooltipShortcutEnabled)
                {
                    ResetTooltipTextNodes();
                }

                _plugin.Config.Save();
            });
    }

    private void ResetTooltipTextNodes()
    {
        _plugin.TooltipHandler.ReleaseAllCurrent();
    }

    private void DrawItemTooltipSettings(string suffix, AlignedSettingsLayout.Scope layout)
    {
        ImGui.TextColored(Ui.ColourCyan, "Item tooltip");
        ImGuiComponents.HelpMarker("Additional language shown on item tooltips.");

        using var table = BeginConfigTable("##BTTItemTooltip", layout);
        if (!table) return;

        DrawGameTextLanguageRow(
            layout,
            "Name",
            $"{suffix}LanguageItemTooltipName",
            _plugin.Config.LanguageItemTooltipName,
            value =>
            {
                _plugin.Config.LanguageItemTooltipName = value;
                _plugin.Config.Save();
                if (value == BttLanguage.Off)
                {
                    _plugin.TooltipHandler.ReleaseCurrent(TooltipDetailAddon.ItemDetail, TooltipDetailParts.Name);
                }
            },
            managed: true);

        DrawGameTextLanguageRow(
            layout,
            "Description",
            $"{suffix}LanguageItemTooltipDescription",
            _plugin.Config.LanguageItemTooltipDescription,
            value =>
            {
                _plugin.Config.LanguageItemTooltipDescription = value;
                _plugin.Config.Save();
                if (value == BttLanguage.Off)
                {
                    _plugin.TooltipHandler.ReleaseCurrent(TooltipDetailAddon.ItemDetail, TooltipDetailParts.Description);
                }
            },
            managed: true);
    }

    private void DrawActionTooltipSettings(string suffix, AlignedSettingsLayout.Scope layout)
    {
        ImGui.TextColored(Ui.ColourCyan, "Action tooltip");
        ImGuiComponents.HelpMarker(
            "Additional language shown on actions, traits, general actions, minions, and mounts.\n" +
            "Translations are raw game text; unsupported expressions can still produce odd or missing text.");

        using var table = BeginConfigTable("##BTTActionTooltip", layout);
        if (!table) return;

        DrawGameTextLanguageRow(
            layout,
            "Name",
            $"{suffix}LanguageActionTooltipName",
            _plugin.Config.LanguageActionTooltipName,
            value =>
            {
                _plugin.Config.LanguageActionTooltipName = value;
                _plugin.Config.Save();
                if (value == BttLanguage.Off)
                {
                    _plugin.TooltipHandler.ReleaseCurrent(TooltipDetailAddon.ActionDetail, TooltipDetailParts.Name);
                }
            },
            managed: true);

        DrawGameTextLanguageRow(
            layout,
            "Description",
            $"{suffix}LanguageActionTooltipDescription",
            _plugin.Config.LanguageActionTooltipDescription,
            value =>
            {
                _plugin.Config.LanguageActionTooltipDescription = value;
                _plugin.Config.Save();
                if (value == BttLanguage.Off)
                {
                    _plugin.TooltipHandler.ReleaseCurrent(TooltipDetailAddon.ActionDetail, TooltipDetailParts.Description);
                }
            },
            managed: true);
    }

    private void DrawTooltipAppearanceSettings(string suffix, AlignedSettingsLayout.Scope layout)
    {
        ImGui.TextColored(Ui.ColourCyan, "Tooltip appearance");

        using var table = BeginConfigTable("##BTTTooltipAppearance", layout);
        if (!table) return;

        SettingsRows.DefaultedInputInt(
            layout,
            "Item name colour",
            $"{suffix}ItemNameColourKey",
            ref _plugin.Config.ItemNameColourKey,
            DefaultConfig.ItemNameColourKey,
            onChanged: MioConfig.Save);
        SettingsControls.DrawUiColourKeyHelpMarker();
        SettingsRows.DefaultedInputInt(
            layout,
            "Item description colour",
            $"{suffix}ItemDescriptionColourKey",
            ref _plugin.Config.ItemDescriptionColourKey,
            DefaultConfig.ItemDescriptionColourKey,
            onChanged: MioConfig.Save);
        SettingsControls.DrawUiColourKeyHelpMarker();
        SettingsRows.DefaultedInputInt(
            layout,
            "Action name colour",
            $"{suffix}ActionNameColourKey",
            ref _plugin.Config.ActionNameColourKey,
            DefaultConfig.ActionNameColourKey,
            onChanged: MioConfig.Save);
        SettingsControls.DrawUiColourKeyHelpMarker();
        SettingsRows.DefaultedInputInt(
            layout,
            "Action description colour",
            $"{suffix}ActionDescriptionColourKey",
            ref _plugin.Config.ActionDescriptionColourKey,
            DefaultConfig.ActionDescriptionColourKey,
            onChanged: MioConfig.Save);
        SettingsControls.DrawUiColourKeyHelpMarker();
        SettingsRows.DefaultedInputFloat(
            layout,
            "Item native name Y",
            $"{suffix}OffsetItemNameNative",
            ref _plugin.Config.OffsetItemNameNative,
            DefaultConfig.OffsetItemNameNative,
            onChanged: MioConfig.Save);
        SettingsRows.DefaultedInputFloat(
            layout,
            "Item translation Y",
            $"{suffix}OffsetItemNameTranslation",
            ref _plugin.Config.OffsetItemNameTranslation,
            DefaultConfig.OffsetItemNameTranslation,
            onChanged: MioConfig.Save);
        SettingsRows.DefaultedInputFloat(
            layout,
            "Action native name Y",
            $"{suffix}OffsetActionNameNative",
            ref _plugin.Config.OffsetActionNameNative,
            DefaultConfig.OffsetActionNameNative,
            onChanged: MioConfig.Save);
        SettingsRows.DefaultedInputFloat(
            layout,
            "Action translation Y",
            $"{suffix}OffsetActionNameTranslation",
            ref _plugin.Config.OffsetActionNameTranslation,
            DefaultConfig.OffsetActionNameTranslation,
            onChanged: MioConfig.Save);
        SettingsRows.DefaultedInputUShort(
            layout,
            "Name max width",
            $"{suffix}TooltipNameMaxLineWidth",
            ref _plugin.Config.TooltipNameMaxLineWidth,
            DefaultConfig.TooltipNameMaxLineWidth,
            onChanged: MioConfig.Save);
        ImGuiComponents.HelpMarker(
            "Maximum width in pixels for item, action, minion, and mount names before horizontal compression.");
    }
}
