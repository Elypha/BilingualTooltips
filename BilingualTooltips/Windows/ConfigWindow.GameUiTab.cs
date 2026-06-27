using Dalamud.Interface.Components;
using Miosuke.Configuration;
using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public partial class ConfigWindow
{
    private void DrawGameUiTab(float padding)
    {
        var suffix = $"###{Name}[GameUI]";
        using var layout = BeginSettingsLayout("GameUI", 150f);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.4f * padding * ImGui.GetTextLineHeight()));
        DrawGameUiTextSettings(suffix, layout);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.8f * ImGui.GetTextLineHeight()));
        DrawGameUiAppearanceSettings(suffix, layout);
    }

    private void DrawGameUiTextSettings(string suffix, AlignedSettingsLayout.Scope layout)
    {
        ImGui.TextColored(Ui.ColourCyan, "Duty finder names");
        ImGuiComponents.HelpMarker("Additional language shown in duty finder and related duty windows.");

        {
            using var table = BeginConfigTable("##BTTDutyFinderText", layout);
            if (!table) return;

            DrawGameTextLanguageRow(
                layout,
                "Name",
                $"{suffix}GameUiNameLanguage",
                _plugin.Config.GameUiNameLanguage,
                value =>
                {
                    _plugin.Config.GameUiNameLanguage = value;
                    _plugin.Config.Save();
                    if (value == BttLanguage.Off)
                    {
                        _plugin.ContentsHandler.ReleaseJournalDetail();
                        _plugin.ContentsHandler.ReleaseContentsFinderConfirm();
                    }
                },
                managed: true);

            DrawGameTextLanguageRow(
                layout,
                "Description",
                $"{suffix}GameUiDescriptionLanguage",
                _plugin.Config.GameUiDescriptionLanguage,
                value =>
                {
                    _plugin.Config.GameUiDescriptionLanguage = value;
                    _plugin.Config.Save();
                },
                managed: true);
            ImGuiComponents.HelpMarker("WIP; low priority.");
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.6f * ImGui.GetTextLineHeight()));
        ImGui.TextColored(Ui.ColourCyan, "Cosmic mission names");
        ImGuiComponents.HelpMarker("Additional language shown in Cosmic Exploration mission windows.");

        {
            using var table = BeginConfigTable("##BTTCosmicMissionText", layout);
            if (!table) return;

            DrawGameTextLanguageRow(
                layout,
                "Name",
                $"{suffix}CosmicMissionNameLanguage",
                _plugin.Config.CosmicMissionNameLanguage,
                value =>
                {
                    _plugin.Config.CosmicMissionNameLanguage = value;
                    _plugin.Config.Save();
                    if (value == BttLanguage.Off)
                    {
                        _plugin.CosmicMissionsHandler.ReleaseAllCurrent();
                    }
                },
                managed: true);
        }
    }

    private void DrawGameUiAppearanceSettings(string suffix, AlignedSettingsLayout.Scope layout)
    {
        ImGui.TextColored(Ui.ColourCyan, "Duty finder name appearance");

        {
            using var table = BeginConfigTable("##BTTDutyFinderAppearance", layout);
            if (!table) return;

            SettingsRows.DefaultedInputInt(
                layout,
                "Name colour",
                $"{suffix}GameUiNameColourKey",
                ref _plugin.Config.GameUiNameColourKey,
                DefaultConfig.GameUiNameColourKey,
                onChanged: MioConfig.Save);
            SettingsControls.DrawUiColourKeyHelpMarker();
            SettingsRows.DefaultedInputInt(
                layout,
                "Description colour",
                $"{suffix}GameUiDescriptionColourKey",
                ref _plugin.Config.GameUiDescriptionColourKey,
                DefaultConfig.GameUiDescriptionColourKey,
                onChanged: MioConfig.Save);
            SettingsControls.DrawUiColourKeyHelpMarker();
            SettingsRows.DefaultedInputFloat(
                layout,
                "Native name Y",
                $"{suffix}OffsetGameUiNameNative",
                ref _plugin.Config.OffsetGameUiNameNative,
                DefaultConfig.OffsetGameUiNameNative,
                onChanged: MioConfig.Save);
            SettingsRows.DefaultedInputFloat(
                layout,
                "Translation Y",
                $"{suffix}OffsetGameUiNameTranslation",
                ref _plugin.Config.OffsetGameUiNameTranslation,
                DefaultConfig.OffsetGameUiNameTranslation,
                onChanged: MioConfig.Save);
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.6f * ImGui.GetTextLineHeight()));
        ImGui.TextColored(Ui.ColourCyan, "Cosmic mission name appearance");

        {
            using var table = BeginConfigTable("##BTTCosmicMissionAppearance", layout);
            if (!table) return;

            SettingsRows.DefaultedInputInt(
                layout,
                "Name colour",
                $"{suffix}CosmicMissionNameColourKey",
                ref _plugin.Config.CosmicMissionNameColourKey,
                DefaultConfig.CosmicMissionNameColourKey,
                onChanged: MioConfig.Save);
            SettingsControls.DrawUiColourKeyHelpMarker();
            SettingsRows.DefaultedInputFloat(
                layout,
                "Native name Y",
                $"{suffix}OffsetCosmicMissionNameNative",
                ref _plugin.Config.OffsetCosmicMissionNameNative,
                DefaultConfig.OffsetCosmicMissionNameNative,
                onChanged: MioConfig.Save);
            SettingsRows.DefaultedInputFloat(
                layout,
                "Translation Y",
                $"{suffix}OffsetCosmicMissionNameTranslation",
                ref _plugin.Config.OffsetCosmicMissionNameTranslation,
                DefaultConfig.OffsetCosmicMissionNameTranslation,
                onChanged: MioConfig.Save);
        }
    }
}
