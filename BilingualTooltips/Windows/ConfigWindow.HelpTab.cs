using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public partial class ConfigWindow
{
    private const float InterfaceCuePreviewWidth = 104f;
    private const float InterfaceCuePreviewColumnWidth = 112f;

    private static void DrawHelpTab(float padding)
    {
        const string discordUrl = "https://discord.com/invite/holdshift";
        const string githubIssuesUrl = "https://github.com/Elypha/BilingualTooltips/issues";

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.5f * padding * ImGui.GetTextLineHeight()));
        ImGui.TextWrapped("Thanks for being interested in testing this niche plugin!");
        ImGui.TextWrapped("Feedback, bug reports, and data-source help are all welcome via:");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.5f * ImGui.GetTextLineHeight()));

        DrawHelpQuoteItem(
            "Discord",
            discordUrl,
            "The official Dalamud discord server. Best for quick questions, screenshots, and test feedback. Use plugin-help-forum > Bilingual Tooltips on the server, or PM @elypha.");

        DrawHelpQuoteItem(
            "GitHub Issues",
            githubIssuesUrl,
            "Best for bugs, feature requests, and anything that should be tracked. Also, to check existing issues and WIPs.");

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.5f * ImGui.GetTextLineHeight()));
        DrawInterfaceCuesSection();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.5f * ImGui.GetTextLineHeight()));
        ImGui.TextColored(Ui.ColourCyan, "Dialogue data volunteers");
        ImGui.TextWrapped(
            "Korean and Traditional Chinese NPC dialogue data are not supported yet because client-derived source bundles are still missing.");
        ImGui.TextWrapped(
            "If you have access to a Korean or Traditional Chinese client and would like to help, please contact me on GitHub or Discord.");
    }

    private static void DrawHelpQuoteItem(
        string name,
        string url,
        string description)
    {
        Ui.DrawQuotedBlock(() =>
        {
            ImGui.TextColored(Ui.ColourCyan, name);
            ImGui.TextUnformatted(url);
            Ui.SameLineInlineCopyActionButton(url, $"HelpCopy:{name}");
            Ui.SameLineInlineOpenUrlActionButton(url, $"HelpOpen:{name}");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.25f * ImGui.GetTextLineHeight()));
            ImGui.TextWrapped(description);
        });
    }

    private static void DrawInterfaceCuesSection()
    {
        ImGui.TextColored(Ui.ColourCyan, "Interface cues");
        using var table = ImRaii.Table(
            "##BTTInterfaceCues",
            2,
            ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Preview", ImGuiTableColumnFlags.WidthFixed, InterfaceCuePreviewColumnWidth);
        ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch);

        DrawInterfaceCueRow(
            () => DrawCuePreview("Managed", SettingsControls.ManagedControlBackgroundColour, null),
            "Changing a camel control asks BilingualTooltips to handle related changes as well as the setting, such as refreshing UI or loading data. This usually finishes right away and the colour is just a friendly cue.");

        DrawInterfaceCueRow(
            () => DrawCuePreview("Ordinary", null, null),
            "Plain controls are simple preferences. Changing them only updates that setting, so no special care is needed.");

        DrawInterfaceCueRow(
            () => DrawCuePreview("Modified", null, SettingsControls.ModifiedInputBorderColour),
            "Light blue outline indicates the value differs from the plugin default. Right click the control to reset it.");
    }

    private static void DrawInterfaceCueRow(Action drawPreview, string description)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var rowY = ImGui.GetCursorPosY();
        var previewY = rowY + ImGui.GetStyle().ItemSpacing.Y;
        ImGui.SetCursorPosY(previewY);
        drawPreview();

        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(rowY);
        ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().X);
        ImGui.TextWrapped(description);
        ImGui.PopTextWrapPos();
    }

    private static void DrawCuePreview(string label, Vector4? backgroundColour, Vector4? borderColour)
    {
        var frameHeight = ImGui.GetFrameHeight();
        var size = new Vector2(InterfaceCuePreviewWidth, frameHeight);
        var position = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        var style = ImGui.GetStyle();
        var background = backgroundColour ?? style.Colors[(int)ImGuiCol.FrameBg];
        var border = borderColour ?? style.Colors[(int)ImGuiCol.Border];
        var borderThickness = borderColour.HasValue ? 1f : 0f;

        drawList.AddRectFilled(
            position,
            position + size,
            ImGui.GetColorU32(background),
            style.FrameRounding);
        if (borderThickness > 0)
        {
            drawList.AddRect(
                position,
                position + size,
                ImGui.GetColorU32(border),
                style.FrameRounding,
                ImDrawFlags.None,
                borderThickness);
        }

        var textSize = ImGui.CalcTextSize(label);
        drawList.AddText(
            position + new Vector2(style.FramePadding.X, (frameHeight - textSize.Y) * 0.5f),
            ImGui.GetColorU32(Ui.ColourWhite),
            label);
        ImGui.Dummy(size);
    }
}
