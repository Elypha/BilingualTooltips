#pragma warning disable CS8618

using BilingualTooltips.Assets;
using BilingualTooltips.Configuration;
using BilingualTooltips.Modules;
using BilingualTooltips.Modules.Dialogue;
using BilingualTooltips.Modules.Lookup;
using BilingualTooltips.Windows;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Command;
using Dalamud.Interface.Style;
using Dalamud.Plugin.Services;
using Miosuke.Configuration;
using Miosuke.Messages;
using Miosuke.UserActions;

namespace BilingualTooltips;

public sealed class BilingualTooltipsPlugin : IDalamudPlugin
{
    public static string Name => "BilingualTooltips";
    public static string NameShort => "BTT";
    private const string CommandMainWindow = "/btt";

    // plugin state
    // --------------------------------
    internal static BilingualTooltipsPlugin P;
    internal BilingualTooltipsConfig Config;
    public StyleModel PluginTheme { get; set; }
    public bool PluginThemeEnabled { get; set; }
    public Dalamud.Game.ClientLanguage ClientLanguage;

    // modules
    // --------------------------------
    public TooltipHandler TooltipHandler { get; set; }
    public ContentsFinderHandler ContentsHandler { get; set; }
    public CosmicMissionsHandler CosmicMissionsHandler { get; set; }
    public DialogueStoreProvider DialogueStoreProvider { get; set; }
    public DialogueHandler DialogueHandler { get; set; }
    public BttLookupHistoryStore LookupHistory { get; set; }
    public BttLookupResolver LookupResolver { get; set; }

    // windows
    // --------------------------------
    public ConfigWindow ConfigWindow { get; init; }
    public MainWindow MainWindow { get; init; }
    public LookupHistoryWindow LookupHistoryWindow { get; init; }
    public DialogueTranslationWindow DialogueTranslationWindow { get; init; }
    public WindowSystem WindowSystem = new("BilingualTooltips");

    private bool _lookupHistoryHotkeyIdle = true;
    private bool _dialogueOverlayHotkeyIdle = true;

    public BilingualTooltipsPlugin(IDalamudPluginInterface pluginInterface)
    {
        // services and configuration
        // --------------------------------
        Service.Init(pluginInterface);
        MiosukeHelper.Init(pluginInterface, this, $"[{NameShort}] ", null);
        P = this;

        MioConfig.Setup(mainConfigFileName: "main.json");
        BilingualTooltipsConfigMigrator.MigrateIfNeeded(MioConfig.MainConfigFile);
        Config = MioConfig.Init<BilingualTooltipsConfig>();
        ClientLanguage = Service.ClientState.ClientLanguage;

        ImGuiThemeLoadCustomOrDefault();

        Service.Commands.AddHandler(CommandMainWindow, new CommandInfo(OnCommandMainWindow)
        {
            HelpMessage = "main command entry:\n" +
                "└ /btt → open the main window.\n" +
                "└ /btt c|config → open the configuration window.\n" +
                "└ /btt ml|history → open the lookup history window.\n" +
                "└ /btt dialogue → toggle the NPC dialogue translation window."
        });

        // module startup
        // --------------------------------
        TooltipHandler = new TooltipHandler(this);
        TooltipHandler.StartHook();
        ContentsHandler = new ContentsFinderHandler(this);
        ContentsHandler.StartHook();
        CosmicMissionsHandler = new CosmicMissionsHandler(this);
        CosmicMissionsHandler.StartHook();
        DialogueStoreProvider = new DialogueStoreProvider(this);
        DialogueHandler = new DialogueHandler(this);
        LookupHistory = new BttLookupHistoryStore();
        LookupResolver = new BttLookupResolver(this);

        // windows
        // --------------------------------
        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        LookupHistoryWindow = new LookupHistoryWindow(this);
        DialogueTranslationWindow = new DialogueTranslationWindow(this);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(LookupHistoryWindow);
        WindowSystem.AddWindow(DialogueTranslationWindow);
        DialogueHandler.StartHook();

        // framework hooks
        // --------------------------------
        Service.PluginInterface.UiBuilder.Draw += DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
        Service.Framework.Update += OnFrameUpdate;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        // command handlers
        Service.Commands.RemoveHandler(CommandMainWindow);

        // modules
        TooltipHandler.Dispose();
        ContentsHandler.StopHook();
        ContentsHandler.Dispose();
        CosmicMissionsHandler.StopHook();
        CosmicMissionsHandler.Dispose();
        DialogueHandler.Dispose();

        // windows
        ConfigWindow.Dispose();
        MainWindow.Dispose();
        LookupHistoryWindow.Dispose();
        DialogueTranslationWindow.Dispose();
        LookupHistory.Flush();
        DialogueStoreProvider.Dispose();

        // event handlers
        Service.PluginInterface.UiBuilder.Draw -= DrawUI;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi -= DrawMainUI;
        Service.Framework.Update -= OnFrameUpdate;

        MiosukeHelper.Dispose();
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

    public void RecordLookup(BttLookupKey key, string displayName)
    {
        if (!Config.LookupHistoryEnabled) return;

        if (!Config.LookupHistorySources.HasFlag(SourceFor(key.Kind))) return;

        LookupHistory.Record(key, displayName, Config.LookupHistoryLimit);
    }

    public static bool ImGuiThemeLoadCustomOrDefault()
    {
        var customTheme = P.Config.CustomTheme.Trim();
        if (string.IsNullOrEmpty(customTheme))
        {
            P.PluginTheme = Data.DefaultTheme;
            return true;
        }

        try
        {
            P.PluginTheme = StyleModel.Deserialize(customTheme)
                ?? throw new InvalidOperationException("Theme deserialised to null.");
            return true;
        }
        catch (Exception e)
        {
            P.Config.CustomTheme = "";
            P.Config.Save();
            P.PluginTheme = Data.DefaultTheme;
            Notice.Error($"Your theme override is invalid and has been reset to the bundled theme: {e.Message}");
            return false;
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
            case "ml" or "history":
                LookupHistoryWindow.Toggle();
                break;
            case "dialogue" or "dialog":
                if (Config.TalkDialogueOverlayEnabled)
                {
                    DialogueTranslationWindow.Toggle();
                }

                break;
            default:
                Notice.Error("Invalid command argument.");
                break;
        }
    }

    public void OnFrameUpdate(IFramework framework)
    {
        LookupHistory.FlushIfDue();
        DialogueHandler.OnFrameUpdate();

        ProcessShortcut(
            Config.LookupHistoryHotkeyEnabled,
            Config.LookupHistoryHotkey,
            ref _lookupHistoryHotkeyIdle,
            LookupHistoryWindow.OnHotkeyTriggered);
        ProcessShortcut(
            Config is
            {
                TalkDialogueEnabled: true,
                TalkDialogueOverlayEnabled: true,
                TalkDialogueOverlayShortcutEnabled: true
            },
            Config.TalkDialogueOverlayShortcutHotkey,
            ref _dialogueOverlayHotkeyIdle,
            () => DialogueTranslationWindow.IsOpen = !DialogueTranslationWindow.IsOpen);
    }

    private static void ProcessShortcut(bool enabled, VirtualKey[] hotkey, ref bool idle, Action trigger)
    {
        if (!enabled)
        {
            idle = true;
            return;
        }

        var active = Hotkey.IsActive(hotkey);
        if (!idle && !active)
        {
            idle = true;
        }

        if (active && idle)
        {
            trigger();
            idle = false;
        }
    }

    private static BttLookupHistorySource SourceFor(BttLookupKind kind) => kind switch
    {
        BttLookupKind.Item => BttLookupHistorySource.Item,
        BttLookupKind.Action => BttLookupHistorySource.Action,
        BttLookupKind.Content => BttLookupHistorySource.Content,
        BttLookupKind.NpcDialogue => BttLookupHistorySource.NpcDialogue,
        _ => BttLookupHistorySource.None,
    };
}
