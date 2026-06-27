using System.IO;
using BilingualTooltips.Modules.Dialogue;
using BilingualTooltips.Modules.Dialogue.Data;
using BilingualTooltips.Modules.Dialogue.Package;
using Dalamud.Interface.Components;
using Miosuke.Configuration;
using Miosuke.UiHelper;

namespace BilingualTooltips.Windows;

public partial class ConfigWindow
{
    private static readonly Vector4 DialogueStatusProgressFillColour = Ui.HslaToDecimal(216, 0.86, 0.71, 0.55);

    private readonly HotkeyEditor _dialogueOverlayHotkeyEditor = new();

    private void DrawDialogueTab(float padding)
    {
        var suffix = $"###{Name}[Dialogue]";
        using var layout = BeginSettingsLayout("Dialogue", 150f);
        var handler = _plugin.DialogueHandler;
        var busy = handler.IsDataOperationRunning;
        var packageInfo = handler.ActivePackageInfo;
        var remoteInfo = handler.RemotePackageInfo;
        var updateAvailable = IsDialoguePackageUpdateAvailable(packageInfo, remoteInfo);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.4f * padding * ImGui.GetTextLineHeight()));
        ImGui.TextColored(Ui.ColourCyan, "Dialogue data");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.2f * ImGui.GetTextLineHeight()));
        DrawDialogueDataPanel(suffix, handler, packageInfo, remoteInfo, updateAvailable);
        DrawDialogueResourceAdvanced(suffix, handler, packageInfo, remoteInfo, busy);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.8f * ImGui.GetTextLineHeight()));
        DrawDialogueBehaviourSettings(suffix, layout, busy);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.8f * ImGui.GetTextLineHeight()));
        DrawDialogueRenderProfileSettings(suffix, layout);
    }

    private void DrawDialogueBehaviourSettings(string suffix, AlignedSettingsLayout.Scope layout, bool busy)
    {
        ImGui.TextColored(Ui.ColourCyan, "Behaviour");
        using var table = BeginConfigTable("##BTTDialogueBehaviour", layout);
        if (!table) return;

        layout.BeginRow("Enable", Ui.ColourWhiteDim);
        var talkDialogueEnabled = _plugin.Config.TalkDialogueEnabled;
        var canChangeTalkDialogueEnabled = _plugin.DialogueHandler.CanChangeTalkDialogueEnabled;
        if (!canChangeTalkDialogueEnabled) ImGui.BeginDisabled();

        if (SettingsControls.ManagedCheckbox($"{suffix}TalkDialogueEnabled", ref talkDialogueEnabled))
        {
            _plugin.Config.TalkDialogueEnabled = talkDialogueEnabled;
            _plugin.Config.Save();
            _plugin.DialogueHandler.OnTalkDialogueEnabledChanged(talkDialogueEnabled);
        }

        if (!canChangeTalkDialogueEnabled) ImGui.EndDisabled();

        layout.BeginRow("Input language", Ui.ColourWhiteDim);
        DrawDialogueLanguageCombo(
            $"{suffix}TalkDialogueInputLanguage",
            _plugin.Config.TalkDialogueInputLanguage,
            includeAuto: true,
            disabled: busy,
            managed: true,
            value =>
            {
                _plugin.Config.TalkDialogueInputLanguage = value;
                _plugin.Config.Save();
                if (!busy && _plugin.Config.TalkDialogueEnabled)
                {
                    _plugin.DialogueHandler.OnTalkDialogueLanguagesChanged();
                }
            });
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "Korean and Traditional Chinese NPC dialogue data are not supported yet because client-derived source bundles are still missing.\n" +
            "If you have access to a Korean or Traditional Chinese client and would like to help, please contact me on GitHub or Discord.");

        layout.BeginRow("Target language", Ui.ColourWhiteDim);
        DrawDialogueLanguageCombo(
            $"{suffix}TalkDialogueTargetLanguage",
            _plugin.Config.TalkDialogueTargetLanguage,
            includeAuto: false,
            disabled: busy,
            managed: true,
            value =>
            {
                _plugin.Config.TalkDialogueTargetLanguage = value;
                _plugin.Config.Save();
                if (!busy && _plugin.Config.TalkDialogueEnabled)
                {
                    _plugin.DialogueHandler.OnTalkDialogueLanguagesChanged();
                }
            });

        SettingsRows.DefaultedCheckbox(
            layout,
            "Show source text",
            $"{suffix}TalkDialogueOverlayShowSourceText",
            ref _plugin.Config.TalkDialogueOverlayShowSourceText,
            DefaultConfig.TalkDialogueOverlayShowSourceText,
            onChanged: MioConfig.Save);

        SettingsRows.DefaultedCheckbox(
            layout,
            "Show match details",
            $"{suffix}TalkDialogueOverlayShowMatchDetails",
            ref _plugin.Config.TalkDialogueOverlayShowMatchDetails,
            DefaultConfig.TalkDialogueOverlayShowMatchDetails,
            onChanged: MioConfig.Save);

        layout.BeginRow("Enable overlay", Ui.ColourWhiteDim);
        if (SettingsControls.ManagedCheckbox($"{suffix}TalkDialogueOverlayEnabled", ref _plugin.Config.TalkDialogueOverlayEnabled))
        {
            if (!_plugin.Config.TalkDialogueOverlayEnabled)
            {
                _plugin.DialogueTranslationWindow.IsOpen = false;
            }

            _plugin.Config.Save();
        }

        DrawDialogueOverlayAutoOpenRow(suffix, layout);
        DrawDialogueOverlayShortcutRow(suffix, layout);
    }

    private void DrawDialogueOverlayAutoOpenRow(string suffix, AlignedSettingsLayout.Scope layout)
    {
        DrawDialogueOverlayCheckboxChildRow(
            layout,
            "Open automatically",
            $"{suffix}TalkDialogueOpenWindowAutomatically",
            ref _plugin.Config.TalkDialogueOpenWindowAutomatically,
            DefaultConfig.TalkDialogueOpenWindowAutomatically,
            last: false);
    }

    private void DrawDialogueOverlayShortcutRow(string suffix, AlignedSettingsLayout.Scope layout)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        SettingsControls.DrawGroupedChildLabel(layout, "Enable shortcut", _plugin.Config.TalkDialogueOverlayEnabled, last: true);
        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!_plugin.Config.TalkDialogueOverlayEnabled))
        {
            if (SettingsControls.ShortcutControls(
                    $"{suffix}TalkDialogueOverlayShortcutEnabled",
                    ref _plugin.Config.TalkDialogueOverlayShortcutEnabled,
                    _dialogueOverlayHotkeyEditor,
                    $"{suffix}TalkDialogueOverlayShortcutHotkey",
                    ref _plugin.Config.TalkDialogueOverlayShortcutHotkey,
                    "Toggles the NPC dialogue overlay window."))
            {
                _plugin.Config.Save();
            }
        }
    }

    private void DrawDialogueOverlayCheckboxChildRow(
        AlignedSettingsLayout.Scope layout,
        string label,
        string id,
        ref bool value,
        bool defaultValue,
        bool last)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        SettingsControls.DrawGroupedChildLabel(layout, label, _plugin.Config.TalkDialogueOverlayEnabled, last);
        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!_plugin.Config.TalkDialogueOverlayEnabled))
        {
            if (SettingsControls.DefaultedCheckbox(id, ref value, defaultValue).ValueChanged)
            {
                _plugin.Config.Save();
            }
        }
    }

    private void DrawDialogueRenderProfileSettings(string suffix, AlignedSettingsLayout.Scope layout)
    {
        ImGui.TextColored(Ui.ColourCyan, "Render profile");

        using var table = BeginConfigTable("##BTTDialogueRenderProfile", layout);
        if (!table) return;

        DrawDialogueRenderProfileNameInput(
            layout,
            "First name",
            $"{suffix}TalkDialogueRenderProfileFirstName",
            _plugin.Config.TalkDialogueRenderProfileFirstName,
            DefaultConfig.TalkDialogueRenderProfileFirstName,
            value => _plugin.Config.TalkDialogueRenderProfileFirstName = value,
            managed: true);

        DrawDialogueRenderProfileNameInput(
            layout,
            "Last name",
            $"{suffix}TalkDialogueRenderProfileLastName",
            _plugin.Config.TalkDialogueRenderProfileLastName,
            DefaultConfig.TalkDialogueRenderProfileLastName,
            value => _plugin.Config.TalkDialogueRenderProfileLastName = value,
            managed: true);

        layout.BeginRow("Gender", Ui.ColourWhiteDim);
        DrawDialogueGenderCombo(
            $"{suffix}TalkDialogueRenderProfileGender",
            _plugin.Config.TalkDialogueRenderProfileGender,
            managed: true,
            value =>
            {
                _plugin.Config.TalkDialogueRenderProfileGender = value;
                _plugin.Config.Save();
                _plugin.DialogueHandler.RefreshCurrentTranslationRendering();
            });
        ImGuiComponents.HelpMarker(
            "Stored as an explicit user setting for gender-aware dialogue templates.\n" +
            "Package default keeps the first branch stored in the installed dialogue package.");
    }

    private void DrawDialogueRenderProfileNameInput(
        AlignedSettingsLayout.Scope layout,
        string label,
        string id,
        string current,
        string defaultValue,
        Action<string> onChanged,
        bool managed = false)
    {
        layout.BeginRow(label, Ui.ColourWhiteDim);

        var value = current;
        if (!SettingsControls.DefaultedInputText(id, ref value, defaultValue, 64, width: 220, managed: managed).ValueChanged) return;

        onChanged(value.Trim());
        _plugin.Config.Save();
        _plugin.DialogueHandler.RefreshCurrentTranslationRendering();
    }

    private static void DrawDialogueGenderCombo(
        string id,
        BttDialogueGender current,
        bool managed,
        Action<BttDialogueGender> onChanged)
    {
        ImGui.SetNextItemWidth(220);
        using var background = SettingsControls.PushManagedControlBackground(managed);
        using var combo = ImRaii.Combo(id, current.DisplayName());
        if (!combo) return;

        foreach (var gender in Enum.GetValues<BttDialogueGender>())
        {
            if (ImGui.Selectable(gender.DisplayName(), gender == current) && gender != current)
            {
                onChanged(gender);
            }
        }
    }

    private static void DrawDialogueLanguageCombo(
        string id,
        BttLanguage current,
        bool includeAuto,
        bool disabled,
        bool managed,
        Action<BttLanguage> onChanged)
    {
        if (disabled) ImGui.BeginDisabled();

        var options = Enum.GetValues<BttLanguage>()
            .Where(type => includeAuto || type != BttLanguage.Auto)
            .ToArray();
        var popupHeight = ImGui.GetTextLineHeightWithSpacing() * options.Length
            + (ImGui.GetStyle().WindowPadding.Y * 2)
            + ImGui.GetStyle().FramePadding.Y;

        ImGui.SetNextItemWidth(220);
        ImGui.SetNextWindowSizeConstraints(new Vector2(220, popupHeight), new Vector2(320, popupHeight));
        using var background = SettingsControls.PushManagedControlBackground(managed);
        if (!ImGui.BeginCombo(id, current.DisplayNameWithSupportStatus()))
        {
            if (disabled) ImGui.EndDisabled();
            return;
        }

        foreach (var type in options)
        {
            var supported = type is BttLanguage.Off or BttLanguage.Auto || type.IsReleaseSupported();
            if (!supported) ImGui.BeginDisabled();

            if (ImGui.Selectable(type.DisplayNameWithSupportStatus(), type == current) && type != current)
            {
                onChanged(type);
            }

            if (!supported) ImGui.EndDisabled();
        }

        ImGui.EndCombo();

        if (disabled) ImGui.EndDisabled();
    }

    private static bool IsDialoguePackageUpdateAvailable(DialoguePackageInfo? packageInfo,
        DialogueRemotePackageInfo? remoteInfo)
    {
        if (remoteInfo == null) return false;
        if (packageInfo?.BuildNumber == null) return true;
        return remoteInfo.BuildNumber > packageInfo.BuildNumber.Value;
    }

    private void DrawDialogueDataPanel(
        string suffix,
        DialogueHandler handler,
        DialoguePackageInfo? packageInfo,
        DialogueRemotePackageInfo? remoteInfo,
        bool updateAvailable)
    {
        const float paddingX = 6f;
        const float paddingY = 6f;
        const float bottomSpacer = 6f;
        const float rounding = 5f;
        var style = ImGui.GetStyle();
        var summaryHeight = ImGui.GetTextLineHeightWithSpacing() * 6;
        var statusHeight = ImGui.GetTextLineHeight()
            + (ImGui.GetFrameHeight() * 2)
            + (style.ItemSpacing.Y * 3)
            + bottomSpacer;
        var separatorHeight = style.ItemSpacing.Y * 2;
        var panelHeight = (paddingY * 2) + summaryHeight + separatorHeight + statusHeight;

        using var styles = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, rounding)
            .Push(ImGuiStyleVar.WindowPadding, new Vector2(paddingX, paddingY));
        using var colours = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0f, 0f, 0f, 0f))
            .Push(ImGuiCol.Border, new Vector4(1f, 1f, 1f, 0.18f));
        using var panel = ImRaii.Child(
            $"##{suffix}DialogueDataPanel",
            new Vector2(-1, panelHeight),
            true,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!panel) return;

        DrawPackageSummaryTable(packageInfo, remoteInfo);
        DrawSubtleSeparator();
        DrawDialogueDataStatus(suffix, handler, packageInfo, remoteInfo, updateAvailable);
    }

    private static void DrawSubtleSeparator()
    {
        var start = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var y = start.Y + ImGui.GetStyle().ItemSpacing.Y;
        ImGui.GetWindowDrawList().AddLine(
            new Vector2(start.X, y),
            new Vector2(start.X + width, y),
            ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.12f)));
        ImGui.Dummy(new Vector2(width, ImGui.GetStyle().ItemSpacing.Y * 2));
    }

    private void DrawDialogueDataStatus(
        string suffix,
        DialogueHandler handler,
        DialoguePackageInfo? packageInfo,
        DialogueRemotePackageInfo? remoteInfo,
        bool updateAvailable)
    {
        var (statusTitle, statusDetail, statusColour) =
            GetDialogueDataStatus(handler, packageInfo, remoteInfo, updateAvailable);
        var progressText = "Idle";
        var progressFraction = 0f;
        var progress = handler.PackageUpdateProgress;
        if (handler.IsDataOperationRunning)
        {
            progressFraction = progress.Fraction;
            progressText = progress.TotalBytes is > 0
                ? $"{progress.Status}: {FormatBytes(progress.BytesDownloaded)} / {FormatBytes(progress.TotalBytes.Value)}"
                : DisplayOrDash(progress.Status);
        }
        else if (handler.IsPackageUpdateCheckRunning)
        {
            progressText = "Checking for updates";
        }

        var customPackagePathActive = !string.IsNullOrWhiteSpace(_plugin.Config.TalkDialogueDataPath);
        var canCheck = handler is
            {
                IsDataOperationRunning: false,
                IsPackageUpdateCheckRunning: false,
                HasVersionManifestUrl: true
            }
            && !customPackagePathActive;
        var canUpdate = canCheck && (
            packageInfo == null
            || updateAvailable
            || !string.IsNullOrWhiteSpace(handler.PackageFailureMessage));
        var canCancel = handler.CanCancelDataOperation;

        const float textInset = 4f;
        var contentStartX = ImGui.GetCursorPosX();
        ImGui.SetCursorPosX(contentStartX + textInset);
        ImGui.TextColored(statusColour, statusTitle);
        ImGui.SameLine(contentStartX + 140);
        ImGui.TextWrapped(statusDetail);

        DrawDialogueStatusProgress(progressFraction, progressText, textInset);

        ImGui.SetCursorPosX(contentStartX);
        DrawFixedActionButton(
            $"Check for updates{suffix}CheckUpdates",
            new Vector2(150, 0),
            canCheck,
            handler.CheckForPackageUpdate);
        ImGui.SameLine();
        DrawFixedActionButton(
            $"Update data{suffix}UpdateData",
            new Vector2(120, 0),
            canUpdate,
            handler.DownloadPackageFromConfiguredUrl);
        ImGui.SameLine();
        DrawFixedActionButton(
            $"Cancel{suffix}CancelOperation",
            new Vector2(90, 0),
            canCancel,
            handler.ResetDataOperation);

        if (customPackagePathActive)
        {
            ImGui.SameLine();
            ImGui.TextColored(Ui.ColourWhiteDim, "Custom package path is active.");
        }

        const float bottomSpacer = 6f;
        ImGui.Dummy(new Vector2(1f, bottomSpacer));
    }

    private static void DrawDialogueStatusProgress(float fraction, string text, float textInset)
    {
        var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        var min = ImGui.GetCursorScreenPos();
        var max = min + size;
        var drawList = ImGui.GetWindowDrawList();
        var rounding = MathF.Min(4f, size.Y / 2f);
        drawList.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.09f)), rounding);
        if (fraction > 0)
        {
            var fillMax = new Vector2(min.X + (size.X * Math.Clamp(fraction, 0f, 1f)), max.Y);
            drawList.AddRectFilled(min, fillMax, ImGui.GetColorU32(DialogueStatusProgressFillColour), rounding);
        }

        drawList.AddText(
            new Vector2(min.X + textInset, min.Y + ((size.Y - ImGui.GetTextLineHeight()) / 2f)),
            ImGui.GetColorU32(Ui.ColourWhite),
            text);
        ImGui.Dummy(size);
    }

    private static (string Title, string Detail, Vector4 Colour) GetDialogueDataStatus(
        DialogueHandler handler,
        DialoguePackageInfo? packageInfo,
        DialogueRemotePackageInfo? remoteInfo,
        bool updateAvailable)
    {
        if (handler.IsDataOperationRunning)
        {
            var title = string.IsNullOrWhiteSpace(handler.OperationName) ? "Working" : handler.OperationName;
            var detail = DisplayOrDash(handler.PackageUpdateProgress.Status);
            return (title, detail, Ui.ColourCyan);
        }

        if (handler.IsPackageUpdateCheckRunning)
            return ("Checking", "Checking the configured package source.", Ui.ColourCyan);

        if (!string.IsNullOrWhiteSpace(handler.PackageFailureMessage))
            return ("Attention needed", handler.PackageFailureMessage, ImGuiColors.DalamudRed);

        if (packageInfo == null)
            return ("Not installed", "Download dialogue data before enabling NPC dialogue translation.",
                Ui.ColourYellow);

        if (updateAvailable && remoteInfo != null)
        {
            var detail = packageInfo.BuildNumber is null
                ? $"Latest build {remoteInfo.BuildNumber} is available."
                : $"Installed build {packageInfo.BuildNumber.Value}; latest build {remoteInfo.BuildNumber}.";
            return ("Update available", detail, Ui.ColourYellow);
        }

        if (!handler.IsStoreLoaded)
            return ("Installed", "Dialogue data is installed but not loaded.", Ui.ColourWhiteDim);

        return remoteInfo == null
            ? ("Ready", "Dialogue data is loaded. Check for updates to compare with the latest package.", Ui.ColourCyan)
            : ("Ready", "Dialogue data is loaded and up to date.", Ui.ColourCyan);
    }

    private static void DrawFixedActionButton(string label, Vector2 size, bool enabled, Action action)
    {
        if (!enabled) ImGui.BeginDisabled();
        if (ImGui.Button(label, size)) action();
        if (!enabled) ImGui.EndDisabled();
    }

    private static void DrawPackageSummaryTable(DialoguePackageInfo? packageInfo, DialogueRemotePackageInfo? remoteInfo)
    {
        if (!ImGui.BeginTable(
                "##BTTDialoguePackageSummary",
                2,
                ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX))
        {
            return;
        }

        ImGui.TableSetupColumn("Installed");
        ImGui.TableSetupColumn("Available");
        ImGui.TableHeadersRow();
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        DrawInstalledPackageSummary(packageInfo);
        ImGui.TableNextColumn();
        DrawRemotePackageSummary(remoteInfo);
        ImGui.EndTable();
    }

    private static void DrawInstalledPackageSummary(DialoguePackageInfo? packageInfo)
    {
        if (packageInfo == null)
        {
            ImGui.TextColored(Ui.ColourYellow, "Not installed");
            return;
        }

        DrawSummaryLine("Build", packageInfo.BuildNumber?.ToString() ?? "-");
        DrawSummaryLine("Game data", DisplayOrDash(packageInfo.GameVersion));
        DrawSummaryLine("Built", DisplayOrDash(FormatIsoUtc(packageInfo.BuiltAt)));
        DrawSummaryLine("Entries", packageInfo.EntryCount is null ? "-" : $"{packageInfo.EntryCount.Value:N0}");
    }

    private static void DrawRemotePackageSummary(DialogueRemotePackageInfo? remoteInfo)
    {
        if (remoteInfo == null)
        {
            ImGui.TextColored(Ui.ColourWhiteDim, "Not checked");
            return;
        }

        DrawSummaryLine("Build", remoteInfo.BuildNumber.ToString());
        DrawSummaryLine("Game data", remoteInfo.GameVersion);
        DrawSummaryLine("Built", FormatIsoUtc(remoteInfo.BuiltAt));
        DrawSummaryLine("Entries", $"{remoteInfo.EntryCount:N0}");
    }

    private static void DrawSummaryLine(string label, string value)
    {
        ImGui.TextColored(Ui.ColourWhiteDim, label);
        ImGui.SameLine(95);
        ImGui.TextWrapped(value);
    }

    private void DrawDialogueResourceAdvanced(
        string suffix,
        DialogueHandler handler,
        DialoguePackageInfo? packageInfo,
        DialogueRemotePackageInfo? remoteInfo,
        bool busy)
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.5f * ImGui.GetTextLineHeight()));
        if (!ImGui.CollapsingHeader($"Advanced{suffix}Advanced")) return;

        ImGui.TextColored(Ui.ColourCyan, "Package source");
        ImGui.TextColored(Ui.ColourWhiteDim, "Version manifest URL override");
        if (busy) ImGui.BeginDisabled();

        if (SettingsControls.DefaultedInputText(
                $"{suffix}VersionManifestUrl",
                ref _plugin.Config.TalkDialogueVersionManifestUrl,
                DefaultConfig.TalkDialogueVersionManifestUrl,
                1024,
                "Use default package source",
                -1).ValueChanged)
        {
            _plugin.Config.Save();
        }

        if (busy) ImGui.EndDisabled();

        if (!handler.HasVersionManifestUrl)
        {
            ImGui.TextColored(Ui.ColourYellow, "No package source is configured.");
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.4f * ImGui.GetTextLineHeight()));
        ImGui.TextColored(Ui.ColourWhiteDim, "Custom package path");
        if (busy) ImGui.BeginDisabled();

        if (SettingsControls.DefaultedInputText(
                $"{suffix}TalkDialogueDataPath",
                ref _plugin.Config.TalkDialogueDataPath,
                DefaultConfig.TalkDialogueDataPath,
                1024,
                "Use active installed package",
                -1).ValueChanged)
        {
            _plugin.Config.Save();
        }

        if (busy) ImGui.EndDisabled();

        ImGuiComponents.HelpMarker(
            "Optional. Leave empty to use the active user package. Override with a package manifest.json or package directory.");

        var canApplySource = !busy && !handler.IsPackageUpdateCheckRunning;
        DrawFixedActionButton(
            $"Apply source{suffix}ApplySource",
            new Vector2(120, 0),
            canApplySource,
            () =>
            {
                handler.ReloadStore();
                handler.CheckForPackageUpdate();
            });
        ImGuiComponents.HelpMarker(
            "Applies package source changes. Active dialogue views may reload data, and the selected source is checked for updates.");

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.7f * ImGui.GetTextLineHeight()));
        ImGui.TextColored(Ui.ColourCyan, "Maintenance");
        var canReload = !busy && packageInfo != null;
        DrawFixedActionButton($"Reload data{suffix}ReloadStore", new Vector2(110, 0), canReload, handler.ReloadStore);
        ImGui.SameLine();
        DrawFixedActionButton(
            $"Clear error{suffix}ClearError",
            new Vector2(100, 0),
            !busy && !string.IsNullOrWhiteSpace(handler.PackageFailureMessage),
            handler.ResetDataOperation);
        ImGui.SameLine();
        DrawFixedActionButton(
            $"Copy diagnostics{suffix}CopyDiagnostics",
            new Vector2(135, 0),
            true,
            () => ImGui.SetClipboardText(BuildDialogueDiagnostics(handler, packageInfo, remoteInfo)));

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (0.7f * ImGui.GetTextLineHeight()));
        ImGui.TextColored(Ui.ColourCyan, "Diagnostics");
        if (!ImGui.BeginTable(
                "##BTTDialogueDiagnostics",
                2,
                ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.PadOuterX,
                new Vector2(-1, 0)))
        {
            return;
        }

        ImGui.TableSetupColumn("Field", ImGuiTableColumnFlags.WidthFixed, 165);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

        DrawDiagnosticLine("Data status", handler.DataStatus);
        DrawDiagnosticLine("Package status", handler.PackageUpdateStatus);
        DrawDiagnosticLine("Operation", handler.OperationName);
        DrawDiagnosticLine("Loaded data", handler.DataPath);
        DrawDiagnosticLine("Active manifest", handler.ActivePackageManifestPath);
        if (packageInfo != null)
        {
            DrawDiagnosticLine("Installed manifest", packageInfo.ManifestPath);
            DrawDiagnosticLine("Built", packageInfo.BuiltAt);
        }

        if (remoteInfo != null)
        {
            DrawDiagnosticLine("Remote package", remoteInfo.PackagePath);
            DrawDiagnosticLine("Remote package hash", remoteInfo.PackageSha256);
            DrawDiagnosticLine("Built remote", remoteInfo.BuiltAt);
        }

        ImGui.EndTable();
    }

    private static void DrawDiagnosticLine(string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextColored(Ui.ColourWhiteDim, label);
        ImGui.TableNextColumn();
        var compactValue = CompactDiagnosticValue(value);
        const string sample = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<>/_-.";
        var averageCharWidth = ImGui.CalcTextSize(sample).X / sample.Length;
        var maxChars = Math.Max(96, (int)(ImGui.GetContentRegionAvail().X / Math.Max(1, averageCharWidth)));
        var display = label.Contains("hash", StringComparison.OrdinalIgnoreCase)
            ? compactValue
            : ShortenMiddle(compactValue, maxChars);
        ImGui.TextUnformatted(display);
        if (!string.Equals(display, value, StringComparison.Ordinal) && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(value);
        }
    }

    private string BuildDialogueDiagnostics(
        DialogueHandler handler,
        DialoguePackageInfo? packageInfo,
        DialogueRemotePackageInfo? remoteInfo)
    {
        var builder = new StringBuilder();
        builder.AppendLine("BilingualTooltips NPC dialogue diagnostics");
        builder.AppendLine($"Data status: {handler.DataStatus}");
        builder.AppendLine($"Package status: {handler.PackageUpdateStatus}");
        builder.AppendLine($"Operation: {DisplayOrDash(handler.OperationName)}");
        builder.AppendLine($"Loaded data: {DisplayOrDash(handler.DataPath)}");
        builder.AppendLine($"Active manifest: {handler.ActivePackageManifestPath}");
        builder.AppendLine($"Input language: {_plugin.Config.TalkDialogueInputLanguage}");
        builder.AppendLine($"Target language: {_plugin.Config.TalkDialogueTargetLanguage}");
        builder.AppendLine($"Render profile first name: {DisplayOrDash(_plugin.Config.TalkDialogueRenderProfileFirstName)}");
        builder.AppendLine($"Render profile last name: {DisplayOrDash(_plugin.Config.TalkDialogueRenderProfileLastName)}");
        builder.AppendLine($"Render profile gender: {_plugin.Config.TalkDialogueRenderProfileGender}");
        builder.AppendLine($"Enabled: {_plugin.Config.TalkDialogueEnabled}");
        builder.AppendLine($"Overlay enabled: {_plugin.Config.TalkDialogueOverlayEnabled}");
        builder.AppendLine($"Auto-open overlay: {_plugin.Config.TalkDialogueOpenWindowAutomatically}");
        builder.AppendLine($"Overlay shortcut enabled: {_plugin.Config.TalkDialogueOverlayShortcutEnabled}");
        builder.AppendLine($"Version manifest URL override: {DisplayOrDash(_plugin.Config.TalkDialogueVersionManifestUrl)}");
        builder.AppendLine($"Custom package path: {DisplayOrDash(_plugin.Config.TalkDialogueDataPath)}");

        if (packageInfo != null)
        {
            builder.AppendLine();
            builder.AppendLine("Installed package");
            builder.AppendLine($"Manifest: {packageInfo.ManifestPath}");
            builder.AppendLine($"Build: {packageInfo.BuildNumber?.ToString() ?? "-"}");
            builder.AppendLine($"Game data: {DisplayOrDash(packageInfo.GameVersion)}");
            builder.AppendLine($"Entries: {packageInfo.EntryCount?.ToString() ?? "-"}");
            builder.AppendLine($"Built: {DisplayOrDash(packageInfo.BuiltAt)}");
        }

        if (remoteInfo != null)
        {
            builder.AppendLine();
            builder.AppendLine("Available package");
            builder.AppendLine($"Build: {remoteInfo.BuildNumber}");
            builder.AppendLine($"Game data: {remoteInfo.GameVersion}");
            builder.AppendLine($"Entries: {remoteInfo.EntryCount}");
            builder.AppendLine($"Package: {remoteInfo.PackagePath}");
            builder.AppendLine($"Package hash: {remoteInfo.PackageSha256}");
            builder.AppendLine($"Built: {remoteInfo.BuiltAt}");
        }

        return builder.ToString();
    }

    private static string CompactDiagnosticValue(string value) =>
        TryCompactPath(value, GetPluginConfigsDirectory(), "<pluginConfigs>")
        ?? value;

    private static string GetPluginConfigsDirectory() =>
        Directory.GetParent(Service.PluginInterface.GetPluginConfigDirectory())?.FullName
        ?? Service.PluginInterface.GetPluginConfigDirectory();

    private static string? TryCompactPath(string value, string root, string alias)
    {
        try
        {
            var fullRoot = Path.GetFullPath(root);
            var fullValue = Path.GetFullPath(value);
            if (string.Equals(
                    fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    fullValue.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
            {
                return alias;
            }

            var rootWithSeparator = fullRoot.EndsWith(Path.DirectorySeparatorChar)
                ? fullRoot
                : fullRoot + Path.DirectorySeparatorChar;

            return fullValue.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
                ? $"{alias}/{Path.GetRelativePath(fullRoot, fullValue).Replace('\\', '/')}"
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ShortenMiddle(string value, int maxChars)
    {
        if (value.Length <= maxChars) return value;
        if (maxChars < 12) return value[..maxChars];

        var prefixLength = (maxChars - 3) / 2;
        var suffixLength = maxChars - prefixLength - 3;
        return $"{value[..prefixLength]}...{value[^suffixLength..]}";
    }

    private static string FormatIsoUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "-";
        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
            : value;
    }

    private static string DisplayOrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static string FormatBytes(long value)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)value;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }
}
