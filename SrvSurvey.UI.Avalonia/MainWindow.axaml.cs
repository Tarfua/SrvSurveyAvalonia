using Avalonia.Controls;
using Avalonia.Interactivity;
using SrvSurvey.Core;
using System.Linq;
using System.IO;

namespace SrvSurvey.UI.Avalonia;

public partial class MainWindow : Window
{
    private TextBox? _logsBox;
    private JournalWatcher? _watcher;
    public MainWindow()
    {
        InitializeComponent();
        _logsBox = this.FindControl<TextBox>("Logs");
        Logging.Message += AppendLog;
        TryStartWatcher();
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TryStartWatcher()
    {
        var folders = JournalPaths.EnumerateLikelyFolders().ToList();
        if (folders.Count == 0)
        {
            Logging.Info("No journal folders found. Set path in settings later.");
            return;
        }
        var folder = folders.First();
        Logging.Info($"Using journal folder: {folder}");
        _watcher = new JournalWatcher(folder);
        _watcher.Changed += file => Logging.Info($"Journal changed: {Path.GetFileName(file)}");
        _watcher.Start();
    }

    private void AppendLog(string line)
    {
        if (_logsBox is null) return;
        _logsBox.Text += line + "\n";
        _logsBox.CaretIndex = _logsBox.Text?.Length ?? 0;
    }
}