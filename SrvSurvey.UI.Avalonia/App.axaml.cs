using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SrvSurvey.UI.Avalonia.Services;
using SrvSurvey.Core;

namespace SrvSurvey.UI.Avalonia;

public partial class App : Application
{
    public static IClipboardService ClipboardService { get; private set; } = null!;
    public static IGlobalHotkeys GlobalHotkeys { get; private set; } = null!;
    public static IGameWindowControl GameWindowControl { get; private set; } = null!;
    public static IInputDeviceService InputDeviceService { get; private set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ClipboardService = new AvaloniaClipboardService();
            GlobalHotkeys = new NoopGlobalHotkeys();
            GameWindowControl = new AppWindowControl();
            InputDeviceService = new NoopInputDeviceService();
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}