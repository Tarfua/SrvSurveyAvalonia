using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SrvSurvey.Core;
using System.Linq;
using Avalonia.Interactivity;
using System.IO;
using Avalonia.Media;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class SettingsView : UserControl
{
    private TextBox? _txt;
    private TextBlock? _txtFolderStatus;
    
    public SettingsView()
    {
        InitializeComponent();
        _txt = this.FindControl<TextBox>("TxtFolder");
        _txtFolderStatus = this.FindControl<TextBlock>("TxtFolderStatus");
        
        var settings = AppConfig.Load();
        UpdateFolderDisplay(settings.JournalFolder);
        AppConfig.SettingsChanged += s => UpdateFolderDisplay(s.JournalFolder);
    }
    
    private void UpdateFolderDisplay(string? folderPath)
    {
        if (_txt == null) return;
        
        _txt.Text = folderPath ?? string.Empty;
        
        if (_txtFolderStatus != null)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                _txtFolderStatus.Text = "❌ No journal folder configured";
                _txtFolderStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 107, 107));
            }
            else if (Directory.Exists(folderPath))
            {
                var journalFiles = Directory.GetFiles(folderPath, "Journal*.log").Length;
                _txtFolderStatus.Text = $"✅ Folder found - {journalFiles} journal files detected";
                _txtFolderStatus.Foreground = new SolidColorBrush(Color.FromRgb(92, 184, 92));
            }
            else
            {
                _txtFolderStatus.Text = "⚠️ Folder path does not exist";
                _txtFolderStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            }
        }
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        var sp = top?.StorageProvider;
        if (sp == null) return;
        var res = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false, Title = "Select journal folder" });
        var f = res?.FirstOrDefault();
        var path = f?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) return;
        var s = AppConfig.Load();
        s.JournalFolder = path;
        AppConfig.Save(s);
        Logging.Info($"Journal folder updated: {path}");
    }
}


