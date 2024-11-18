using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Style;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using Lumina.Excel.Sheets;
using Lumina.Excel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Components;
using Dalamud.Interface;
using ImGuiNET;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System;


namespace BilingualTooltips;

public class MainWindow : Window, IDisposable
{
    // private IDalamudTextureWrap goatImage;
    private BilingualTooltipsPlugin plugin;

    public MainWindow(BilingualTooltipsPlugin plugin) : base(
        "BilingualTooltips",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(720, 720);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void OnClose()
    {
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
        ImGui.Text("Thanks for being interested in testing BilingualTooltips.");
        ImGui.Text("Please let me know if you have any questions or suggestions via:");
        ImGui.Text("- Dalamud Discord > plugin-help-forum");
        ImGui.Text("- Discord PM: elypha");
    }
}
