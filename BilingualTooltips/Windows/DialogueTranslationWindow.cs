using BilingualTooltips.Assets;
using BilingualTooltips.Modules.Dialogue;
using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public sealed class DialogueTranslationWindow : Window, IDisposable
{
    private const float PanelRounding = 8f;
    private const float PanelBorderThickness = 1f;
    private const float PanelMinHeight = 84f;
    private const float DragHandleHeight = 24f;
    private const float CloseButtonSize = 18f;
    private const float BodyTop = 16f;
    private const float SpeakerLabelFadeStartBeforeTextEnd = 16f;
    private const float SpeakerLabelFadeWidth = 48f;
    private const float SpeakerLabelRounding = 7f;
    private static readonly Vector2 PanelOuterPadding = new(4f, 10f);
    private static readonly Vector2 PanelPadding = new(16f, 9f);
    private static readonly Vector2 SpeakerLabelOffset = new(18f, -6f);
    private static readonly Vector2 SpeakerLabelPadding = new(7f, 2f);
    private static readonly Vector2 CloseButtonOffsetFromTopRight = new(-3f, 3f);
    private static readonly Vector2 PanelShadowOffset = new(2f, 3f);
    private static readonly Vector4 PanelShadowColour = new(0f, 0f, 0f, 0.34f);
    private static readonly Vector4 PanelBackgroundColour = Ui.HslaToDecimal(220, 0.10, 0.10, 0.82);
    private static readonly Vector4 PanelHighlightColour = new(1f, 1f, 1f, 0.07f);
    private static readonly Vector4 PanelBorderColour = Ui.HslaToDecimal(190, 0.24, 0.62, 0.32);
    private static readonly Vector4 SpeakerTextColour = Ui.HslaToDecimal(175, 0.38, 0.70, 0.94);
    private static readonly Vector4 BodyTextColour = Ui.HslaToDecimal(42, 0.18, 0.92);
    private static readonly Vector4 SecondaryTextColour = Ui.HslaToDecimal(42, 0.10, 0.72, 0.86);
    private static readonly Vector4 WarningTextColour = Ui.HslaToDecimal(42, 0.75, 0.72);
    private static readonly Vector4 SeparatorColour = new(1f, 1f, 1f, 0.14f);
    private static readonly Vector4 CloseButtonColour = new(1f, 1f, 1f, 0.42f);
    private static readonly Vector4 CloseButtonHoverColour = new(1f, 1f, 1f, 0.84f);
    private static readonly Vector4 SpeakerLabelBackgroundColour = Ui.HslaToDecimal(210, 0.18, 0.13, 0.92);
    private static readonly Vector4 SpeakerLabelFadeColour = Ui.HslaToDecimal(210, 0.18, 0.13, 0.00);

    private readonly BilingualTooltipsPlugin _plugin;
    private IDisposable? _overlayStyle;
    private IDisposable? _overlayColours;

    public DialogueTranslationWindow(BilingualTooltipsPlugin plugin) : base(
        "NPC dialogue###BTT Dialogue",
        ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoScrollWithMouse
        | ImGuiWindowFlags.NoCollapse
        | ImGuiWindowFlags.NoBackground)
    {
        _plugin = plugin;
        Size = new Vector2(560, 118);
        SizeCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = false;
    }

    public void Dispose()
    {
    }

    public override void PreDraw()
    {
        if (_plugin.Config.EnableTheme)
        {
            _plugin.PluginTheme.Push();
            _plugin.PluginThemeEnabled = true;
        }

        PushOverlayWindowStyle();
    }

    public override void PostDraw()
    {
        PopOverlayWindowStyle();

        if (_plugin.PluginThemeEnabled)
        {
            _plugin.PluginTheme.Pop();
            _plugin.PluginThemeEnabled = false;
        }
    }

    private void PushOverlayWindowStyle()
    {
        _overlayStyle = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero)
            .Push(ImGuiStyleVar.WindowBorderSize, 0f)
            .Push(ImGuiStyleVar.WindowRounding, 0f);
        _overlayColours = ImRaii.PushColor(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, 0f))
            .Push(ImGuiCol.Border, new Vector4(0f, 0f, 0f, 0f));
    }

    private void PopOverlayWindowStyle()
    {
        _overlayColours?.Dispose();
        _overlayColours = null;
        _overlayStyle?.Dispose();
        _overlayStyle = null;
    }

    public override void Draw()
    {
        var state = _plugin.DialogueHandler.State;
        DrawDialoguePanel(state);
    }

    private void DrawDialoguePanel(DialogueTranslationState state)
    {
        var availableSize = ImGui.GetContentRegionAvail();
        if (availableSize.X <= PanelOuterPadding.X * 2f || availableSize.Y <= PanelOuterPadding.Y * 2f) return;

        var panelSize = availableSize - PanelOuterPadding * 2f - PanelShadowOffset;
        panelSize.Y = Math.Max(panelSize.Y, PanelMinHeight);

        var panelMin = ImGui.GetCursorScreenPos() + PanelOuterPadding;
        var panelMax = panelMin + panelSize;
        var speakerName = GetSpeakerName(state);
        DrawPanelFrame(panelMin, panelMax);
        DrawSpeakerLabel(speakerName, panelMin);

        var contentMin = panelMin + new Vector2(PanelPadding.X, BodyTop);
        var contentSize = new Vector2(
            panelSize.X - PanelPadding.X * 2f,
            Math.Max(1f, panelSize.Y - BodyTop - PanelPadding.Y));

        ImGui.SetCursorScreenPos(contentMin);
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, PanelRounding)
                   .Push(ImGuiStyleVar.ChildBorderSize, 0f)
                   .Push(ImGuiStyleVar.WindowPadding, Vector2.Zero))
        using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0f, 0f, 0f, 0f)))
        using (var child = ImRaii.Child(
                   "##BTTDialogueFloatingNotePanel",
                   contentSize,
                   false,
                   ImGuiWindowFlags.NoBackground))
        {
            if (child)
            {
                ImGui.SetCursorPos(Vector2.Zero);
                var contentWidth = ImGui.GetContentRegionAvail().X;
                var contentLayout = new ContentLayout(0f, contentWidth, contentWidth);
                DrawStateContent(state, contentLayout);
            }
        }

        DrawWindowDragHandle(panelMin, panelSize);
        DrawCloseButton(panelMin, panelSize);
    }

    private static void DrawWindowDragHandle(Vector2 panelMin, Vector2 panelSize)
    {
        ImGui.SetCursorScreenPos(panelMin);
        ImGui.InvisibleButton("##BTTDialogueDragHandle", new Vector2(panelSize.X - CloseButtonSize - 10f, DragHandleHeight));
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            ImGui.SetWindowPos(ImGui.GetWindowPos() + ImGui.GetIO().MouseDelta);
        }
    }

    private void DrawCloseButton(Vector2 panelMin, Vector2 panelSize)
    {
        var buttonPos = panelMin + new Vector2(panelSize.X - CloseButtonSize, 0f) + CloseButtonOffsetFromTopRight;
        var buttonSize = new Vector2(CloseButtonSize);
        var buttonMax = buttonPos + buttonSize;
        var hovered = ImGui.IsMouseHoveringRect(buttonPos, buttonMax, false);
        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            IsOpen = false;
        }

        var drawList = ImGui.GetWindowDrawList();
        if (hovered)
        {
            drawList.AddCircleFilled(
                buttonPos + buttonSize * 0.5f,
                CloseButtonSize * 0.5f,
                ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.08f)));
        }

        var text = "x";
        var textSize = ImGui.CalcTextSize(text);
        drawList.AddText(
            buttonPos + (buttonSize - textSize) * 0.5f,
            ImGui.GetColorU32(hovered ? CloseButtonHoverColour : CloseButtonColour),
            text);
    }

    private static void DrawPanelFrame(Vector2 panelMin, Vector2 panelMax)
    {
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(
            panelMin + PanelShadowOffset,
            panelMax + PanelShadowOffset,
            ImGui.GetColorU32(PanelShadowColour),
            PanelRounding);
        drawList.AddRectFilled(
            panelMin,
            panelMax,
            ImGui.GetColorU32(PanelBackgroundColour),
            PanelRounding);
        drawList.AddRect(
            panelMin + new Vector2(1f, 1f),
            panelMax - new Vector2(1f, 1f),
            ImGui.GetColorU32(PanelHighlightColour),
            PanelRounding,
            ImDrawFlags.RoundCornersAll,
            1f);
        drawList.AddRect(
            panelMin,
            panelMax,
            ImGui.GetColorU32(PanelBorderColour),
            PanelRounding,
            ImDrawFlags.RoundCornersAll,
            PanelBorderThickness);
    }

    private static string GetSpeakerName(DialogueTranslationState state) =>
        string.IsNullOrWhiteSpace(state.SpeakerName)
            ? ""
            : state.SpeakerName;

    private static void DrawSpeakerLabel(string speakerName, Vector2 panelMin)
    {
        if (speakerName.Length == 0) return;

        var drawList = ImGui.GetWindowDrawList();
        var textPos = panelMin + SpeakerLabelOffset;
        var textSize = ImGui.CalcTextSize(speakerName);
        var backgroundMin = textPos - SpeakerLabelPadding;
        var backgroundMax = textPos + textSize + SpeakerLabelPadding;
        var fadeStartX = Math.Max(
            backgroundMin.X + SpeakerLabelRounding,
            textPos.X + textSize.X - SpeakerLabelFadeStartBeforeTextEnd);
        var solidMax = new Vector2(fadeStartX, backgroundMax.Y);
        var fadeMin = new Vector2(fadeStartX, backgroundMin.Y);
        var fadeMax = new Vector2(fadeStartX + SpeakerLabelFadeWidth, backgroundMax.Y);

        drawList.AddRectFilled(
            backgroundMin,
            solidMax,
            ImGui.GetColorU32(SpeakerLabelBackgroundColour),
            SpeakerLabelRounding,
            ImDrawFlags.RoundCornersLeft);

        drawList.AddRectFilledMultiColor(
            fadeMin,
            fadeMax,
            ImGui.GetColorU32(SpeakerLabelBackgroundColour),
            ImGui.GetColorU32(SpeakerLabelFadeColour),
            ImGui.GetColorU32(SpeakerLabelFadeColour),
            ImGui.GetColorU32(SpeakerLabelBackgroundColour));
        drawList.AddText(textPos, ImGui.GetColorU32(SpeakerTextColour), speakerName);
    }

    private void DrawStateContent(DialogueTranslationState state, ContentLayout layout)
    {
        switch (state.Status)
        {
            case DialogueTranslationStatus.Ok:
                DrawTranslation(state, layout);
                break;
            case DialogueTranslationStatus.Searching:
                DrawBodyText("Searching...", SecondaryTextColour, layout);
                DrawSourceText(state, layout);
                break;
            case DialogueTranslationStatus.Loading:
                DrawBodyText("Loading dialogue data...", SecondaryTextColour, layout);
                DrawSourceText(state, layout);
                break;
            case DialogueTranslationStatus.NoData:
                DrawBodyText("No local dialogue package is loaded.", WarningTextColour, layout);
                DrawSourceText(state, layout);
                break;
            case DialogueTranslationStatus.Error:
                DrawBodyText("Dialogue data operation failed.", WarningTextColour, layout);
                DrawSourceText(state, layout);
                break;
            case DialogueTranslationStatus.NotFound:
                DrawBodyText("No local dialogue match found.", WarningTextColour, layout);
                DrawSourceText(state, layout);
                break;
            default:
                DrawBodyText("No dialogue captured yet.", SecondaryTextColour, layout);
                break;
        }
    }

    private void DrawTranslation(DialogueTranslationState state, ContentLayout layout)
    {
        DrawBodyText(state.TargetText, BodyTextColour, layout);

        if (!HasMatchDetails(state) && !HasSourceText(state)) return;

        DrawSubtleSeparator(layout);
        DrawMatchDetails(state, layout);
        DrawSourceText(state, layout);
    }

    private void DrawMatchDetails(DialogueTranslationState state, ContentLayout layout)
    {
        if (!HasMatchDetails(state)) return;

        var details = BuildMatchDetails(state);
        DrawSecondaryText(details, SecondaryTextColour, layout);
    }

    private void DrawSourceText(DialogueTranslationState state, ContentLayout layout)
    {
        if (!HasSourceText(state)) return;

        DrawBodyText(state.SourceText, SecondaryTextColour, layout);
    }

    private static void DrawBodyText(string text, Vector4 textColour, ContentLayout layout)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        SetContentColumn(layout);
        using (Data.DialogueCjkBody20.Push())
        using (ImRaii.PushColor(ImGuiCol.Text, textColour))
        using (new TextWrapScope(layout.WrapLocalX))
        {
            ImGui.TextWrapped(text);
        }
    }

    private static void DrawSecondaryText(string text, Vector4 textColour, ContentLayout layout)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        SetContentColumn(layout);
        using (ImRaii.PushColor(ImGuiCol.Text, textColour))
        using (new TextWrapScope(layout.WrapLocalX))
        {
            ImGui.TextWrapped(text);
        }
    }

    private static void DrawSubtleSeparator(ContentLayout layout)
    {
        var style = ImGui.GetStyle();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);
        SetContentColumn(layout);
        var start = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddLine(
            start,
            start with { X = start.X + layout.Width },
            ImGui.GetColorU32(SeparatorColour));
        ImGui.Dummy(new Vector2(layout.Width, style.ItemSpacing.Y));
    }

    private bool HasMatchDetails(DialogueTranslationState state) =>
        _plugin.Config.TalkDialogueOverlayShowMatchDetails && !string.IsNullOrWhiteSpace(BuildMatchDetails(state));

    private bool HasSourceText(DialogueTranslationState state) =>
        _plugin.Config.TalkDialogueOverlayShowSourceText && !string.IsNullOrWhiteSpace(state.SourceText);

    private static string BuildMatchDetails(DialogueTranslationState state)
    {
        var hasMatchKind = !string.IsNullOrWhiteSpace(state.MatchKind);
        var hasKey = !string.IsNullOrWhiteSpace(state.Key);
        return (hasMatchKind, hasKey) switch
        {
            (true, true) => $"{state.MatchKind} · {state.Key}",
            (true, false) => state.MatchKind,
            (false, true) => state.Key,
            _ => "",
        };
    }

    private static void SetContentColumn(ContentLayout layout)
    {
        var cursor = ImGui.GetCursorPos();
        if (Math.Abs(cursor.X - layout.LocalX) > 0.01f)
        {
            ImGui.SetCursorPos(new Vector2(layout.LocalX, cursor.Y));
        }
    }

    private readonly record struct ContentLayout(float LocalX, float WrapLocalX, float Width);

    private readonly struct TextWrapScope : IDisposable
    {
        public TextWrapScope(float wrapLocalX)
        {
            ImGui.PushTextWrapPos(wrapLocalX);
        }

        public void Dispose()
        {
            ImGui.PopTextWrapPos();
        }
    }
}
