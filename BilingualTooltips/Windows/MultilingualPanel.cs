using Dalamud.Game.Gui;
using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public class MultilingualPanel : Window, IDisposable
{
    private BilingualTooltipsPlugin plugin;

    public List<string> translations = [];

    public MultilingualPanel(BilingualTooltipsPlugin plugin) : base(
        "Multilingual Panel",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(720, 720);
        SizeCondition = ImGuiCond.FirstUseEver;

        TitleBarButtons.Add(new()
        {
            ShowTooltip = () => ImGui.SetTooltip("Edit mode (for copying text)"),
            Icon = FontAwesomeIcon.Edit,
            IconOffset = new(1, 1),
            Click = _ => IsEditMode = !IsEditMode,
        });

        this.plugin = plugin;
    }

    public void Dispose()
    {
    }

    public bool IsIdle = true;
    private bool IsEditMode = false;

    public string NameJa = "";
    public string NameEn = "";
    public string NameDe = "";
    public string NameFr = "";
    public string DescJa = "";
    public string DescEn = "";
    public string DescDe = "";
    public string DescFr = "";
    private string _NameJa = "";
    private string _NameEn = "";
    private string _NameDe = "";
    private string _NameFr = "";
    private string _DescJa = "";
    private string _DescEn = "";
    private string _DescDe = "";
    private string _DescFr = "";

    public void OnHotkeyTriggered()
    {
        if (!P.Config.ItemTooltipPanelHotkeyEnabled) return;

        if (P.Config.ItemTooltipPanelHotkeyOpenWindow)
        {
            if (!IsOpen)
            {
                IsOpen = true;
            }
            else if (IsOpen && !P.Config.ItemTooltipPanelUpdateOnHotkey)
            {
                IsOpen = false;
            }
        }

        if (P.Config.ItemTooltipPanelUpdateOnHotkey)
        {
            UpdateContent();
        }

    }

    private void UpdateContent()
    {
        _NameJa = NameJa;
        _NameEn = NameEn;
        _NameDe = NameDe;
        _NameFr = NameFr;
        _DescJa = DescJa;
        _DescEn = DescEn;
        _DescDe = DescDe;
        _DescFr = DescFr;
    }

    public override void Draw()
    {
        if (IsEditMode)
        {
            var width = ImGui.GetWindowWidth();
            var height = ImGui.GetTextLineHeightWithSpacing() * 5;
            var size = new Vector2(width, height);
            var _tmp_name_ja = P.Config.ItemTooltipPanelUpdateOnHotkey ? _NameJa : NameJa;
            var _tmp_name_en = P.Config.ItemTooltipPanelUpdateOnHotkey ? _NameEn : NameEn;
            var _tmp_name_de = P.Config.ItemTooltipPanelUpdateOnHotkey ? _NameDe : NameDe;
            var _tmp_name_fr = P.Config.ItemTooltipPanelUpdateOnHotkey ? _NameFr : NameFr;
            var _tmp_desc_ja = P.Config.ItemTooltipPanelUpdateOnHotkey ? _DescJa : DescJa;
            var _tmp_desc_en = P.Config.ItemTooltipPanelUpdateOnHotkey ? _DescEn : DescEn;
            var _tmp_desc_de = P.Config.ItemTooltipPanelUpdateOnHotkey ? _DescDe : DescDe;
            var _tmp_desc_fr = P.Config.ItemTooltipPanelUpdateOnHotkey ? _DescFr : DescFr;
            ImGui.InputText("##1", ref _tmp_name_ja, 1024, ImGuiInputTextFlags.CtrlEnterForNewLine);
            ImGui.InputTextMultiline("Description (JA)", ref _tmp_desc_ja, 1024, size, ImGuiInputTextFlags.CtrlEnterForNewLine);
            ImGui.InputText("##2", ref _tmp_name_en, 1024, ImGuiInputTextFlags.CtrlEnterForNewLine);
            ImGui.InputTextMultiline("Description (EN)", ref _tmp_desc_en, 1024, size, ImGuiInputTextFlags.CtrlEnterForNewLine);
            ImGui.InputText("##3", ref _tmp_name_de, 1024, ImGuiInputTextFlags.CtrlEnterForNewLine);
            ImGui.InputTextMultiline("Description (DE)", ref _tmp_desc_de, 1024, size, ImGuiInputTextFlags.CtrlEnterForNewLine);
            ImGui.InputText("##4", ref _tmp_name_fr, 1024, ImGuiInputTextFlags.CtrlEnterForNewLine);
            ImGui.InputTextMultiline("Description (FR)", ref _tmp_desc_fr, 1024, size, ImGuiInputTextFlags.CtrlEnterForNewLine);
        }
        else
        {


            if (P.Config.ItemTooltipPanelText1 != GameLanguage.Off)
            {
                ImGui.TextColored(Ui.ColourKhaki, P.Config.ItemTooltipPanelUpdateOnHotkey ? _NameJa : NameJa);
                ImGui.TextWrapped(P.Config.ItemTooltipPanelUpdateOnHotkey ? _DescJa : DescJa);
                ImGui.Separator();
            }

            if (P.Config.ItemTooltipPanelText2 != GameLanguage.Off)
            {
                ImGui.TextColored(Ui.ColourKhaki, P.Config.ItemTooltipPanelUpdateOnHotkey ? _NameEn : NameEn);
                ImGui.TextWrapped(P.Config.ItemTooltipPanelUpdateOnHotkey ? _DescEn : DescEn);
                ImGui.Separator();
            }

            if (P.Config.ItemTooltipPanelText3 != GameLanguage.Off)
            {
                ImGui.TextColored(Ui.ColourKhaki, P.Config.ItemTooltipPanelUpdateOnHotkey ? _NameDe : NameDe);
                ImGui.TextWrapped(P.Config.ItemTooltipPanelUpdateOnHotkey ? _DescDe : DescDe);
                ImGui.Separator();
            }

            if (P.Config.ItemTooltipPanelText4 != GameLanguage.Off)
            {
                ImGui.TextColored(Ui.ColourKhaki, P.Config.ItemTooltipPanelUpdateOnHotkey ? _NameFr : NameFr);
                ImGui.TextWrapped(P.Config.ItemTooltipPanelUpdateOnHotkey ? _DescFr : DescFr);
            }
        }
    }
}
