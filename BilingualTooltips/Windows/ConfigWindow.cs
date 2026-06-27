using BilingualTooltips.Configuration;
using Miosuke.Configuration;
using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public partial class ConfigWindow : Window, IDisposable
{
    private const string GeneralTabName = "General";
    private const string HistoryTabName = "History";
    private const string TooltipsTabName = "Tooltips";
    private const string GameUiTabName = "Game UI";
    private const string DialogueTabName = "NPC dialogue";
    private const string HelpTabName = "Help";
    private static readonly Vector4 HistoryTabColour = Ui.HslaToDecimal(225, 1.00, 0.90);
    private static readonly Vector4 TooltipsTabColour = Ui.HslaToDecimal(85, 0.58, 0.76);
    private static readonly Vector4 GameUiTabColour = Ui.HslaToDecimal(135, 0.71, 0.80);
    private static readonly Vector4 DialogueTabColour = Ui.HslaToDecimal(200, 1.00, 0.84);
    private static readonly Vector4 HelpTabColour = Ui.HslaToDecimal(39, 1.00, 0.82);
    private static readonly BilingualTooltipsConfig DefaultConfig = new();

    private readonly AlignedSettingsLayout _settingsLayout = new();

    private readonly BilingualTooltipsPlugin _plugin;
    private string? _activeConfigTabName;
    private string? _requestedTabName;

    public ConfigWindow(BilingualTooltipsPlugin plugin) : base(
        "BilingualTooltips Configuration"
        // ImGuiWindowFlags.NoResize |
        // ImGuiWindowFlags.NoCollapse |
        // ImGuiWindowFlags.NoScrollbar |
        // ImGuiWindowFlags.NoScrollWithMouse
    )
    {
        Size = new Vector2(720, 480);
        SizeCondition = ImGuiCond.FirstUseEver;

        _plugin = plugin;
    }

    public void Dispose()
    {
        ClearGeneralShowcaseDialogueRequirement();
        GC.SuppressFinalize(this);
    }

    public void OpenDialogueResources()
    {
        _requestedTabName = DialogueTabName;
        IsOpen = true;
    }

    public override void OnOpen()
    {
        _activeConfigTabName = null;
        UpdateGeneralShowcaseDialogueRequirement();
    }

    public override void OnClose()
    {
        ClearGeneralShowcaseDialogueRequirement();
        _plugin.Config.Save();
    }

    public override void PreDraw()
    {
        if (_plugin.Config.EnableTheme)
        {
            _plugin.PluginTheme.Push();
            _plugin.PluginThemeEnabled = true;
        }
    }

    public override void PostDraw()
    {
        if (_plugin.PluginThemeEnabled)
        {
            _plugin.PluginTheme.Pop();
            _plugin.PluginThemeEnabled = false;
        }
    }

    public override void Draw()
    {
        UpdateGeneralShowcaseDialogueRequirement();
        float padding = 0.8f;
        DrawConfigTabBar(
            $"###{Name}[ConfigTabs]",
            _requestedTabName,
            ImGuiTabBarFlags.None,
            [
                new ConfigTab(GeneralTabName, () => DrawGeneralTab(padding), null, true),
                new ConfigTab(HistoryTabName, () => DrawHistoryTab(padding), HistoryTabColour, true),
                new ConfigTab(TooltipsTabName, () => DrawTooltipsTab(padding), TooltipsTabColour, true),
                new ConfigTab(GameUiTabName, () => DrawGameUiTab(padding), GameUiTabColour, true),
                new ConfigTab(DialogueTabName, () => DrawDialogueTab(padding), DialogueTabColour, true),
                new ConfigTab(HelpTabName, () => DrawHelpTab(padding), HelpTabColour, true),
            ]);
        _requestedTabName = null;
    }

    private AlignedSettingsLayout.Scope BeginSettingsLayout(string id, float fallbackLabelWidth) =>
        _settingsLayout.Begin(id, fallbackLabelWidth);

    private static ImRaii.TableDisposable BeginConfigTable(string id, AlignedSettingsLayout.Scope layout) =>
        layout.BeginTable(id);

    private static void DrawGameTextLanguageRow(
        AlignedSettingsLayout.Scope layout,
        string label,
        string id,
        BttLanguage current,
        Action<BttLanguage> onChanged,
        bool managed = false)
    {
        layout.BeginRow(label, Ui.ColourWhiteDim);
        ImGui.SetNextItemWidth(160);
        using var background = SettingsControls.PushManagedControlBackground(managed);
        using var combo = ImRaii.Combo(id, current.DisplayName());
        if (!combo) return;

        if (ImGui.Selectable(BttLanguage.Off.DisplayName(), current == BttLanguage.Off) && current != BttLanguage.Off)
        {
            onChanged(BttLanguage.Off);
        }

        foreach (var language in BttLanguageExtensions.GameClientLanguages)
        {
            if (ImGui.Selectable(language.DisplayName(), language == current) && language != current)
            {
                onChanged(language);
            }
        }
    }

    private void DrawConfigTabBar(string id, string? openTabName, ImGuiTabBarFlags flags, params ConfigTab[] tabs)
    {
        using var tabBar = ImRaii.TabBar(id, flags);
        if (!tabBar) return;

        foreach (var tab in tabs)
        {
            var tabFlags = openTabName == tab.Name ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
            using var tabItem = BeginConfigTabItem(tab, tabFlags);
            if (!tabItem) continue;

            if (tab.Child)
            {
                using var child = ImRaii.Child($"{tab.Name}Child");
                if (!child) continue;

                NotifyConfigTabActive(tab.Name);
                tab.Draw();
                continue;
            }

            NotifyConfigTabActive(tab.Name);
            tab.Draw();
        }
    }

    private void NotifyConfigTabActive(string tabName)
    {
        if (string.Equals(_activeConfigTabName, tabName, StringComparison.Ordinal)) return;

        _activeConfigTabName = tabName;
        if (string.Equals(tabName, GeneralTabName, StringComparison.Ordinal))
        {
            RefreshGeneralShowcase(force: true);
        }
    }

    private static ImRaii.TabItemDisposable BeginConfigTabItem(ConfigTab tab, ImGuiTabItemFlags flags)
    {
        using var textColour = ImRaii.PushColor(ImGuiCol.Text, tab.Colour.GetValueOrDefault(), tab.Colour.HasValue);
        return ImRaii.TabItem(tab.Name, flags);
    }

    private readonly record struct ConfigTab(string Name, Action Draw, Vector4? Colour, bool Child);
}
