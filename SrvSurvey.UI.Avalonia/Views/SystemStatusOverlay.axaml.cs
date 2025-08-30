using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using SrvSurvey.Core;
using System.Timers;
using System;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class SystemStatusOverlay : Window
{
    private TextBlock? _txtStatus;
    private TextBlock? _txtHeader;

    public SystemStatusOverlay()
    {
        InitializeComponent();
        _txtStatus = this.FindControl<TextBlock>("TxtStatus");
        _txtHeader = this.FindControl<TextBlock>("TxtHeader");

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

        // Position based on saved settings or default
        PositionOnScreen();

        // Ensure window stays on top - try platform-specific methods
        EnsureTopmost();
    }

    private void PositionOnScreen()
    {
        var settings = AppConfig.Load();
        var overlayName = "System Status";

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
            // Use default position (top-right)
            var screen = Screens.Primary;
            if (screen != null)
            {
                var bounds = screen.WorkingArea;
                Position = new PixelPoint((int)(bounds.X + bounds.Width - Width - 20), (int)(bounds.Y + 20));
            }
        }
    }

    private void EnsureTopmost()
    {
        // Periodically ensure window stays on top
        var timer = new System.Timers.Timer(1000); // Check every second
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
            // Try to bring window to front using platform-specific methods
            if (OperatingSystem.IsLinux())
            {
                // For Linux, we can try to use wmctrl or similar
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

    public void UpdateStatus(string status, string? header = null)
    {
        if (_txtStatus != null)
            _txtStatus.Text = status;
            
        if (_txtHeader != null && header != null)
            _txtHeader.Text = header;
    }

    public void ShowOverlay()
    {
        if (!IsVisible)
            Show();
    }

    public void HideOverlay()
    {
        if (IsVisible)
            Hide();
    }
}
