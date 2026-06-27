using System.IO;
using BilingualTooltips.Modules.Dialogue.Data;

namespace BilingualTooltips.Modules.Dialogue;

public sealed class DialogueStoreProvider : IDisposable
{
    public const string TalkRuntimeConsumer = "talk-runtime";
    public const string LookupHistoryConsumer = "lookup-history";
    public const string GeneralShowcaseConsumer = "general-showcase";

    private readonly BilingualTooltipsPlugin _plugin;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, DialogueShardRequirement> _requirements = new(StringComparer.Ordinal);

    private DialogueStore? _store;
    private string _storeManifestPath = "";
    private DateTime _storeManifestLastWriteUtc;
    private int _version;
    private int _preparedVersion = -1;
    private int _generation;
    private Task? _prepareTask;
    private CancellationTokenSource? _prepareCancellationTokenSource;
    private volatile string _status = "Not loaded";
    private volatile bool _disposed;

    public DialogueStoreProvider(BilingualTooltipsPlugin plugin)
    {
        _plugin = plugin;
    }

    public string Status => _status;
    public int Generation => Volatile.Read(ref _generation);
    public string PackageSummary => GetLoadedStore()?.PackageSummary ?? "";
    public string DataPath => GetLoadedStore()?.DataPath ?? "";
    public bool IsStoreLoaded => GetLoadedStore() != null;

    public bool IsPreparing
    {
        get
        {
            lock (_lock)
            {
                return _prepareTask is { IsCompleted: false };
            }
        }
    }

    public void SetRequirement(string consumer, DialogueShardRequirement requirement)
    {
        lock (_lock)
        {
            if (requirement.RequiresStore)
            {
                if (_requirements.TryGetValue(consumer, out var currentRequirement) && currentRequirement.Equals(requirement))
                    return;

                _requirements[consumer] = requirement;
            }
            else
            {
                if (!_requirements.Remove(consumer)) return;
            }

            OnRequirementsChangedLocked();
        }

        StartPrepareIfNeeded();
    }

    public void ClearRequirement(string consumer)
    {
        lock (_lock)
        {
            if (!_requirements.Remove(consumer)) return;

            _version++;
            _prepareCancellationTokenSource?.Cancel();
            if (_requirements.Count == 0)
            {
                DropStoreLocked("Not loaded");
                _preparedVersion = _version;
            }
        }

        StartPrepareIfNeeded();
    }

    public void Invalidate()
    {
        lock (_lock)
        {
            _version++;
            _preparedVersion = _requirements.Count == 0 ? _version : -1;
            _prepareCancellationTokenSource?.Cancel();
            DropStoreLocked("Not loaded");
        }

        StartPrepareIfNeeded();
    }

    public bool TryGetStore(DialogueShardRequirement requirement, out DialogueStore store)
    {
        lock (_lock)
        {
            if (_store != null && requirement.IsSatisfiedBy(_store))
            {
                store = _store;
                return true;
            }
        }

        StartPrepareIfNeeded();
        store = null!;
        return false;
    }

    public void Dispose()
    {
        _disposed = true;
        lock (_lock)
        {
            _prepareCancellationTokenSource?.Cancel();
            _requirements.Clear();
            DropStoreLocked("Not loaded");
        }
    }

    private DialogueStore? GetLoadedStore()
    {
        lock (_lock)
        {
            return _store;
        }
    }

    private void StartPrepareIfNeeded()
    {
        if (_disposed) return;

        lock (_lock)
        {
            if (_prepareTask is { IsCompleted: false }) return;
            if (_preparedVersion == _version) return;

            _prepareCancellationTokenSource?.Dispose();
            _prepareCancellationTokenSource = new CancellationTokenSource();
            var cancellationTokenSource = _prepareCancellationTokenSource;
            _prepareTask = Task.Run(() => PrepareLoop(cancellationTokenSource.Token), cancellationTokenSource.Token);
            _prepareTask.ContinueWith(
                _ => StartPrepareIfNeeded(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private void PrepareLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && !_disposed)
            {
                DialogueShardRequirement requirement;
                DialogueStore? currentStore;
                string currentManifestPath;
                DateTime currentManifestLastWriteUtc;
                int version;

                lock (_lock)
                {
                    version = _version;
                    requirement = DialogueShardRequirement.Merge(_requirements.Values);
                    if (!requirement.RequiresStore)
                    {
                        DropStoreLocked("Not loaded");
                        _preparedVersion = version;
                        return;
                    }

                    currentStore = _store;
                    currentManifestPath = _storeManifestPath;
                    currentManifestLastWriteUtc = _storeManifestLastWriteUtc;
                    _status = currentStore == null ? "Loading data" : "Preparing data";
                }

                if (!DialogueStore.TryResolveDataPath(_plugin.Config.TalkDialogueDataPath, out var dataPath, out var reason))
                {
                    lock (_lock)
                    {
                        if (version != _version) continue;

                        DropStoreLocked(reason);
                        _preparedVersion = version;
                    }

                    return;
                }

                var manifestPath = Path.GetFullPath(dataPath);
                var manifestLastWriteUtc = File.GetLastWriteTimeUtc(manifestPath);
                var store = currentStore != null
                    && string.Equals(currentManifestPath, manifestPath, StringComparison.OrdinalIgnoreCase)
                    && currentManifestLastWriteUtc == manifestLastWriteUtc
                        ? currentStore
                        : new DialogueStore(manifestPath);

                cancellationToken.ThrowIfCancellationRequested();
                store.PrepareShards(requirement, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                lock (_lock)
                {
                    if (version != _version) continue;

                    store.TrimTo(requirement);
                    _store = store;
                    _storeManifestPath = manifestPath;
                    _storeManifestLastWriteUtc = manifestLastWriteUtc;
                    _status = $"Loaded {store.EntryCount:N0} entries";
                    _preparedVersion = version;
                    AdvanceGenerationLocked();
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when requirements change or the plugin is disposed.
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                DropStoreLocked("Failed to load dialogue data");
                _preparedVersion = _version;
            }

            Service.Log.Warning(ex, "[DialogueStoreProvider] Failed to prepare dialogue store");
        }
    }

    private void OnRequirementsChangedLocked()
    {
        _version++;
        _prepareCancellationTokenSource?.Cancel();
        if (_requirements.Count == 0)
        {
            DropStoreLocked("Not loaded");
            _preparedVersion = _version;
        }
    }

    private void DropStoreLocked(string status)
    {
        _store = null;
        _storeManifestPath = "";
        _storeManifestLastWriteUtc = default;
        _status = status;
        AdvanceGenerationLocked();
    }

    private void AdvanceGenerationLocked()
    {
        Interlocked.Increment(ref _generation);
    }
}
