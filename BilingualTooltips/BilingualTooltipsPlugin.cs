#pragma warning disable CS8618

using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Style;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using Lumina.Excel.Sheets;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Miosuke.Action;
using Miosuke.Messages;
using Miosuke.Configuration;
using Miosuke;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BilingualTooltips.Configuration;
using BilingualTooltips.Assets;
using BilingualTooltips.Windows;
using BilingualTooltips.Modules;


namespace BilingualTooltips;

public sealed partial class BilingualTooltipsPlugin : IDalamudPlugin
{
    public static string Name => "BilingualTooltips";
    public static string NameShort => "BTT";
    private const string CommandMainWindow = "/btt";

    // PLUGIN
    internal static BilingualTooltipsPlugin P;
    internal BilingualTooltipsConfig Config;
    public StyleModel PluginTheme { get; set; }
    public bool PluginThemeEnabled { get; set; }
    public Dalamud.Game.ClientLanguage ClientLanguage;

    // MODULES
    public TooltipHandler TooltipHandler { get; set; } = null!;
    public ContentHandler ContentHandler { get; set; } = null!;

    // WINDOWS
    public ConfigWindow ConfigWindow { get; init; }
    public MainWindow MainWindow { get; init; }
    public MultilingualPanel ItemTooltipPanel { get; init; }
    public WindowSystem WindowSystem = new("BilingualTooltips");





    public BilingualTooltipsPlugin(IDalamudPluginInterface pluginInterface)
    {
        // PLUGIN

        // dalamud service
        Service.Init(pluginInterface);
        MiosukeHelper.Init(
            pluginInterface,
            this,
            $"[{NameShort}] ",
            null
        );


        // PLUGIN

        // plugin init
        P = this;
        // config init
        MioConfig.Setup(MainConfigFileName: "main.json");
        if (Service.PluginInterface.ConfigFile.Exists) MioConfig.Migrate<BilingualTooltipsConfig>(Service.PluginInterface.ConfigFile.FullName);
        Config = MioConfig.Init<BilingualTooltipsConfig>();
        ClientLanguage = Service.ClientState.ClientLanguage;

        // theme
        ImGuiThemeLoadCustomOrDefault();

        // command handlers
        Service.Commands.AddHandler(CommandMainWindow, new CommandInfo(OnCommandMainWindow)
        {
            HelpMessage = "main command entry:\n" +
                "└ /btt → open the main window.\n" +
                "└ /btt c|config → open the configuration window.\n" +
                "└ /btt enabled → toggle if plugin is enabled.\n" +
                "└ /btt enabled true|false → set if plugin is enabled.\n" +
                "└ /btt ml → the same function as the hotkey but in text command form."
        });


        // MODULES

        TooltipHandler = new TooltipHandler(this);
        TooltipHandler.StartHook();
        ContentHandler = new ContentHandler(this);
        ContentHandler.StartHook();


        // WINDOWS

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        ItemTooltipPanel = new MultilingualPanel(this);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ItemTooltipPanel);


        // HANDLERS

        Service.PluginInterface.UiBuilder.Draw += DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
        Service.Framework.Update += OnFrameUpdate;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        // unload command handlers
        Service.Commands.RemoveHandler(CommandMainWindow);

        // unload modules
        TooltipHandler.StopHook();
        TooltipHandler.Dispose();
        ContentHandler.StopHook();
        ContentHandler.Dispose();

        // unload windows
        ConfigWindow.Dispose();
        MainWindow.Dispose();
        ItemTooltipPanel.Dispose();

        // unload event handlers
        Service.PluginInterface.UiBuilder.Draw -= DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi -= DrawMainUI;
        Service.Framework.Update -= OnFrameUpdate;
    }

    public void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void DrawMainUI()
    {
        MainWindow.Toggle();
    }

    public void DrawConfigUI()
    {
        ConfigWindow.Toggle();
    }

    public static void ImGuiThemeLoadCustomOrDefault()
    {
        try
        {
            if (P.Config.CustomTheme != "")
            {
                var _theme = StyleModel.Deserialize(P.Config.CustomTheme);
                if (_theme is not null) P.PluginTheme = _theme;
                return;
            }
        }
        catch (Exception e)
        {
            P.Config.CustomTheme = "";
            P.Config.Save();
            Notice.Error($"Your custom theme is invalid and has been reset: {e.Message}");
        }
        finally
        {
            P.PluginTheme = Data.defaultTheme;
        }
    }

    public void OnCommandMainWindow(string command, string args)
    {
        switch (args)
        {
            case "":
                MainWindow.Toggle();
                break;
            case "config" or "c":
                ConfigWindow.Toggle();
                break;
            case string arg1 when arg1.StartsWith("enabled"):
                var arg2 = args[7..].Trim();
                if (bool.TryParse(arg2, out var result))
                {
                    ToggleEnabled(result);
                }
                else
                {
                    ToggleEnabled();
                }
                break;
            case "ml":
                ItemTooltipPanel.OnHotkeyTriggered();
                break;
            default:
                Notice.Error("Invalid command argument.");
                break;
        }
    }

    public void OnFrameUpdate(IFramework framework)
    {
        if (!P.ItemTooltipPanel.IsIdle && !Hotkey.IsActive(Config.ItemTooltipPanelHotkey)) P.ItemTooltipPanel.IsIdle = true;
        if (Hotkey.IsActive(Config.ItemTooltipPanelHotkey) && P.ItemTooltipPanel.IsIdle)
        {
            P.ItemTooltipPanel.OnHotkeyTriggered();
            P.ItemTooltipPanel.IsIdle = false;
        }

    }

    public void ToggleEnabled(bool? target = null)
    {
        if (target == null)
        {
            if (Config.Enabled)
            {
                TooltipHandler.ResetItemTooltip();
                TooltipHandler.ResetActionTooltip();
            }
            Config.Enabled = !Config.Enabled;
        }
        else
        {
            if (Config.Enabled == (bool)target) return;
            if (Config.Enabled && !(bool)target)
            {
                TooltipHandler.ResetItemTooltip();
                TooltipHandler.ResetActionTooltip();
            }
            Config.Enabled = (bool)target;
        }
        Config.Save();
    }

}

public enum GameLanguage
{
    Japanese,
    English,
    German,
    French,
    Off,
}