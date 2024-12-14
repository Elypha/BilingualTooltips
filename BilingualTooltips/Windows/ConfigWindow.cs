using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using System.Numerics;
using Miosuke;
using Miosuke.UiHelper;
using System;
using Miosuke.Configuration;


namespace BilingualTooltips.Windows;

public class ConfigWindow : Window, IDisposable
{

    private BilingualTooltipsPlugin plugin;
    private readonly HotkeyUi temporary_enable_hotkey_helper;
    private readonly HotkeyUi multilingual_panel_hotkey_helper;

    public ConfigWindow(BilingualTooltipsPlugin plugin) : base(
        "BilingualTooltips Configuration"
    // ImGuiWindowFlags.NoResize |
    // ImGuiWindowFlags.NoCollapse |
    // ImGuiWindowFlags.NoScrollbar |
    // ImGuiWindowFlags.NoScrollWithMouse
    )
    {
        Size = new Vector2(400, 300);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
        temporary_enable_hotkey_helper = new HotkeyUi();
        multilingual_panel_hotkey_helper = new HotkeyUi();
    }


    public void Dispose()
    {
    }

    public override void OnOpen()
    {
    }


    public override void OnClose()
    {
        plugin.Config.Save();
    }


    public override void PreDraw()
    {
        if (plugin.Config.EnableTheme)
        {
            plugin.PluginTheme.Push();
            plugin.PluginThemeEnabled = true;
        }
    }

    public override void PostDraw()
    {
        if (plugin.PluginThemeEnabled)
        {
            plugin.PluginTheme.Pop();
            plugin.PluginThemeEnabled = false;
        }
    }

    public override void Draw()
    {
        float padding = 0.8f;
        string suffix;

        // ----------------- General -----------------
        // ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
        suffix = $"###{BilingualTooltipsPlugin.Name}[General]";
        ImGui.TextColored(Ui.ColourKhaki, "General");
        ImGui.Separator();

        // Enable
        var Enabled = plugin.Config.Enabled;
        if (ImGui.Checkbox($"Enable{suffix}Enable", ref Enabled))
        {
            plugin.ToggleEnabled(Enabled);
        }
        ImGuiComponents.HelpMarker(
            "The one switch to enable/disable this plugin."
        );

        // TemporaryEnableOnly
        ImGui.Text("┗");
        ImGui.SameLine();
        var TemporaryEnableOnly = plugin.Config.TemporaryEnableOnly;
        if (ImGui.Checkbox($"but only upon hotkey{suffix}TemporaryEnableOnly", ref TemporaryEnableOnly))
        {
            if (TemporaryEnableOnly)
            {
                plugin.TooltipHandler.ResetItemTooltip();
                plugin.TooltipHandler.ResetActionTooltip();
            }

            plugin.Config.TemporaryEnableOnly = TemporaryEnableOnly;
            plugin.Config.Save();
        }
        ImGui.SameLine();
        var TemporaryEnableHotkey = plugin.Config.TemporaryEnableHotkey;
        if (temporary_enable_hotkey_helper.DrawConfigUi("Hotkey", ref TemporaryEnableHotkey, 120))
        {
            plugin.Config.TemporaryEnableHotkey = TemporaryEnableHotkey;
            plugin.Config.Save();
        }

        // ----------------- Language -----------------
        DrawLanguageConfig(padding);


        // ----------------- UI -----------------
        DrawUiConfig(padding);

    }

    private void DrawLanguageConfig(float padding)
    {
        // setup
        float table_width = ImGui.GetWindowSize().X;
        float table_height = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y * 2;
        float col_name_width = ImGui.CalcTextSize("　Action name translation　").X + 2 * ImGui.GetStyle().ItemSpacing.X;
        float col_value_width = 150.0f;
        float col_value_content_width = 120.0f;
        var suffix = $"###{BilingualTooltipsPlugin.Name}[Language]";
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
        ImGui.TextColored(Ui.ColourAccentLightAlt, "Language");
        ImGui.Separator();

        // TABLE Item tooltip
        ImGui.TextColored(Ui.ColourCyan, "Item tooltip");
        ImGuiComponents.HelpMarker(
            "The language you want to display additionally on item tooltips."
        );

        ImGui.BeginChild("table DrawLanguageConfig Item tooltip", new Vector2(table_width, table_height * 2), false);
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, col_name_width);
        ImGui.SetColumnWidth(1, col_value_width);

        // LanguageItemTooltipName
        ImGui.TextColored(Ui.ColourWhiteDim, "　Name");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.BeginCombo($"{suffix}LanguageItemTooltipName", plugin.Config.LanguageItemTooltipName.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(GameLanguage)).Cast<GameLanguage>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.LanguageItemTooltipName))
                {
                    plugin.Config.LanguageItemTooltipName = type;
                    plugin.Config.Save();
                    if (type == GameLanguage.Off)
                    {
                        plugin.TooltipHandler.ResetItemTooltip();
                    }
                }
            }
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        // LanguageItemTooltipDescription
        ImGui.TextColored(Ui.ColourWhiteDim, "　Description");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.BeginCombo($"{suffix}LanguageItemTooltipDescription", plugin.Config.LanguageItemTooltipDescription.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(GameLanguage)).Cast<GameLanguage>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.LanguageItemTooltipDescription))
                {
                    plugin.Config.LanguageItemTooltipDescription = type;
                    plugin.Config.Save();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        ImGui.Columns(1);
        ImGui.EndChild();


        // TABLE Action tooltip
        ImGui.TextColored(Ui.ColourCyan, "Action tooltip");
        ImGuiComponents.HelpMarker(
            "The language you want to display additionally on actions, traits (passive skills) and general actions (sprint, etc.).\n" +
            "Note that translations are raw text extracted from the game, so if you see any weird/missing text, it's because of the original text contains expressions that are not currently supported by this plugin."
        );

        ImGui.BeginChild("table DrawLanguageConfig Action tooltip", new Vector2(table_width, table_height * 2), false);
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, col_name_width);
        ImGui.SetColumnWidth(1, col_value_width);

        // LanguageActionTooltipName
        ImGui.TextColored(Ui.ColourWhiteDim, "　Name");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.BeginCombo($"{suffix}LanguageActionTooltipName", plugin.Config.LanguageActionTooltipName.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(GameLanguage)).Cast<GameLanguage>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.LanguageActionTooltipName))
                {
                    plugin.Config.LanguageActionTooltipName = type;
                    plugin.Config.Save();
                    if (type == GameLanguage.Off)
                    {
                        plugin.TooltipHandler.ResetActionTooltip();
                    }
                }
            }
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        // LanguageActionTooltipDescription
        ImGui.TextColored(Ui.ColourWhiteDim, "　Description");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.BeginCombo($"{suffix}LanguageActionTooltipDescription", plugin.Config.LanguageActionTooltipDescription.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(GameLanguage)).Cast<GameLanguage>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.LanguageActionTooltipDescription))
                {
                    plugin.Config.LanguageActionTooltipDescription = type;
                    plugin.Config.Save();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        ImGui.Columns(1);
        ImGui.EndChild();


        // TABLE Action tooltip
        ImGui.TextColored(Ui.ColourCyan, "Multilingual panel");
        ImGuiComponents.HelpMarker(
            "A separate window where you can see multiple language variations of your choice in a configurable order at the same time."
        );

        // ItemTooltipPanelUpdateOnHotkey
        if (ImGui.Checkbox($"Update only on hotkey{suffix}ItemTooltipPanelUpdateOnHotkey", ref P.Config.ItemTooltipPanelUpdateOnHotkey))
        {
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Update the multilingual panel only when the hotkey is pressed.\n" +
            "Disable: Update the multilingual panel every time the item tooltip is updated."
        );

        ImGui.BeginChild("table DrawLanguageConfig Multilingual panel", new Vector2(table_width, table_height * 5), false);
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, col_name_width);
        ImGui.SetColumnWidth(1, col_value_width);

        // Hotkey
        ImGui.TextColored(Ui.ColourWhiteDim, "　Hotkey");
        ImGui.NextColumn();

        if (multilingual_panel_hotkey_helper.DrawConfigUi("ItemTooltipPanelHotkey", ref P.Config.ItemTooltipPanelHotkey, col_value_content_width))
        {
            plugin.Config.Save();
        }
        ImGui.NextColumn();

        // ItemTooltipPanelText1
        ImGui.TextColored(Ui.ColourWhiteDim, "　Language 1");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.BeginCombo($"{suffix}ItemTooltipPanelText1", plugin.Config.ItemTooltipPanelText1.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(GameLanguage)).Cast<GameLanguage>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.ItemTooltipPanelText1))
                {
                    plugin.Config.ItemTooltipPanelText1 = type;
                    plugin.Config.Save();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        // ItemTooltipPanelText2
        ImGui.TextColored(Ui.ColourWhiteDim, "　Language 2");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.BeginCombo($"{suffix}ItemTooltipPanelText2", plugin.Config.ItemTooltipPanelText2.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(GameLanguage)).Cast<GameLanguage>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.ItemTooltipPanelText2))
                {
                    plugin.Config.ItemTooltipPanelText2 = type;
                    plugin.Config.Save();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        // ItemTooltipPanelText3
        ImGui.TextColored(Ui.ColourWhiteDim, "　Language 3");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.BeginCombo($"{suffix}ItemTooltipPanelText3", plugin.Config.ItemTooltipPanelText3.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(GameLanguage)).Cast<GameLanguage>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.ItemTooltipPanelText3))
                {
                    plugin.Config.ItemTooltipPanelText3 = type;
                    plugin.Config.Save();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        // ItemTooltipPanelText4
        ImGui.TextColored(Ui.ColourWhiteDim, "　Language 4");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.BeginCombo($"{suffix}ItemTooltipPanelText4", plugin.Config.ItemTooltipPanelText4.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(GameLanguage)).Cast<GameLanguage>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.ItemTooltipPanelText4))
                {
                    plugin.Config.ItemTooltipPanelText4 = type;
                    plugin.Config.Save();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        ImGui.Columns(1);
        ImGui.EndChild();
    }


    private void DrawUiConfig(float padding)
    {
        // setup
        float table_width = ImGui.GetWindowSize().X;
        float table_height = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y * 2;
        float col_name_width = ImGui.CalcTextSize("　Action name translation　").X + 2 * ImGui.GetStyle().ItemSpacing.X;
        float col_value_width = 150.0f;
        float col_value_content_width = 120.0f;
        var suffix = $"###{BilingualTooltipsPlugin.Name}[UI]";
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
        ImGui.TextColored(Ui.ColourAccentLightAlt, "UI");
        ImGui.Separator();


        // EnableTheme
        var EnableTheme = plugin.Config.EnableTheme;
        if (ImGui.Checkbox($"Use bundled theme{suffix}EnableTheme", ref EnableTheme))
        {
            plugin.Config.EnableTheme = EnableTheme;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: Use a bundled theme (more compact and compatible) for this plugin.\n" +
            "Disable: Use your current Dalamud theme."
        );


        // TABLE Translation Colour
        ImGui.TextColored(Ui.ColourCyan, "Translation colour");
        ImGuiComponents.HelpMarker(
            "Set custom colour code of the translated text.\n" +
            "The colour code is a NUMBER, like 3 (default). You can find all colour codes in /xldata > UIColour > Row ID."
        );

        ImGui.BeginChild("table DrawUiConfig Translation colour", new Vector2(table_width, table_height * 4), false);
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, col_name_width);
        ImGui.SetColumnWidth(1, col_value_width);

        // ItemNameColourKey
        ImGui.TextColored(Ui.ColourWhiteDim, "　Item name");
        ImGui.NextColumn();
        var ItemNameColourKey = (int)plugin.Config.ItemNameColourKey;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}ItemNameColourKey", ref ItemNameColourKey))
        {
            plugin.Config.ItemNameColourKey = (ushort)ItemNameColourKey;
            plugin.Config.Save();
        }
        ImGui.NextColumn();

        // ItemDescriptionColourKey
        ImGui.TextColored(Ui.ColourWhiteDim, "　Item description");
        ImGui.NextColumn();
        var ItemDescriptionColourKey = (int)plugin.Config.ItemDescriptionColourKey;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}ItemDescriptionColourKey", ref ItemDescriptionColourKey))
        {
            plugin.Config.ItemDescriptionColourKey = (ushort)ItemDescriptionColourKey;
            plugin.Config.Save();
        }
        ImGui.NextColumn();


        // ActionNameColourKey
        ImGui.TextColored(Ui.ColourWhiteDim, "　Action name");
        ImGui.NextColumn();
        var ActionNameColourKey = (int)plugin.Config.ActionNameColourKey;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}ActionNameColourKey", ref ActionNameColourKey))
        {
            plugin.Config.ActionNameColourKey = (ushort)ActionNameColourKey;
            plugin.Config.Save();
        }
        ImGui.NextColumn();

        // ActionDescriptionColourKey
        ImGui.TextColored(Ui.ColourWhiteDim, "　Action description");
        ImGui.NextColumn();
        var ActionDescriptionColourKey = (int)plugin.Config.ActionDescriptionColourKey;
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputInt($"{suffix}ActionDescriptionColourKey", ref ActionDescriptionColourKey))
        {
            plugin.Config.ActionDescriptionColourKey = (ushort)ActionDescriptionColourKey;
            plugin.Config.Save();
        }
        ImGui.NextColumn();

        ImGui.Columns(1);
        ImGui.EndChild();



        ImGui.TextColored(Ui.ColourCyan, "Y offset");
        ImGuiComponents.HelpMarker(
            "Try a different offset to fit your favourite UI layout.\n" +
            "The updated position Y' = Y + offset.\n"
        );

        ImGui.BeginChild("table DrawUiConfig Y offset", new Vector2(table_width, table_height * 7), false);
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, col_name_width);
        ImGui.SetColumnWidth(1, col_value_width);

        // OffsetItemNameOriginal
        var OffsetItemNameOriginal = plugin.Config.OffsetItemNameOriginal;
        ImGui.TextColored(Ui.ColourWhiteDim, "　Item name");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}OffsetItemNameOriginal", ref OffsetItemNameOriginal))
        {
            plugin.Config.OffsetItemNameOriginal = OffsetItemNameOriginal;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("Default: 4.5");
        ImGui.NextColumn();

        // OffsetItemNameTranslation
        var OffsetItemNameTranslation = plugin.Config.OffsetItemNameTranslation;
        ImGui.TextColored(Ui.ColourWhiteDim, "　Item name translation");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}OffsetItemNameTranslation", ref OffsetItemNameTranslation))
        {
            plugin.Config.OffsetItemNameTranslation = OffsetItemNameTranslation;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("Default: 2.0");
        ImGui.NextColumn();

        // OffsetActionNameOriginal
        var OffsetActionNameOriginal = plugin.Config.OffsetActionNameOriginal;
        ImGui.TextColored(Ui.ColourWhiteDim, "　Action name");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}OffsetActionNameOriginal", ref OffsetActionNameOriginal))
        {
            plugin.Config.OffsetActionNameOriginal = OffsetActionNameOriginal;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("Default: -1.0");
        ImGui.NextColumn();

        // OffsetActionNameTranslation
        var OffsetActionNameTranslation = plugin.Config.OffsetActionNameTranslation;
        ImGui.TextColored(Ui.ColourWhiteDim, "　Action name translation");
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(col_value_content_width);
        if (ImGui.InputFloat($"{suffix}OffsetActionNameTranslation", ref OffsetActionNameTranslation))
        {
            plugin.Config.OffsetActionNameTranslation = OffsetActionNameTranslation;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker("Default: -8.5");
        ImGui.NextColumn();

        // // OffsetGlamName
        // var OffsetGlamName = plugin.Config.OffsetGlamName;
        // ImGui.TextColored(Ui.ColourWhiteDim, "　Glamour name");
        // ImGui.NextColumn();
        // ImGui.SetNextItemWidth(col_value_content_width);
        // if (ImGui.InputFloat2($"{suffix}OffsetGlamName", ref OffsetGlamName))
        // {
        //     plugin.Config.OffsetGlamName = OffsetGlamName;
        //     plugin.Config.Save();
        // }
        // ImGui.NextColumn();


        // // GlamNameFontSize
        // var GlamNameFontSize = plugin.Config.GlamNameFontSize;
        // ImGui.TextColored(Ui.ColourWhiteDim, "　Glamour name font size");
        // ImGui.NextColumn();
        // ImGui.SetNextItemWidth(col_value_content_width);
        // if (ImGui.InputFloat($"{suffix}GlamNameFontSize", ref GlamNameFontSize))
        // {
        //     plugin.Config.GlamNameFontSize = GlamNameFontSize;
        //     plugin.Config.Save();
        // }


        ImGui.Columns(1);
        ImGui.EndChild();
    }

}
