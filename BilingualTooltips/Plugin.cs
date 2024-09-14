using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Style;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Miosuke;


namespace BilingualTooltips;

public sealed partial class Plugin : IDalamudPlugin
{
    public string Name => "BilingualTooltips";
    private const string CommandMainWindow = "/btt";


    public bool PluginThemeEnabled { get; set; }
    public StyleModel PluginTheme { get; set; }


    public IFontHandle Axis20 { get; set; }


    public BilingualTooltipsConfig Config { get; init; }
    public WindowSystem WindowSystem = new("BilingualTooltips");
    public ConfigWindow ConfigWindow { get; init; }
    public MainWindow MainWindow { get; init; }


    public TooltipHandler TooltipHandler { get; set; } = null!;




    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        Service.PluginLog.Info($"[General] Plugin loading...");

        PluginTheme = StyleModel.Deserialize("DS1H4sIAAAAAAAACq1XS3PbOAz+LzpnMnpQlORbE2+bQ7uTadLpbm+MzdiqFcsry27TTP97CZIgIcr2etLqIojChzdB8CUS0SS5jC+ih2jyEv0TTTh8/KvfPy+iWTRhsDCPJjG8peWKLVd8mSuuRyXjIlrY5aWVWNtv8WCJrxbMLDjTKlbRJIWFxnI9BYYYrnWAZXq1DVZTvboZGQmr/6nf2q5O2ad/bS2wVwtKy0W0s6bs7Y9v9vu7lfXsBOfE+x8H1Qlhl7OBH7NW+fkS3cvvvf2f2P/6/cW+P+u34gfGab0VD42cj7VrgH47wOd6PW+/XS28UUXJsiQrEES+NZh8KyHxZZlmeVoxJep6WTdzKgklINIiQO1tu9ltKG+JzCVyl5Zdy75qu7nsHDvLLDsQxCcTYcN8txTKs7OseduJJ0msSSvLDITmBsLIZ57/pt3LjsaZYaAZWsUsjKDezPp67zcGRxBHEEcQLyCjdd9Q25Kk4KyIuUX5T431n8baEtKTeTGB8qxicVGlGMw85knpjGdVnFTc5WEk67ptGrHZkgC8TtwHud5diY76iDEBwvjFSM3ezTql+mEAOZVex/+ug+aCxiYWAoTGADFWAqAw0wyxQBjvjmKDkOcIBcIUCYVeL+Vs9UF0KwcoMdVAaAAQXldTq2IfeHa0DkNEWIoFlmKBpVgQ3NWu71tsrPFlgY4AobmBcNVuuMPAZVWa5LxERdA5kgS3PE8LZoMB2z6PqzL2osLSZbrWsW8wBH8hxTUWdSMF7SM5bnQgDBI3epY49tE+P9hPVV4cIgwsxhUdpdmQG9GJvj23uTn+14fWdnBGpY0awyvy9FFu6x/yXVf7E7XAAANhigQDnOYDSOhOgZkFwiBL2hc98k+Yfk+2T4rxB8J0f6ykkhve34g9tOe8MmJebflAyqf1Yzvb0T4ccwxeonquelBIwspYPW5jFCkLZAQmJVlu8SYiXIvDOmYp7SvTdraq14vbTu5r6Q/elId7DMQZNzzqr6dN/0yPYNSIKSCKbpu2f1+v5dZvMOxFQJhwJYcAw7zhKEV2WpYHsJt627cLdXI7Xa6iByfNAcQxZRi9wdwGE5vpHfQcRF1AjMKgMXbW6bt2vTh1tOWHge/rxbI/Vfkj3MfhtHji2PXsb5r/m16hZu34eicbOeslnSRPlFAGXWTaicW0azf3olvIY6q8cbBv/hb7G+V7M/T/mB7jv8KYcVnVawg+7lgRIKf1E3ENdxbuUPQrhcGonYvG4M4DqWDA3UeNmdEkks3zZikidRkzdwgxKsRhXIyPvg0WQZ3TUUDd887gOu8GIs+62Dx6593JoCkTAHs2GN7FyNMixoRSmctRGfMDmusRl+ummX4IL15ZQSPMIOxEpFdH65Q21cZLDENdcRpG36BQqRu8CZcf39T0dHB7GD68KXunWWZCOAyOP+k5/DfNI3YWDoKjbtRBWvIY+xOV6Seh0s2+6l6BjT2nqfZHQOkOKUyPlm45Fatqxmr15y+4vrSZwxAAAA==")!;

        Axis20 = Service.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, 20.0f));

        Config = Service.PluginInterface.GetPluginConfig() as BilingualTooltipsConfig ?? new BilingualTooltipsConfig();
        Config.Initialize(Service.PluginInterface);
        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        TooltipHandler = new TooltipHandler(this);
        TooltipHandler.StartHook();

        Service.PluginInterface.UiBuilder.Draw += DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
        Service.Framework.Update += OnFrameUpdate;

        Service.Commands.AddHandler(CommandMainWindow, new CommandInfo(OnCommandMainWindow)
        {
            HelpMessage = "main command entry:\n" +
                "└ /btt → open the main window.\n" +
                "└ /btt c|config → open the configuration window.\n" +
                "└ /btt enabled → toggle if plugin is enabled.\n" +
                "└ /btt enabled true|false → set if plugin is enabled."
        });

        Service.PluginLog.Info($"[General] Plugin initialised");
    }

    public void Dispose()
    {
        Service.Commands.RemoveHandler(CommandMainWindow);

        Service.PluginInterface.UiBuilder.Draw -= DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi -= DrawMainUI;
        Service.Framework.Update -= OnFrameUpdate;

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        MainWindow.Dispose();

        TooltipHandler.StopHook();
        TooltipHandler.Dispose();

        Axis20.Dispose();
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
        }
    }

    public void OnFrameUpdate(IFramework framework)
    {
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