using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using SrvSurvey.Core;
using System.Timers;
using System;

namespace SrvSurvey.UI.Avalonia.Views;

public partial class BioStatusOverlay : Window
{
    private TextBlock? _txtBody;
    private TextBlock? _txtSignals;
    private TextBlock? _txtTemperature;
    private TextBlock? _txtLastScan;

        public BioStatusOverlay()
    {
        InitializeComponent();
        _txtBody = this.FindControl<TextBlock>("TxtBody");
        _txtSignals = this.FindControl<TextBlock>("TxtSignals");
        _txtTemperature = this.FindControl<TextBlock>("TxtTemperature");
        _txtLastScan = this.FindControl<TextBlock>("TxtLastScan");

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

        // Position bottom-left of primary screen
        PositionOnScreen();

        // Ensure window stays on top
        EnsureTopmost();
    }

    private void PositionOnScreen()
    {
        var settings = AppConfig.Load();
        var overlayName = "Bio Status";

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
            // Use default position (bottom-left)
            var screen = Screens.Primary;
            if (screen != null)
            {
                var bounds = screen.WorkingArea;
                Position = new PixelPoint((int)(bounds.X + 20), (int)(bounds.Y + bounds.Height - Height - 20));
            }
        }
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

    public void UpdateBioStatus(string bodyName, int signalCount, double? temperature, string? lastScan = null)
    {
        if (_txtBody != null)
            _txtBody.Text = bodyName;
            
        if (_txtSignals != null)
            _txtSignals.Text = $"Signals: {signalCount}";
            
        if (_txtTemperature != null)
        {
            if (temperature.HasValue)
                _txtTemperature.Text = $"Temperature: {temperature:F1}K";
            else
                _txtTemperature.Text = "Temperature: Unknown";
        }
        
        if (_txtLastScan != null && !string.IsNullOrEmpty(lastScan))
            _txtLastScan.Text = lastScan;
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
