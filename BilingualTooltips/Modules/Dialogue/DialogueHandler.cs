using BilingualTooltips.Modules.Dialogue.Data;
using BilingualTooltips.Modules.Dialogue.Package;
using BilingualTooltips.Modules.Lookup;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace BilingualTooltips.Modules.Dialogue;

public sealed class DialogueHandler : IDisposable
{
    private const string TalkAddonName = "Talk";

    private const string DefaultVersionManifestUrl =
        "https://ffxiv.elypha.com/assets/bilingualtooltips/latest.json";

    private readonly BilingualTooltipsPlugin _plugin;
    private readonly DialoguePackageUpdater _packageUpdater = new();

    private readonly Lock _operationLock = new();
    private readonly Lock _updateCheckLock = new();

    private volatile DialogueTranslationState _state = new();
    private volatile string _dataStatus = "Not loaded";
    private volatile string _operationName = "";
    private volatile string _packageUpdateStatus = "Not checked";
    private volatile string _packageFailureMessage = "";
    private volatile bool _disposed;
    private volatile DialogueRemotePackageInfo? _remotePackageInfo;

    private string _lastSpeakerName = "";
    private string _lastText = "";
    private long _requestSequence;
    private int _observedStoreGeneration;
    private bool _talkAddonVisible;

    private Task? _operationTask;
    private Task? _updateCheckTask;
    private CancellationTokenSource? _operationCancellationTokenSource;
    private bool _unloadAfterCurrentOperation;

    public DialogueTranslationState State => _state;
    public string DataStatus => IsDataOperationRunning ? _dataStatus : _plugin.DialogueStoreProvider.Status;
    public string OperationName => _operationName;
    public string PackageUpdateStatus => _packageUpdateStatus;
    public string PackageFailureMessage => _packageFailureMessage;
    public string PackageSummary => _plugin.DialogueStoreProvider.PackageSummary;
    public string DataPath => _plugin.DialogueStoreProvider.DataPath;
    public bool IsStoreLoaded => _plugin.DialogueStoreProvider.IsStoreLoaded;

    public bool IsDataOperationRunning
    {
        get
        {
            lock (_operationLock)
            {
                return _operationTask is { IsCompleted: false };
            }
        }
    }

    public bool IsPackageUpdateCheckRunning
    {
        get
        {
            lock (_updateCheckLock)
            {
                return _updateCheckTask is { IsCompleted: false };
            }
        }
    }

    public bool CanCancelDataOperation
    {
        get
        {
            lock (_operationLock)
            {
                return _operationCancellationTokenSource is { IsCancellationRequested: false }
                    && _operationTask is { IsCompleted: false };
            }
        }
    }

    public bool CanChangeTalkDialogueEnabled => !IsDataOperationRunning;
    public DialoguePackageUpdateProgress PackageUpdateProgress => _packageUpdater.Progress;
    public DialoguePackageInfo? ActivePackageInfo => DialogueStore.TryReadPackageInfo(_plugin.Config.TalkDialogueDataPath);
    public DialogueRemotePackageInfo? RemotePackageInfo => _remotePackageInfo;
    public string ActivePackageManifestPath => DialoguePaths.UserActivePackageManifestPath;

    public string EffectiveVersionManifestUrl
    {
        get
        {
            var overrideUrl = _plugin.Config.TalkDialogueVersionManifestUrl.Trim();
            return string.IsNullOrWhiteSpace(overrideUrl) ? DefaultVersionManifestUrl : overrideUrl;
        }
    }

    public bool HasVersionManifestUrl => !string.IsNullOrWhiteSpace(EffectiveVersionManifestUrl);

    public DialogueHandler(BilingualTooltipsPlugin plugin)
    {
        _plugin = plugin;
    }

    // state publication
    // --------------------------------
    private void PublishState(DialogueTranslationState state) => _state = state;

    private void PublishSearchingState(string speakerName, string text)
    {
        PublishState(new DialogueTranslationState
        {
            Status = DialogueTranslationStatus.Searching,
            SpeakerName = speakerName,
            SourceText = text,
        });
    }

    private void PublishLoadingState()
    {
        PublishState(new DialogueTranslationState
        {
            Status = DialogueTranslationStatus.Loading,
        });
    }

    private void PublishIdleState()
    {
        PublishState(new DialogueTranslationState
        {
            Status = DialogueTranslationStatus.Idle,
        });
    }

    // plugin lifecycle and Talk requirements
    // --------------------------------
    public void StartHook()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreUpdate, TalkAddonName, TalkVisibilityHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, TalkAddonName, TalkDrawHandler);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, TalkAddonName, TalkFinalizeHandler);
        CleanStalePackageStaging();
        CheckForPackageUpdate();

        if (_plugin.Config.TalkDialogueEnabled)
        {
            LoadStore();
        }
    }

    public void OnFrameUpdate()
    {
        if (_disposed) return;
        if (!_plugin.Config.TalkDialogueEnabled) return;

        var generation = _plugin.DialogueStoreProvider.Generation;
        if (generation == _observedStoreGeneration) return;
        _observedStoreGeneration = generation;

        if (!TryCreateCurrentTalkTranslationRequest(out var sequence, out var speakerName, out var text))
            return;

        PublishSearchingState(speakerName, text);
        _ = Task.Run(() => UpdateTranslation(sequence, speakerName, text));
    }

    public void StopHook()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreUpdate, TalkAddonName, TalkVisibilityHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, TalkAddonName, TalkDrawHandler);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, TalkAddonName, TalkFinalizeHandler);
        _talkAddonVisible = false;
    }

    public void Dispose()
    {
        _disposed = true;
        StopHook();
        lock (_operationLock)
        {
            _unloadAfterCurrentOperation = false;
            _operationCancellationTokenSource?.Cancel();
        }
    }

    public void OnTalkDialogueEnabledChanged(bool enabled)
    {
        if (enabled)
        {
            LoadStore();
            return;
        }

        _talkAddonVisible = false;
        _plugin.DialogueTranslationWindow.IsOpen = false;
        UnloadStore();
    }

    public void LoadStore()
    {
        _observedStoreGeneration = _plugin.DialogueStoreProvider.Generation;
        SetTalkRequirement();
        if (TryCreateCurrentTalkTranslationRequest(out var sequence, out var speakerName, out var text))
        {
            PublishSearchingState(speakerName, text);
            UpdateTranslation(sequence, speakerName, text);
            return;
        }

        PublishLoadingState();
    }

    public void ReloadStore()
    {
        _plugin.DialogueStoreProvider.Invalidate();
        if (_plugin.Config.TalkDialogueEnabled)
        {
            LoadStore();
            return;
        }

        ClearTalkRequirement("Not loaded");
    }

    public void OnTalkDialogueLanguagesChanged()
    {
        if (!_plugin.Config.TalkDialogueEnabled) return;

        SetTalkRequirement();
        if (!TryCreateCurrentTalkTranslationRequest(out var sequence, out var speakerName, out var text)) return;
        PublishSearchingState(speakerName, text);
        UpdateTranslation(sequence, speakerName, text);
    }

    public void UnloadStore()
    {
        lock (_operationLock)
        {
            if (_operationTask is { IsCompleted: false })
            {
                _unloadAfterCurrentOperation = true;
                _dataStatus = "Unload pending";
                return;
            }
        }

        ClearTalkRequirement("Not loaded");
    }

    // package resources
    // --------------------------------
    public void ResetDataOperation()
    {
        lock (_operationLock)
        {
            if (_operationTask is not { IsCompleted: false })
            {
                _packageFailureMessage = "";
                _packageUpdateStatus = "Idle";
                _packageUpdater.Progress.Reset("Idle");
                return;
            }

            _unloadAfterCurrentOperation = false;
            _operationCancellationTokenSource?.Cancel();
            _dataStatus = $"Cancelling {_operationName}";
            _packageUpdateStatus = "Cancelling";
            _packageFailureMessage = "";
        }
    }

    public void DownloadPackageFromConfiguredUrl()
    {
        var versionManifestUrl = EffectiveVersionManifestUrl;
        if (string.IsNullOrWhiteSpace(versionManifestUrl))
        {
            _dataStatus = "No package source configured";
            RequestResourcePanelOpen();
            return;
        }

        _ = StartExclusiveOperation("Updating package", async cancellationToken =>
        {
            _packageFailureMessage = "";
            _dataStatus = "Downloading package";
            var result = await _packageUpdater.DownloadToActivePackageAsync(versionManifestUrl, cancellationToken);
            Service.Log.Info($"[DialogueHandler] {result}");
            _packageUpdateStatus = result;
            _plugin.DialogueStoreProvider.Invalidate();

            if (_plugin.Config.TalkDialogueEnabled)
                SetTalkRequirement();
            else
                ClearTalkRequirement(result);
        });
    }

    public void CheckForPackageUpdate()
    {
        if (_disposed) return;

        if (string.IsNullOrWhiteSpace(EffectiveVersionManifestUrl))
        {
            _packageUpdateStatus = "No package source configured";
            return;
        }

        if (IsPackageUpdateCheckRunning) return;

        lock (_updateCheckLock)
        {
            if (_updateCheckTask is { IsCompleted: false })
                return;

            _updateCheckTask = Task.Run(CheckForPackageUpdateCore);
        }
    }

    private async Task CheckForPackageUpdateCore()
    {
        try
        {
            if (_disposed) return;

            _packageFailureMessage = "";
            if (!string.IsNullOrWhiteSpace(_plugin.Config.TalkDialogueDataPath))
            {
                _packageUpdateStatus = "Custom dialogue package path active";
                return;
            }

            _packageUpdateStatus = "Checking for dialogue package updates";
            var remote = await _packageUpdater.FetchRemotePackageInfoAsync(EffectiveVersionManifestUrl);
            if (_disposed) return;

            _remotePackageInfo = remote;

            var active = ActivePackageInfo;
            if (active == null)
            {
                _packageUpdateStatus = "No dialogue package installed";
                RequestResourcePanelOpen();
                return;
            }

            if (active.BuildNumber == null)
            {
                _packageUpdateStatus = "Installed dialogue package has no build number";
                RequestResourcePanelOpen();
                return;
            }

            if (remote.BuildNumber > active.BuildNumber.Value)
            {
                _packageUpdateStatus = $"Update available: build {active.BuildNumber.Value} -> {remote.BuildNumber}";
                RequestResourcePanelOpen();
                return;
            }

            _packageUpdateStatus = $"Up to date: build {active.BuildNumber.Value}";
        }
        catch (Exception ex)
        {
            _packageUpdateStatus = "Dialogue package update check failed";
            _packageFailureMessage = "Update check failed. Check the plugin log for details.";
            Service.Log.Warning(ex, "[DialogueHandler] Dialogue package update check failed");
        }
    }

    private bool StartExclusiveOperation(string name, Func<CancellationToken, Task> operation)
    {
        if (_disposed) return false;

        lock (_operationLock)
        {
            if (_disposed) return false;

            if (_operationTask is { IsCompleted: false })
            {
                _dataStatus = $"Busy: {_operationName}";
                return false;
            }

            _operationName = name;
            _dataStatus = name;
            _operationCancellationTokenSource?.Dispose();
            _operationCancellationTokenSource = new CancellationTokenSource();
            var cancellationTokenSource = _operationCancellationTokenSource;
            // The wrapper must always run its finally cleanup; cancellation belongs inside operation().
            _operationTask = Task.Run(
                async () =>
                {
                    try
                    {
                        await operation(cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
                    {
                        CleanStalePackageStaging();
                        _dataStatus = $"{name} cancelled";
                        _packageUpdateStatus = "Cancelled";
                        _packageFailureMessage = "";
                        _packageUpdater.Progress.Reset("Cancelled");
                    }
                    catch (Exception ex)
                    {
                        _dataStatus = $"{name} failed";
                        _packageFailureMessage = $"{name} failed. Check the plugin log for details.";
                        PublishState(new DialogueTranslationState
                        {
                            Status = DialogueTranslationStatus.Error,
                            SourceText = State.SourceText,
                            SpeakerName = State.SpeakerName,
                        });
                        Service.Log.Error(ex, $"[DialogueHandler] {name} failed");
                        if (!_disposed)
                        {
                            RequestResourcePanelOpen();
                        }
                    }
                    finally
                    {
                        lock (_operationLock)
                        {
                            var shouldUnload = _unloadAfterCurrentOperation;
                            _unloadAfterCurrentOperation = false;
                            _operationName = "";
                            if (ReferenceEquals(_operationCancellationTokenSource, cancellationTokenSource))
                            {
                                _operationCancellationTokenSource = null;
                            }

                            if (shouldUnload)
                            {
                                ClearTalkRequirement("Not loaded");
                            }
                        }

                        cancellationTokenSource.Dispose();
                    }
                },
                CancellationToken.None);

            return true;
        }
    }

    // Talk addon callbacks
    // --------------------------------
    private unsafe void TalkVisibilityHandler(AddonEvent type, AddonArgs args)
    {
        if (!_plugin.Config.TalkDialogueEnabled)
        {
            _talkAddonVisible = false;
            return;
        }

        var addon = (AtkUnitBase*)args.Addon.Address;
        var isVisible = addon != null && addon->IsVisible;
        if (_talkAddonVisible
            && !isVisible
            && _plugin.Config is { TalkDialogueOverlayEnabled: true, TalkDialogueOpenWindowAutomatically: true })
        {
            _plugin.DialogueTranslationWindow.IsOpen = false;
        }

        _talkAddonVisible = isVisible;
    }

    private void TalkFinalizeHandler(AddonEvent type, AddonArgs args)
    {
        _talkAddonVisible = false;
        if (_plugin.Config is
            {
                TalkDialogueEnabled: true,
                TalkDialogueOverlayEnabled: true,
                TalkDialogueOpenWindowAutomatically: true
            })
        {
            _plugin.DialogueTranslationWindow.IsOpen = false;
        }
    }

    private unsafe void TalkDrawHandler(AddonEvent type, AddonArgs args)
    {
        if (!_plugin.Config.TalkDialogueEnabled) return;

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null || !addon->IsVisible) return;
        _talkAddonVisible = true;

        var nameNode = addon->GetTextNodeById(2);
        var textNode = addon->GetTextNodeById(3);
        if (nameNode == null || textNode == null) return;

        var rawSpeakerName = nameNode->NodeText.ToString();
        var rawText = MemoryHelper.ReadSeStringAsString(out _, (nint)textNode->NodeText.StringPtr.Value);
        var speakerName = DialogueText.NormaliseRenderedText(rawSpeakerName);
        var text = DialogueText.NormaliseRenderedText(rawText);
        if (string.IsNullOrWhiteSpace(text)) return;

        if (speakerName == _lastSpeakerName && text == _lastText) return;
        _lastSpeakerName = speakerName;
        _lastText = text;

        var sequence = Interlocked.Increment(ref _requestSequence);
        PublishSearchingState(speakerName, text);

        if (_plugin.Config is { TalkDialogueOverlayEnabled: true, TalkDialogueOpenWindowAutomatically: true })
        {
            _plugin.DialogueTranslationWindow.IsOpen = true;
        }

        _ = Task.Run(() => UpdateTranslation(sequence, speakerName, text));
    }

    // translation pipeline
    // --------------------------------
    private void UpdateTranslation(long sequence, string speakerName, string text)
    {
        var requirement = DialogueShardRequirement.ForTalk(
            _plugin.Config.TalkDialogueInputLanguage,
            _plugin.Config.TalkDialogueTargetLanguage);
        if (!_plugin.DialogueStoreProvider.TryGetStore(requirement, out var dialogueStore))
        {
            if (sequence != Interlocked.Read(ref _requestSequence)) return;

            PublishState(new DialogueTranslationState
            {
                Status = _plugin.DialogueStoreProvider.IsPreparing
                    ? DialogueTranslationStatus.Loading
                    : DialogueTranslationStatus.NoData,
                SpeakerName = speakerName,
                SourceText = text,
            });

            if (_plugin.Config.TalkDialogueEnabled)
            {
                SetTalkRequirement();
            }

            return;
        }

        if (sequence != Interlocked.Read(ref _requestSequence)) return;

        var inputLanguage = _plugin.Config.TalkDialogueInputLanguage.ResolveInput();
        var targetLanguage = _plugin.Config.TalkDialogueTargetLanguage;
        var match = dialogueStore.Match(text, inputLanguage, targetLanguage);

        if (sequence != Interlocked.Read(ref _requestSequence)) return;

        if (match == null)
        {
            PublishState(new DialogueTranslationState
            {
                Status = DialogueTranslationStatus.NotFound,
                SpeakerName = speakerName,
                SourceText = text,
            });
            return;
        }

        PublishState(new DialogueTranslationState
        {
            Status = DialogueTranslationStatus.Ok,
            SpeakerName = speakerName,
            SourceText = text,
            TargetTemplate = match.TargetTemplate,
            TargetText = RenderTargetTemplate(match.TargetTemplate),
            MatchKind = match.MatchKind,
            Key = match.Key,
        });
        _plugin.RecordLookup(
            BttLookupKey.NpcDialogue(match.Key),
            string.IsNullOrWhiteSpace(speakerName) ? "Unnamed NPC" : speakerName);
    }

    public void RefreshCurrentTranslationRendering()
    {
        var state = State;
        if (state.Status != DialogueTranslationStatus.Ok) return;

        PublishState(new DialogueTranslationState
        {
            Status = state.Status,
            SpeakerName = state.SpeakerName,
            SourceText = state.SourceText,
            TargetTemplate = state.TargetTemplate,
            TargetText = RenderTargetTemplate(state.TargetTemplate),
            MatchKind = state.MatchKind,
            Key = state.Key,
        });
    }

    private string RenderTargetTemplate(DialogueTemplate targetTemplate) =>
        targetTemplate.Render(DialogueRenderProfile.FromConfig(_plugin.Config));

    private void SetTalkRequirement()
    {
        if (!_plugin.Config.TalkDialogueEnabled)
        {
            ClearTalkRequirement("Not loaded");
            return;
        }

        _plugin.DialogueStoreProvider.SetRequirement(
            DialogueStoreProvider.TalkRuntimeConsumer,
            DialogueShardRequirement.ForTalk(
                _plugin.Config.TalkDialogueInputLanguage,
                _plugin.Config.TalkDialogueTargetLanguage));
    }

    private void ClearTalkRequirement(string status)
    {
        _observedStoreGeneration = _plugin.DialogueStoreProvider.Generation;
        _plugin.DialogueStoreProvider.ClearRequirement(DialogueStoreProvider.TalkRuntimeConsumer);
        Interlocked.Increment(ref _requestSequence);
        _dataStatus = status;
        PublishIdleState();
        _lastSpeakerName = "";
        _lastText = "";
    }

    private bool TryCreateCurrentTalkTranslationRequest(out long sequence, out string speakerName, out string text)
    {
        sequence = 0;
        speakerName = _lastSpeakerName;
        text = _lastText;

        if (!_plugin.Config.TalkDialogueEnabled
            || !_talkAddonVisible
            || string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        sequence = Interlocked.Increment(ref _requestSequence);
        return true;
    }

    // shared helpers
    // --------------------------------
    private void RequestResourcePanelOpen()
    {
        if (_disposed || !_plugin.Config.TalkDialogueEnabled) return;

        try
        {
            _plugin.ConfigWindow.OpenDialogueResources();
        }
        catch (Exception ex)
        {
            Service.Log.Debug(ex, "[DialogueHandler] Failed to open dialogue resources panel");
        }
    }

    private void CleanStalePackageStaging()
    {
        try
        {
            _packageUpdater.CleanStagingPackage();
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, "[DialogueHandler] Failed to clean stale dialogue package staging directory");
        }
    }
}
