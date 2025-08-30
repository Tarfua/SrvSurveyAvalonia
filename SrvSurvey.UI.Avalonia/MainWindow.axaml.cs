using Avalonia.Controls;
using Avalonia.Interactivity;
using SrvSurvey.Core;
using System.Linq;
using System.IO;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Threading;
using System;
using Avalonia.Media;

namespace SrvSurvey.UI.Avalonia;

public partial class MainWindow : Window
{
    private TextBox? _logsBox;
    private JournalWatcher? _watcher;
    private TextBlock? _txtStatus;
    private TextBlock? _txtTime;
    private TextBlock? _txtConnection;
    private TextBlock? _txtSystem;
    private TextBlock? _txtBody;
    private TextBlock? _txtLatitude;
    private TextBlock? _txtLongitude;
    private ScrollViewer? _logScrollViewer;
    private GameState _state = new GameState();
    private JournalProcessor? _processor;
    private Timer? _timeTimer;
    public MainWindow()
    {
        InitializeComponent();
        InitializeControls();
        SetupEventHandlers();
        StartTimeTimer();
        TryStartWatcher();
    }
    
    private void InitializeControls()
    {
        _logsBox = this.FindControl<TextBox>("Logs");
        _txtStatus = this.FindControl<TextBlock>("TxtStatus");
        _txtTime = this.FindControl<TextBlock>("TxtTime");
        _txtConnection = this.FindControl<TextBlock>("TxtConnection");
        _txtSystem = this.FindControl<TextBlock>("TxtSystem");
        _txtBody = this.FindControl<TextBlock>("TxtBody");
        _txtLatitude = this.FindControl<TextBlock>("TxtLatitude");
        _txtLongitude = this.FindControl<TextBlock>("TxtLongitude");
        _logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
    }
    
    private void SetupEventHandlers()
    {
        Logging.Message += AppendLog;
        _state.Changed += s => UpdateGameStatus(s);
        AppConfig.SettingsChanged += OnSettingsChanged;
    }
    
    private void StartTimeTimer()
    {
        _timeTimer = new Timer(_ => UpdateTime(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.JournalFolder) && Directory.Exists(settings.JournalFolder))
        {
            _watcher?.Stop();
            StartWatcher(settings.JournalFolder);
        }
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnSelectFolderClick(object? sender, RoutedEventArgs e)
    {
        var top = this.StorageProvider;
        if (top == null) return;
        var result = await top.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Elite Dangerous journal folder",
            AllowMultiple = false
        });
        var folder = result?.FirstOrDefault();
        if (folder == null) return;
        var path = folder.TryGetLocalPath();
        if (string.IsNullOrEmpty(path)) return;
        var settings = AppConfig.Load();
        settings.JournalFolder = path;
        AppConfig.Save(settings);
        Logging.Info($"Journal folder set: {path}");
        _watcher?.Stop();
        _watcher = new JournalWatcher(path);
        _watcher.Changed += file => Logging.Info($"Journal changed: {Path.GetFileName(file)}");
        _watcher.Start();
    }

    private void TryStartWatcher()
    {
        var settings = AppConfig.Load();
        if (!string.IsNullOrWhiteSpace(settings.JournalFolder) && Directory.Exists(settings.JournalFolder))
        {
            StartWatcher(settings.JournalFolder);
            return;
        }

        var folders = JournalPaths.EnumerateLikelyFolders().ToList();
        if (folders.Count == 0)
        {
            Logging.Info("No journal folders found. Set path in settings later.");
            return;
        }
        StartWatcher(folders.First());
    }

    private void StartWatcher(string folder)
    {
        Logging.Info($"Using journal folder: {folder}");
        _watcher = new JournalWatcher(folder);
        _watcher.Changed += file => Logging.Info($"Journal changed: {Path.GetFileName(file)}");
        _processor ??= new JournalProcessor(_state);
        _watcher.Changed += file => _processor!.ProcessDelta(file);
        _watcher.Start();
    }

    private void AppendLog(string line)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_logsBox is null) return;
            
            var timestamp = DateTime.Now.ToString("[HH:mm:ss] ");
            _logsBox.Text += timestamp + line + "\n";
            _logsBox.CaretIndex = _logsBox.Text?.Length ?? 0;
            
            // Auto-scroll to bottom
            if (_logScrollViewer != null)
            {
                _logScrollViewer.ScrollToEnd();
            }
        });
    }

    private void UpdateGameStatus(GameState s)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Update individual status fields
            if (_txtSystem != null) _txtSystem.Text = s.StarSystem ?? "Unknown";
            if (_txtBody != null) _txtBody.Text = s.Body ?? "Unknown";
            if (_txtLatitude != null) _txtLatitude.Text = s.Latitude?.ToString("F6") ?? "Unknown";
            if (_txtLongitude != null) _txtLongitude.Text = s.Longitude?.ToString("F6") ?? "Unknown";
            
            // Update connection status
            if (_txtConnection != null)
            {
                _txtConnection.Text = "Connected";
                _txtConnection.Foreground = new SolidColorBrush(Color.FromRgb(92, 184, 92)); // Green
            }
            
            // Update main status bar
            if (_txtStatus != null)
            {
                var sys = s.StarSystem ?? "Unknown";
                var body = s.Body ?? "Unknown";
                if (s.Latitude.HasValue && s.Longitude.HasValue)
                {
                    _txtStatus.Text = $"📍 {sys} > {body} ({s.Latitude:F4}, {s.Longitude:F4})";
                }
                else
                {
                    _txtStatus.Text = $"🚀 {sys} > {body}";
                }
            }
        });
    }
    
    private void UpdateTime()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_txtTime != null)
            {
                _txtTime.Text = DateTime.Now.ToString("🕒 HH:mm:ss");
            }
        });
    }
    
    protected override void OnClosed(EventArgs e)
    {
        _timeTimer?.Dispose();
        _watcher?.Stop();
        base.OnClosed(e);
    }
}