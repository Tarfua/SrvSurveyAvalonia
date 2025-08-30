using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using SrvSurvey.Core;
using System.Timers;
using System;

namespace SrvSurvey.UI.Avalonia.Views;

/// <summary>
/// Base class for overlay windows with Linux-specific always-on-top behavior
/// </summary>
public abstract class OverlayWindowBase : Window
{
    private System.Timers.Timer? _overlayMonitorTimer;
    private bool _isMonitoring = false;

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Setup overlay behavior after window is opened
        SetupOverlayBehavior();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Clean up resources
        StopOverlayMonitor();
        base.OnClosed(e);
    }

    private void SetupOverlayBehavior()
    {
        if (!OperatingSystem.IsLinux())
        {
            // For non-Linux platforms, use basic Topmost
            Topmost = true;
            return;
        }

        // Linux-specific overlay setup
        SetupLinuxOverlay();
    }

    private void SetupLinuxOverlay()
    {
        // Set basic properties
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        CanResize = false;
        IsHitTestVisible = false;

        // Apply window manager hints for better overlay behavior
        ApplyWindowManagerHints();

        // Start monitoring to maintain overlay state
        StartOverlayMonitor();
    }

    private void ApplyWindowManagerHints()
    {
        // Method 1: Try wmctrl (most reliable for X11)
        if (TryWmctrlMethod())
            return;

        // Method 2: Try xdotool (alternative tool)
        if (TryXdotoolMethod())
            return;

        // Method 3: Try xprop with window search
        TryXpropMethod();
    }

    private bool TryWmctrlMethod()
    {
        try
        {
            // Use wmctrl to set window always on top
            // Wait a bit for window to be fully created
            System.Threading.Thread.Sleep(100);

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wmctrl",
                    Arguments = $"-r \"{Title}\" -b add,above",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private bool TryXdotoolMethod()
    {
        try
        {
            // Use xdotool as alternative
            System.Threading.Thread.Sleep(100);

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "xdotool",
                    Arguments = $"search --name \"{Title}\" set_window --overrideredirect 1 windowraise",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void TryXpropMethod()
    {
        try
        {
            // Use xprop to find and modify window properties
            var findProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "xprop",
                    Arguments = "-root _NET_ACTIVE_WINDOW",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            findProcess.Start();
            var output = findProcess.StandardOutput.ReadToEnd();
            findProcess.WaitForExit();

            if (findProcess.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                // Could parse window ID and apply properties
                // For now, rely on other methods
            }
        }
        catch
        {
            // xprop method failed
        }
    }

    private void StartOverlayMonitor()
    {
        if (_isMonitoring) return;

        _overlayMonitorTimer = new System.Timers.Timer(3000); // Check every 3 seconds
        _overlayMonitorTimer.Elapsed += async (s, e) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    EnsureOverlayState();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Overlay monitor error: {ex.Message}");
                }
            });
        };
        _overlayMonitorTimer.Start();
        _isMonitoring = true;
    }

    private void StopOverlayMonitor()
    {
        if (_overlayMonitorTimer != null)
        {
            _overlayMonitorTimer.Stop();
            _overlayMonitorTimer.Dispose();
            _overlayMonitorTimer = null;
        }
        _isMonitoring = false;
    }

    private void EnsureOverlayState()
    {
        // Re-ensure Topmost property
        if (!Topmost)
        {
            Topmost = true;
        }

        // Additional Linux-specific checks
        if (OperatingSystem.IsLinux() && IsVisible)
        {
            // Periodically reapply window manager hints
            ApplyWindowManagerHints();
        }
    }
}

public partial class SystemStatusOverlay : OverlayWindowBase
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
        WindowState = WindowState.Normal;
        WindowStartupLocation = WindowStartupLocation.Manual;

        // Set transparent background
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

        // Position based on saved settings or default
        PositionOnScreen();
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
