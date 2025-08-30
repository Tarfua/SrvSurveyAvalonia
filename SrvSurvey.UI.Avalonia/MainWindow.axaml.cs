using Avalonia.Controls;
using Avalonia.Interactivity;
using SrvSurvey.Core;
using System.Linq;
using System.IO;
using Avalonia.Platform.Storage;

namespace SrvSurvey.UI.Avalonia;

public partial class MainWindow : Window
{
    private TextBox? _logsBox;
    private JournalWatcher? _watcher;
    private TextBlock? _txtStatus;
    private GameState _state = new GameState();
    private JournalProcessor? _processor;
    public MainWindow()
    {
        InitializeComponent();
        _logsBox = this.FindControl<TextBox>("Logs");
        _txtStatus = this.FindControl<TextBlock>("TxtStatus");
        Logging.Message += AppendLog;
        _state.Changed += s => UpdateStatus(s);
        TryStartWatcher();
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
        if (_logsBox is null) return;
        _logsBox.Text += line + "\n";
        _logsBox.CaretIndex = _logsBox.Text?.Length ?? 0;
    }

    private void UpdateStatus(GameState s)
    {
        if (_txtStatus is null) return;
        var sys = s.StarSystem ?? "?";
        var body = s.Body ?? "?";
        var lat = s.Latitude?.ToString("F4") ?? "?";
        var lon = s.Longitude?.ToString("F4") ?? "?";
        _txtStatus.Text = $"{sys} | {body} | {lat}, {lon}";
    }
}