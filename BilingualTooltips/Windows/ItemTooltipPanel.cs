namespace BilingualTooltips.Windows;

public class ItemTooltipPanel : Window, IDisposable
{
    private BilingualTooltipsPlugin plugin;

    public List<string> translations = [];

    public ItemTooltipPanel(BilingualTooltipsPlugin plugin) : base(
        "ItemTooltipPanel",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(720, 720);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        if (Service.GameGui.HoveredItem == 0)
        {
            IsOpen = false;
            return;
        }

        ImGui.Text("Thanks for being interested in testing BilingualTooltips.");
        ImGui.Text("Please let me know if you have any questions or suggestions via:");
        ImGui.Text("- Dalamud Discord > plugin-help-forum");
        ImGui.Text("- Discord PM: elypha");
    }
}
