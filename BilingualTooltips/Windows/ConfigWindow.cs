using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System;


namespace BilingualTooltips;

public class ConfigWindow : Window, IDisposable
{

    private Plugin plugin;

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


        // ----------------- Language -----------------
        // ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
        suffix = $"###{plugin.Name}[Language]";
        ImGui.TextColored(Miosuke.UI.ColourKhaki, "Language");
        ImGui.Separator();

        // TooltipLanguage
        ImGui.Text("Tooltip");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if (ImGui.BeginCombo($"{suffix}TooltipLanguage", plugin.Config.TooltipLanguage.ToString()))
        {
            foreach (var type in Enum.GetValues(typeof(GameLanguage)).Cast<GameLanguage>())
            {
                if (ImGui.Selectable(type.ToString(), type == plugin.Config.TooltipLanguage))
                {
                    plugin.Config.TooltipLanguage = type;
                    plugin.Config.Save();
                }
            }

            ImGui.EndCombo();
        }
        ImGuiComponents.HelpMarker(
            "The language you want to display additionally in the tooltip."
        );


        // ----------------- Tooltips -----------------
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (padding * ImGui.GetTextLineHeight()));
        suffix = $"###{plugin.Name}[TooltipColour]";
        ImGui.TextColored(Miosuke.UI.ColourKhaki, "Tooltip Colour");
        ImGui.Separator();


        // ItemNameColourKey
        ImGui.Text("Item name");
        ImGui.SameLine();
        var ItemNameColourKey = (int)plugin.Config.ItemNameColourKey;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"{suffix}ItemNameColourKey", ref ItemNameColourKey))
        {
            plugin.Config.ItemNameColourKey = (ushort)ItemNameColourKey;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "This is the colour CODE of the translated item name in the tooltip. It's a number, like 3.\n" +
            "You can find the colour code in /xldata -> UIColour -> Row ID."
            );

        // ItemDescriptionColourKey
        ImGui.Text("Item description");
        ImGui.SameLine();
        var ItemDescriptionColourKey = (int)plugin.Config.ItemDescriptionColourKey;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"{suffix}ItemDescriptionColourKey", ref ItemDescriptionColourKey))
        {
            plugin.Config.ItemDescriptionColourKey = (ushort)ItemDescriptionColourKey;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Same as above, but for the item description."
            );


        // ActionNameColourKey
        ImGui.Text("(WIP)Action name");
        ImGui.SameLine();
        var ActionNameColourKey = (int)plugin.Config.ActionNameColourKey;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"{suffix}ActionNameColourKey", ref ActionNameColourKey))
        {
            plugin.Config.ActionNameColourKey = (ushort)ActionNameColourKey;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Same as above, but for the action name."
            );

        // ActionDescriptionColourKey
        ImGui.Text("(WIP)Action description");
        ImGui.SameLine();
        var ActionDescriptionColourKey = (int)plugin.Config.ActionDescriptionColourKey;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"{suffix}ActionDescriptionColourKey", ref ActionDescriptionColourKey))
        {
            plugin.Config.ActionDescriptionColourKey = (ushort)ActionDescriptionColourKey;
            plugin.Config.Save();
        }
        ImGuiComponents.HelpMarker(
            "Same as above, but for the action description."
            );


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
    }
}
