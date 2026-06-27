using System.IO;
using System.Text.Json;

namespace BilingualTooltips.Modules.Lookup;

public sealed class BttLookupHistoryStore
{
    private const string FileName = "lookup-history.json";
    private static readonly TimeSpan SaveDebounce = TimeSpan.FromSeconds(2);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Lock _lock = new();
    private readonly string _path;
    private readonly List<BttLookupHistoryEntry> _entries = [];
    private bool _dirty;
    private DateTimeOffset _saveDueAt;

    public BttLookupHistoryStore()
    {
        _path = Path.Combine(Service.PluginInterface.GetPluginConfigDirectory(), FileName);
        Load();
    }

    private void Load()
    {
        lock (_lock)
        {
            _entries.Clear();
            _dirty = false;
            if (!File.Exists(_path)) return;

            try
            {
                var loaded = JsonSerializer.Deserialize<List<BttLookupHistoryEntry>>(File.ReadAllText(_path), JsonOptions);
                if (loaded == null) return;

                _entries.AddRange(loaded
                    .Where(static entry => entry.Key.IsValid && !string.IsNullOrWhiteSpace(entry.DisplayName))
                    .OrderByDescending(static entry => entry.LastSeenAt));
            }
            catch (Exception ex)
            {
                Service.Log.Warning(ex, "[LookupHistory] Failed to load lookup history");
            }
        }
    }

    public IReadOnlyList<BttLookupHistoryEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries
                    .Select(static entry => new BttLookupHistoryEntry
                    {
                        Key = entry.Key,
                        DisplayName = entry.DisplayName,
                        LastSeenAt = entry.LastSeenAt,
                        Count = entry.Count,
                    })
                    .ToArray();
            }
        }
    }

    public void Record(BttLookupKey key, string displayName, int limit)
    {
        if (!key.IsValid) return;

        displayName = NormaliseDisplayName(displayName);
        if (string.IsNullOrWhiteSpace(displayName)) return;

        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var existingIndex = _entries.FindIndex(entry => entry.Key == key);
            switch (existingIndex)
            {
                case 0:
                {
                    var existing = _entries[0];
                    existing.LastSeenAt = now;
                    existing.Count++;
                    existing.DisplayName = displayName;
                    ScheduleSaveCore(now);
                    return;
                }
                case >= 0:
                {
                    var existing = _entries[existingIndex];
                    existing.LastSeenAt = now;
                    existing.Count++;
                    existing.DisplayName = displayName;
                    _entries.RemoveAt(existingIndex);
                    _entries.Insert(0, existing);
                    break;
                }
                default:
                    _entries.Insert(0, new BttLookupHistoryEntry
                    {
                        Key = key,
                        DisplayName = displayName,
                        LastSeenAt = now,
                        Count = 1,
                    });
                    break;
            }

            PruneCore(limit);
            ScheduleSaveCore(now);
        }
    }

    private static string NormaliseDisplayName(string displayName) => displayName
        .Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace('\r', '\n')
        .Split('\n', 2)[0]
        .Trim();

    public void Prune(int limit)
    {
        lock (_lock)
        {
            if (!PruneCore(limit)) return;

            SaveNowCore();
        }
    }

    private bool PruneCore(int limit)
    {
        if (_entries.Count <= limit) return false;

        _entries.RemoveRange(limit, _entries.Count - limit);
        return true;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
            SaveNowCore();
        }
    }

    public void FlushIfDue()
    {
        lock (_lock)
        {
            if (!_dirty || DateTimeOffset.UtcNow < _saveDueAt) return;

            SaveNowCore();
        }
    }

    public void Flush()
    {
        lock (_lock)
        {
            if (_dirty) SaveNowCore();
        }
    }

    private void ScheduleSaveCore(DateTimeOffset now)
    {
        _dirty = true;
        _saveDueAt = now + SaveDebounce;
    }

    private void SaveNowCore()
    {
        SaveCore();
        _dirty = false;
    }

    private void SaveCore()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? "");
            File.WriteAllText(_path, JsonSerializer.Serialize(_entries, JsonOptions));
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, "[LookupHistory] Failed to save lookup history");
        }
    }
}
