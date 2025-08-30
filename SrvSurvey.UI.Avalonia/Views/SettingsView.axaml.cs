using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SrvSurvey.Core;
using System.Linq;
using Avalonia.Interactivity;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class SettingsView : UserControl
{
    private TextBox? _txt;
    public SettingsView()
    {
        InitializeComponent();
        _txt = this.FindControl<TextBox>("TxtFolder");
        _txt!.Text = AppConfig.Load().JournalFolder ?? string.Empty;
        AppConfig.SettingsChanged += s => _txt!.Text = s.JournalFolder ?? string.Empty;
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
    }
}


