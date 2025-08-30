using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using System.Timers;
using System;
using SrvSurvey.Core;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class FloatieWindow : Window
{
    private TextBlock? _txt;

    public FloatieWindow()
    {
        InitializeComponent();
        _txt = this.FindControl<TextBlock>("Txt");

        // Configure as overlay window
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        CanResize = false;
        IsHitTestVisible = false;

        // Important for overlay behavior
        WindowState = WindowState.Normal;
        WindowStartupLocation = WindowStartupLocation.Manual;

        // Set transparent background
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

        // Ensure window stays on top
        EnsureTopmost();
    }

    private void EnsureTopmost()
    {
        // Periodically ensure window stays on top
        var timer = new Timer(1000);
        timer.Elapsed += (s, e) =>
        {
            if (!Topmost)
            {
                Topmost = true;
                Dispatcher.UIThread.InvokeAsync(() => BringToFront());
            }
        };
        timer.Start();
    }

    private void BringToFront()
    {
        try
        {
            if (OperatingSystem.IsLinux())
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "wmctrl",
                        Arguments = $"-r :ACTIVE: -b add,above",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
            }
        }
        catch
        {
            // Ignore errors - fallback to basic Topmost
        }
    }

    public void ShowMessage(string message)
    {
        _txt!.Text = message;
        if (!IsVisible)
            Show();

        // Position based on saved settings or default (bottom center)
        PositionOnScreen();
    }

    private void PositionOnScreen()
    {
        var settings = AppConfig.Load();
        var overlayName = "Floatie";

        if (settings.OverlayPositions != null && settings.OverlayPositions.TryGetValue(overlayName, out var position))
        {
            // Use saved position
            var screen = Screens.Primary;
            if (screen != null)
            {
                var bounds = screen.WorkingArea;
                int x = (int)(bounds.X + position.X);
                int y = (int)(bounds.Y + position.Y);

                // Ensure window stays within screen bounds
                x = Math.Max(bounds.X, Math.Min(x, bounds.X + bounds.Width - (int)Width));
                y = Math.Max(bounds.Y, Math.Min(y, bounds.Y + bounds.Height - (int)Height));

                Position = new PixelPoint(x, y);
            }
        }
        else
        {
            // Use default position (bottom center)
            var screen = Screens.Primary;
            if (screen != null)
            {
                var bounds = screen.WorkingArea;
                Position = new PixelPoint((int)(bounds.X + (bounds.Width - Width) / 2), (int)(bounds.Y + bounds.Height - Height - 40));
            }
        }
    }
}
