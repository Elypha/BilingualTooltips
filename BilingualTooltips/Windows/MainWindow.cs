using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly BilingualTooltipsPlugin _plugin;

    public MainWindow(BilingualTooltipsPlugin plugin) : base(
        "BilingualTooltips",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(720, 720);
        SizeCondition = ImGuiCond.FirstUseEver;

        _plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void OnClose()
    {
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
        var githubIssuesUrl = "https://github.com/Elypha/BilingualTooltips/issues";

        ImGui.Text("Thanks for being interested in testing this niche plugin!");
        ImGui.Text("Please let me know if you have any question or suggestion via:");

        ImGui.Text("- Discord");
        ImGui.Indent();
        ImGui.Text("1) Official Dalamud Server:");
        ImGui.SameLine();
        Ui.TextUrlWithInlineActionButtons("https://discord.com/invite/holdshift");
        ImGui.Indent();
        ImGui.Text("Goto: plugin-help-forum > Bilingual Tooltips");
        ImGui.Unindent();
        ImGui.Text("2) PM @elypha");
        ImGui.Unindent();

        ImGui.Text("- GitHub Issues (if you want to keep track of the progress)");
        ImGui.Indent();
        Ui.TextUrlWithInlineActionButtons(githubIssuesUrl);
        ImGui.Text("A more detailed guide is available there as well.");
        ImGui.Unindent();

        if (ImGui.Button("Show Config?"))
        {
            _plugin.ConfigWindow.Toggle();
        }
    }
}
