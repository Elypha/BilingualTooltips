namespace BilingualTooltips.Modules.Dialogue.Package;

public sealed class DialoguePackageUpdateProgress
{
    private readonly Lock _lock = new();
    private string _status = "Idle";
    private int _buildNumber;
    private string _gameVersion = "";
    private int _entryCount;
    private long _bytesDownloaded;
    private long? _totalBytes;

    public string Status
    {
        get
        {
            lock (_lock) return _status;
        }
        set
        {
            lock (_lock) _status = value;
        }
    }

    public int BuildNumber
    {
        get
        {
            lock (_lock) return _buildNumber;
        }
        set
        {
            lock (_lock) _buildNumber = value;
        }
    }

    public string GameVersion
    {
        get
        {
            lock (_lock) return _gameVersion;
        }
        set
        {
            lock (_lock) _gameVersion = value;
        }
    }

    public int EntryCount
    {
        get
        {
            lock (_lock) return _entryCount;
        }
        set
        {
            lock (_lock) _entryCount = value;
        }
    }

    public long BytesDownloaded
    {
        get
        {
            lock (_lock) return _bytesDownloaded;
        }
        set
        {
            lock (_lock) _bytesDownloaded = value;
        }
    }

    public long? TotalBytes
    {
        get
        {
            lock (_lock) return _totalBytes;
        }
        set
        {
            lock (_lock) _totalBytes = value;
        }
    }

    public float Fraction
    {
        get
        {
            lock (_lock)
            {
                return _totalBytes is not > 0
                    ? 0
                    : Math.Clamp((float)((double)_bytesDownloaded / _totalBytes.Value), 0, 1);
            }
        }
    }

    public void Reset(string status)
    {
        lock (_lock)
        {
            _status = status;
            _buildNumber = 0;
            _gameVersion = "";
            _entryCount = 0;
            _bytesDownloaded = 0;
            _totalBytes = null;
        }
    }
}
