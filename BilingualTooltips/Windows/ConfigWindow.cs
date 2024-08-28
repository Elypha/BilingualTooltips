using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Miosuke;
using System;


namespace BilingualTooltips;

public class ConfigWindow : Window, IDisposable
{

    private Plugin plugin;
    private readonly HotkeyUi temporary_enable_hotkey_helper;

    public ConfigWindow(Plugin plugin) : base(
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
        suffix = $"###{plugin.Name}[General]";
        ImGui.TextColored(Miosuke.UI.ColourKhaki, "General");
        ImGui.Separator();

        // Enabled
        var Enabled = plugin.Config.Enabled;
        if (ImGui.Checkbox($"Enabled{suffix}Enabled", ref Enabled))
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
        if (ImGui.Checkbox($"(Testing) but only upon hotkey{suffix}TemporaryEnableOnly", ref TemporaryEnableOnly))
        {
            if (TemporaryEnableOnly) plugin.TooltipHandler.Reset();
            plugin.Config.TemporaryEnableOnly = TemporaryEnableOnly;
            plugin.Config.Save();
        }
        ImGui.SameLine();
        var TemporaryEnableHotkey = plugin.Config.TemporaryEnableHotkey;
        if (temporary_enable_hotkey_helper.DrawConfigUi("Hotkey", ref TemporaryEnableHotkey, 100))
        {
            plugin.Config.TemporaryEnableHotkey = TemporaryEnableHotkey;
            plugin.Config.Save();
        }

        // ----------------- Language -----------------
        DrawLanguageSelector(padding);


        // ----------------- Tooltips -----------------
        DrawColourConfig(padding);

        // ----------------- UI -----------------
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
        suffix = $"###{plugin.Name}[UI]";
        ImGui.TextColored(Miosuke.UI.ColourKhaki, "UI");
        ImGui.Separator();


        // EnableTheme
        var EnableTheme = plugin.Config.EnableTheme;
        if (ImGui.Checkbox($"Bundled theme{suffix}EnableTheme", ref EnableTheme))
        {
            plugin.Config.EnableTheme = EnableTheme;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Enable: A bundled theme will apply to this plugin so that it can be more compact and compatible.\n" +
            "Disable: Your own (default) dalamud theme will be used."
        );

        // OffsetItemNameOriginal
        var OffsetItemNameOriginal = plugin.Config.OffsetItemNameOriginal;
        ImGui.Text("Offset item name original");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputFloat($"{suffix}OffsetItemNameOriginal", ref OffsetItemNameOriginal))
        {
            plugin.Config.OffsetItemNameOriginal = OffsetItemNameOriginal;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "The new Y position = Y.original + offset. Adjust this to fit your custom UI layout."
        );

        // OffsetItemNameTranslation
        var OffsetItemNameTranslation = plugin.Config.OffsetItemNameTranslation;
        ImGui.Text("Offset item name translation");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputFloat($"{suffix}OffsetItemNameTranslation", ref OffsetItemNameTranslation))
        {
            plugin.Config.OffsetItemNameTranslation = OffsetItemNameTranslation;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "The new Y position = Y.original + offset. Adjust this to fit your custom UI layout."
        );
    }

    private void DrawLanguageSelector(float padding)
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
        var suffix = $"###{plugin.Name}[Language]";
        ImGui.TextColored(Miosuke.UI.ColourKhaki, "Language");
        ImGui.Separator();

        var table_width = ImGui.GetWindowSize().X;
        var table_height = ImGui.GetTextLineHeightWithSpacing() * 4 + ImGui.GetStyle().ItemSpacing.Y * 8;
        ImGui.BeginChild("table DrawLanguageSelector", new Vector2(table_width, table_height), false);
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, ImGui.CalcTextSize("Action Tooltip Description xx").X + 2 * ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetColumnWidth(1, 150.0f);

        ImGui.Text("Item tooltip name");
        ImGui.NextColumn();

        ImGui.SetNextItemWidth(120);
        if (ImGui.BeginCombo($"{suffix}LanguageItemTooltipName", plugin.Config.LanguageItemTooltipName.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(GameLanguage)).Cast<GameLanguage>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.LanguageItemTooltipName))
                {
                    if (type == GameLanguage.Off) plugin.TooltipHandler.Reset();
                    plugin.Config.LanguageItemTooltipName = type;
                    plugin.Config.Save();
                }
            }
            ImGui.EndCombo();
        }
        ImGuiComponents.HelpMarker(
            "The language you want to display additionally for item name."
        );
        ImGui.NextColumn();

        ImGui.Text("Item tooltip description");
        ImGui.NextColumn();

        ImGui.SetNextItemWidth(120);
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
        ImGuiComponents.HelpMarker(
            "The language you want to display additionally for item description."
        );
        ImGui.NextColumn();

        ImGui.Text("Action tooltip name");
        ImGui.NextColumn();

        ImGui.SetNextItemWidth(120);
        if (ImGui.BeginCombo($"{suffix}LanguageActionTooltipName", plugin.Config.LanguageActionTooltipName.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(GameLanguage)).Cast<GameLanguage>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.LanguageActionTooltipName))
                {
                    plugin.Config.LanguageActionTooltipName = type;
                    plugin.Config.Save();
                }
            }
            ImGui.EndCombo();
        }
        ImGuiComponents.HelpMarker(
            "(WIP) The language you want to display additionally for action name."
        );
        ImGui.NextColumn();

        ImGui.Text("Action tooltip description");
        ImGui.NextColumn();

        ImGui.SetNextItemWidth(120);
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
        ImGuiComponents.HelpMarker(
            "(WIP) The language you want to display additionally for action description."
        );
        ImGui.NextColumn();

        ImGui.Columns(1);
        ImGui.EndChild();

    }

    private void DrawColourConfig(float padding)
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
        var suffix = $"###{plugin.Name}[TooltipColour]";
        ImGui.TextColored(Miosuke.UI.ColourKhaki, "Tooltip Colour");
        ImGui.Separator();


        var table_width = ImGui.GetWindowSize().X;
        var table_height = ImGui.GetTextLineHeightWithSpacing() * 4 + ImGui.GetStyle().ItemSpacing.Y * 8;
        ImGui.BeginChild("table DrawColourConfig", new Vector2(table_width, table_height), false);
        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, ImGui.CalcTextSize("Action Tooltip Description xx").X + 2 * ImGui.GetStyle().ItemSpacing.X);
        ImGui.SetColumnWidth(1, 150.0f);

        // ItemNameColourKey
        ImGui.Text("Item name");
        ImGui.NextColumn();
        var ItemNameColourKey = (int)plugin.Config.ItemNameColourKey;
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt($"{suffix}ItemNameColourKey", ref ItemNameColourKey))
        {
            plugin.Config.ItemNameColourKey = (ushort)ItemNameColourKey;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "This is the colour CODE of the translated item name in the tooltip. It's a number, like 3.\n" +
            "You can find the colour code in /xldata -> UIColour -> Row ID."
            );
        ImGui.NextColumn();

        // ItemDescriptionColourKey
        ImGui.Text("Item description");
        ImGui.NextColumn();
        var ItemDescriptionColourKey = (int)plugin.Config.ItemDescriptionColourKey;
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt($"{suffix}ItemDescriptionColourKey", ref ItemDescriptionColourKey))
        {
            plugin.Config.ItemDescriptionColourKey = (ushort)ItemDescriptionColourKey;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Same as above, but for the item description."
            );
        ImGui.NextColumn();


        // ActionNameColourKey
        ImGui.Text("(WIP)Action name");
        ImGui.NextColumn();
        var ActionNameColourKey = (int)plugin.Config.ActionNameColourKey;
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt($"{suffix}ActionNameColourKey", ref ActionNameColourKey))
        {
            plugin.Config.ActionNameColourKey = (ushort)ActionNameColourKey;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Same as above, but for the action name."
            );
        ImGui.NextColumn();

        // ActionDescriptionColourKey
        ImGui.Text("(WIP)Action description");
        ImGui.NextColumn();
        var ActionDescriptionColourKey = (int)plugin.Config.ActionDescriptionColourKey;
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt($"{suffix}ActionDescriptionColourKey", ref ActionDescriptionColourKey))
        {
            plugin.Config.ActionDescriptionColourKey = (ushort)ActionDescriptionColourKey;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Same as above, but for the action description."
            );
        ImGui.NextColumn();

        ImGui.Columns(1);
        ImGui.EndChild();

    }

}
