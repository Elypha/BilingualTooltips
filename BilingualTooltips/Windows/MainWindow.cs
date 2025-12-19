using Miosuke.UiHelper;


namespace BilingualTooltips.Windows;

public class MainWindow : Window, IDisposable
{
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
        var github_issues_url = "https://github.com/Elypha/BilingualTooltips/issues";

        ImGui.Text("Thanks for being interested in testing this niche plugin!");
        ImGui.Text("Please let me know if you have any question or suggestion via:");

        ImGui.Text("- Discord");
        ImGui.Indent();
        ImGui.Text("1) Official Dalamud Server:");
        ImGui.SameLine();
        Ui.TextUrlWithLabelButton("https://discord.com/invite/holdshift");
        ImGui.Indent();
        ImGui.Text("Goto: plugin-help-forum > Bilingual Tooltips");
        ImGui.Unindent();
        ImGui.Text("2) PM @elypha");
        ImGui.Unindent();

        ImGui.Text("- GitHub Issues (if you want to keep track of the progress)");
        ImGui.Indent();
        Ui.TextUrlWithLabelButton(github_issues_url);
        ImGui.Text("A more detailed guide is available there as well.");
        ImGui.Unindent();


        if (ImGui.Button("Show Config?"))
        {
            P.ConfigWindow.Toggle();
        }
    }
}
