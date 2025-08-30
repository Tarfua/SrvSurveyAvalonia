namespace SrvSurvey.Core;

public sealed class JournalWatcher : IDisposable
{
    private readonly string _folder;
    private FileSystemWatcher? _watcher;
    public event Action<string>? Changed;

    public JournalWatcher(string folder)
    {
        _folder = folder;
    }

    public void Start()
    {
        Stop();
        _watcher = new FileSystemWatcher(_folder, "Journal*.log");
        _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.EnableRaisingEvents = true;
        Logging.Info($"JournalWatcher started: {_folder}");
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        Changed?.Invoke(e.FullPath);
    }

    public void Stop()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnChanged;
            _watcher.Created -= OnChanged;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}


