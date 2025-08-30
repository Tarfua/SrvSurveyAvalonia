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
using SrvSurvey.UI.Avalonia.Views;
using System.Collections.Generic;

namespace SrvSurvey.UI.Avalonia;

public partial class MainWindow : Window
{
    private const int CONNECTED_COLOR_R = 92;
    private const int CONNECTED_COLOR_G = 184;
    private const int CONNECTED_COLOR_B = 92;

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
    private readonly GameState _state = new GameState();
    private readonly Random _random = new Random();
    private JournalProcessor? _processor;
    private Timer? _timeTimer;
    private FloatieWindow? _floatie;
    private SystemStatusOverlay? _systemStatus;
    private BioStatusOverlay? _bioStatus;
    private ColonyCommoditiesOverlay? _colonyCommodities;
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
        _state.Changed += s =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateGameStatus(s);
            });

            // ShowFloatieForState already handles its own UI thread safety
            ShowFloatieForState(s);
        };
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
        // Update individual status fields with null safety
        UpdateTextBlock(_txtSystem, s.StarSystem ?? "Unknown");
        UpdateTextBlock(_txtBody, s.Body ?? "Unknown");
        UpdateTextBlock(_txtLatitude, s.Latitude?.ToString("F6") ?? "Unknown");
        UpdateTextBlock(_txtLongitude, s.Longitude?.ToString("F6") ?? "Unknown");

        // Update connection status
        UpdateConnectionStatus();

        // Update main status bar
        UpdateMainStatus(s);
    }

    private void UpdateTextBlock(TextBlock? textBlock, string text)
    {
        if (textBlock != null)
        {
            textBlock.Text = text;
        }
    }

    private void UpdateConnectionStatus()
    {
        if (_txtConnection != null)
        {
            _txtConnection.Text = "Connected";
            _txtConnection.Foreground = new SolidColorBrush(Color.FromRgb(CONNECTED_COLOR_R, CONNECTED_COLOR_G, CONNECTED_COLOR_B));
        }
    }

    private void UpdateMainStatus(GameState s)
    {
        if (_txtStatus == null) return;

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

    private void ShowFloatieForState(GameState s)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Show small overlay when system/body changes
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(s.StarSystem)) parts.Add(s.StarSystem!);
            if (!string.IsNullOrWhiteSpace(s.Body)) parts.Add(s.Body!);
            if (parts.Count == 0) return;

            _floatie ??= new FloatieWindow();
            _floatie.ShowMessage(string.Join(" > ", parts));

            // Update system status overlay
            UpdateSystemStatusOverlay(s);

            // Update bio status overlay
            UpdateBioStatusOverlay(s);

            // Update colony commodities overlay
            UpdateColonyCommoditiesOverlay(s);
        });
    }
    
    private void UpdateSystemStatusOverlay(GameState s)
    {
        if (string.IsNullOrWhiteSpace(s.StarSystem)) return;

        _systemStatus ??= new SystemStatusOverlay();

        var status = s.StarSystem;
        if (!string.IsNullOrWhiteSpace(s.Body))
            status += $" > {s.Body}";

        _systemStatus.UpdateStatus(status, "System Status");
        _systemStatus.ShowOverlay();
    }

    private void UpdateBioStatusOverlay(GameState s)
    {
        if (string.IsNullOrWhiteSpace(s.Body)) return;

        _bioStatus ??= new BioStatusOverlay();

        // Get bio signal count and temperature from game state
        var bioSignalCount = GetBioSignalCount(s.Body);
        var temperature = GetCurrentTemperature();
        _bioStatus.UpdateBioStatus(s.Body, bioSignalCount, temperature);
        _bioStatus.ShowOverlay();
    }

    private void UpdateColonyCommoditiesOverlay(GameState s)
    {
        // For now, show sample colony data
        // TODO: Integrate with actual colony data from journal events
        _colonyCommodities ??= new ColonyCommoditiesOverlay();

        var sampleCommodities = new Dictionary<string, int>
        {
            { "Aluminium", 10055 },
            { "Steel", 14076 },
            { "Titanium", 8205 },
            { "Ceramic Composites", 1207 },
            { "Polymers", 1046 }
        };

        _colonyCommodities.UpdateCommodities(sampleCommodities, "Sample Colony Project");
        _colonyCommodities.ShowOverlay();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        _timeTimer?.Dispose();
        _watcher?.Stop();
        // Note: JournalProcessor doesn't implement IDisposable
        _floatie?.Close();
        _systemStatus?.Close();
        _bioStatus?.Close();
        _colonyCommodities?.Close();
        base.OnClosed(e);
    }

    private int GetBioSignalCount(string? bodyName)
    {
        // TODO: Parse journal events for bio signals on this body
        // For now, return a random number between 0-5 for demo
        if (string.IsNullOrEmpty(bodyName)) return 0;
        return _random.Next(0, 6);
    }

    private double? GetCurrentTemperature()
    {
        // TODO: Parse journal events for current temperature
        // For now, return a random temperature between -50 and 50
        return _random.Next(-50, 51);
    }
}