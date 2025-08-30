using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using SrvSurvey.Core;
using System.Timers;
using System;
using System.Collections.Generic;
using System.Linq;

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
        // Detect display server and desktop environment
        DetectDisplayEnvironment();

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

    private string _displayServer = "unknown";
    private string _desktopEnvironment = "unknown";

    private void DetectDisplayEnvironment()
    {
        try
        {
            // Detect display server (X11 or Wayland)
            var displayProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = "-c \"echo $XDG_SESSION_TYPE\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            displayProcess.Start();
            _displayServer = displayProcess.StandardOutput.ReadToEnd().Trim();
            displayProcess.WaitForExit();

            // Detect desktop environment
            var deProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = "-c \"echo $XDG_CURRENT_DESKTOP\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            deProcess.Start();
            _desktopEnvironment = deProcess.StandardOutput.ReadToEnd().Trim().ToUpper();
            deProcess.WaitForExit();

            Console.WriteLine($"Detected display server: {_displayServer}, Desktop environment: {_desktopEnvironment}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error detecting display environment: {ex.Message}");
        }
    }

    private void ApplyWindowManagerHints()
    {
        // First, check if there are fullscreen windows and handle them specially
        if (HandleFullscreenWindows())
            return;

        // Choose strategy based on display server and desktop environment
        if (_displayServer.Contains("wayland"))
        {
            // Wayland has limited window management capabilities
            ApplyWaylandStrategy();
        }
        else
        {
            // X11 has full window management support
            ApplyX11Strategy();
        }
    }

    private void ApplyWaylandStrategy()
    {
        Console.WriteLine("Applying Wayland-specific overlay strategy");

        // Try different approaches based on compositor/desktop environment
        if (_desktopEnvironment.Contains("GNOME"))
        {
            TryGnomeWaylandMethod();
        }
        else if (_desktopEnvironment.Contains("KDE"))
        {
            TryKdeWaylandMethod();
        }
        else if (TryWlrootsCompositorMethod())
        {
            // Sway, Hyprland, etc.
            return;
        }
        else
        {
            // Generic Wayland fallback
            TryGenericWaylandMethod();
        }

        // Always keep basic Topmost as last resort
        Topmost = true;
    }

    private bool TryWlrootsCompositorMethod()
    {
        try
        {
            // Check if we're running on wlroots-based compositor (Sway, Hyprland, etc.)
            var swaymsg = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "swaymsg",
                    Arguments = "-t get_tree",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            swaymsg.Start();
            var output = swaymsg.StandardOutput.ReadToEnd();
            swaymsg.WaitForExit();

            if (swaymsg.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                // We're on Sway - try Sway-specific methods
                return ApplySwayOverlayMethod();
            }

            // Try Hyprland
            var hyprctl = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "hyprctl",
                    Arguments = "activewindow",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            hyprctl.Start();
            var hyprOutput = hyprctl.StandardOutput.ReadToEnd();
            hyprctl.WaitForExit();

            if (hyprctl.ExitCode == 0)
            {
                // We're on Hyprland
                return ApplyHyprlandOverlayMethod();
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool ApplySwayOverlayMethod()
    {
        try
        {
            // For Sway, try to set window as floating and sticky
            var commands = new[]
            {
                $"swaymsg \"[title=\"{Title}\"] floating enable\"",
                $"swaymsg \"[title=\"{Title}\"] sticky enable\"",
                $"swaymsg \"[title=\"{Title}\"] move position 0 0\""
            };

            foreach (var command in commands)
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = $"-c \"{command}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                process.WaitForExit();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool ApplyHyprlandOverlayMethod()
    {
        try
        {
            // For Hyprland, set window rules for overlay behavior
            var commands = new[]
            {
                $"hyprctl dispatch setprop address:$(hyprctl activewindow -j | jq -r '.address') float true",
                $"hyprctl dispatch pin address:$(hyprctl activewindow -j | jq -r '.address')",
                $"hyprctl keyword windowrule \"float, title:{Title}\"",
                $"hyprctl keyword windowrule \"pin, title:{Title}\"",
                $"hyprctl keyword windowrule \"stayfocused, title:{Title}\""
            };

            foreach (var command in commands)
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = $"-c \"{command}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                process.WaitForExit();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void TryKdeWaylandMethod()
    {
        try
        {
            // KDE Plasma on Wayland has some D-Bus interfaces
            var commands = new[]
            {
                // Try to set window as keep above through KWin
                $"dbus-send --session --dest=org.kde.KWin --type=method_call /KWin org.kde.KWin.setWindowKeepAbove string:\"{Title}\" boolean:true",
                // Set window type to override
                $"dbus-send --session --dest=org.kde.KWin --type=method_call /KWin org.kde.KWin.setWindowType string:\"{Title}\" uint32:5"
            };

            foreach (var command in commands)
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = $"-c \"{command}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                process.WaitForExit();
            }
        }
        catch
        {
            // Fallback to basic methods
            Topmost = true;
        }
    }

    private void TryGenericWaylandMethod()
    {
        try
        {
            // Generic Wayland methods that might work with XWayland
            var commands = new[]
            {
                // Try XWayland compatibility mode
                $"env DISPLAY=:0 wmctrl -r \"{Title}\" -b add,above",
                $"env DISPLAY=:0 xprop -name \"{Title}\" -f _NET_WM_STATE 32a -set _NET_WM_STATE _NET_WM_STATE_ABOVE",
                // Try through Mutter/GNOME Shell if available
                "gdbus call --session --dest org.gnome.Shell --object-path /org/gnome/Shell --method org.gnome.Shell.Eval \"global.get_window_actors().forEach(a => { if (a.get_meta_window().get_title().includes('" + Title.Replace("'", "\\'") + "')) { a.get_meta_window().make_above(); } });\""
            };

            foreach (var command in commands)
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = $"-c \"{command}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                process.WaitForExit();
            }
        }
        catch
        {
            // Last resort - just set Topmost
            Topmost = true;
        }
    }

    private void ApplyX11Strategy()
    {
        // X11 has full window management support
        // Method 1: Try wmctrl (most reliable for X11)
        if (TryWmctrlMethod())
            return;

        // Method 2: Try xdotool (alternative tool)
        if (TryXdotoolMethod())
            return;

        // Method 3: Try advanced X11 properties
        TryAdvancedX11Method();

        // Method 4: Try desktop environment specific methods
        TryDesktopEnvironmentSpecificMethod();

        // Method 5: Try xprop with window search
        TryXpropMethod();
    }

    private void TryGnomeWaylandMethod()
    {
        try
        {
            Console.WriteLine("Trying GNOME on Wayland overlay methods");

            // GNOME on Wayland has limited but some working methods
            var commands = new[]
            {
                // Try through GNOME Shell extensions API
                "gdbus call --session --dest org.gnome.Shell --object-path /org/gnome/Shell --method org.gnome.Shell.Eval \"global.get_window_actors().forEach(actor => { const win = actor.get_meta_window(); if (win.get_title() && win.get_title().includes('" + Title.Replace("'", "\\'") + "')) { win.make_above(); win.stick(); } });\"",

                // Try through Mutter D-Bus interface
                $"dbus-send --session --dest=org.gnome.Mutter.DisplayConfig --type=method_call /org/gnome/Mutter/DisplayConfig org.gnome.Mutter.DisplayConfig.ApplyMonitorsConfig uint32:0 array:struct && sleep 0.1",

                // Try XWayland compatibility if running through XWayland
                $"env DISPLAY=:0 wmctrl -r \"{Title}\" -b add,above,sticky 2>/dev/null || true",

                // Try through GNOME settings daemon
                $"gsettings set org.gnome.desktop.wm.preferences focus-mode 'mouse'",
                $"gsettings set org.gnome.desktop.wm.preferences raise-on-click false",

                // Try to set window type through xprop (if XWayland)
                $"xprop -name \"{Title}\" -f _NET_WM_WINDOW_TYPE 32a -set _NET_WM_WINDOW_TYPE _NET_WM_WINDOW_TYPE_NOTIFICATION 2>/dev/null || true",

                // Alternative approach through Mutter
                $"dbus-send --session --dest=org.gnome.Shell --type=method_call /org/gnome/Shell/Extensions org.gnome.Shell.Extensions.ReloadExtension string:\"window-list@gnome-shell-extensions.gcampax.github.com\" 2>/dev/null || true"
            };

            foreach (var command in commands)
            {
                try
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "bash",
                            Arguments = $"-c \"{command}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true
                        }
                    };

                    process.Start();
                    process.WaitForExit(500); // Timeout after 500ms

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"GNOME Wayland command succeeded: {command.Split(' ')[0]}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GNOME Wayland command failed: {ex.Message}");
                }
            }

            // Additional GNOME-specific window management
            TryGnomeShellIntegration();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GNOME Wayland method error: {ex.Message}");
        }
    }

    private void TryGnomeShellIntegration()
    {
        try
        {
            // Try to create a GNOME Shell extension-like behavior
            // This simulates what a GNOME extension would do

            var extensionScript = @"
// GNOME Shell extension simulation for overlay windows
const Meta = imports.gi.Meta;
const Shell = imports.gi.Shell;

function makeWindowOverlay(titlePattern) {
    const windows = global.get_window_actors();
    for (let i = 0; i < windows.length; i++) {
        const window = windows[i].get_meta_window();
        if (window.get_title() && window.get_title().includes(titlePattern)) {
            // Make window always on top
            window.make_above();
            window.stick();

            // Set window type to override
            window.set_window_type(Meta.WindowType.OVERRIDE);

            // Connect to focus change events
            window.connect('focus', () => {
                window.make_above();
            });

            console.log('Made window overlay: ' + window.get_title());
        }
    }
}

// Apply to our overlay windows
makeWindowOverlay('" + Title.Replace("'", "\\'") + @"');
";

            // Execute through GNOME Shell's eval
            var evalProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "gdbus",
                    Arguments = "call --session --dest org.gnome.Shell --object-path /org/gnome/Shell --method org.gnome.Shell.Eval \"" + extensionScript.Replace("\"", "\\\"").Replace("\n", " ").Replace("  ", " ") + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            evalProcess.Start();
            var output = evalProcess.StandardOutput.ReadToEnd();
            evalProcess.WaitForExit();

            Console.WriteLine($"GNOME Shell eval result: {output}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GNOME Shell integration failed: {ex.Message}");
        }
    }

    private void TryDesktopEnvironmentSpecificMethod()
    {
        // Apply desktop environment specific optimizations
        if (_desktopEnvironment.Contains("GNOME"))
        {
            TryGnomeMethod();
        }
        else if (_desktopEnvironment.Contains("KDE"))
        {
            TryKdeMethod();
        }
        else if (_desktopEnvironment.Contains("XFCE"))
        {
            TryXfceMethod();
        }
    }

    private void TryGnomeMethod()
    {
        try
        {
            // GNOME specific settings
            var commands = new[]
            {
                $"gsettings set org.gnome.desktop.wm.preferences raise-on-click false",
                $"wmctrl -r \"{Title}\" -b add,above",
                $"xprop -name \"{Title}\" -f _NET_WM_STATE 32a -set _NET_WM_STATE _NET_WM_STATE_ABOVE,_NET_WM_STATE_STICKY"
            };

            foreach (var command in commands)
            {
                try
                {
                    var parts = command.Split(' ', 2);
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = parts[0],
                            Arguments = parts[1],
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardError = true
                        }
                    };

                    process.Start();
                    process.WaitForExit();
                }
                catch
                {
                    // Continue with next command
                }
            }
        }
        catch
        {
            // Ignore GNOME errors
        }
    }

    private void TryKdeMethod()
    {
        try
        {
            // KDE specific settings
            var commands = new[]
            {
                $"wmctrl -r \"{Title}\" -b add,above,sticky",
                $"xprop -name \"{Title}\" -f _KDE_NET_WM_WINDOW_TYPE_OVERRIDE 32a -set _KDE_NET_WM_WINDOW_TYPE_OVERRIDE _KDE_NET_WM_WINDOW_TYPE_ON_SCREEN_DISPLAY",
                $"qdbus org.kde.KWin /KWin setWindowRule \"{Title}\" \"above\" \"true\""
            };

            foreach (var command in commands)
            {
                try
                {
                    var parts = command.Split(' ', 2);
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = parts[0],
                            Arguments = parts[1],
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardError = true
                        }
                    };

                    process.Start();
                    process.WaitForExit();
                }
                catch
                {
                    // Continue with next command
                }
            }
        }
        catch
        {
            // Ignore KDE errors
        }
    }

    private void TryXfceMethod()
    {
        try
        {
            // XFCE specific settings
            var commands = new[]
            {
                $"wmctrl -r \"{Title}\" -b add,above,sticky",
                $"xfconf-query -c xfwm4 -p /general/raise_on_click -s false",
                $"xprop -name \"{Title}\" -f _NET_WM_STATE 32a -set _NET_WM_STATE _NET_WM_STATE_ABOVE"
            };

            foreach (var command in commands)
            {
                try
                {
                    var parts = command.Split(' ', 2);
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = parts[0],
                            Arguments = parts[1],
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardError = true
                        }
                    };

                    process.Start();
                    process.WaitForExit();
                }
                catch
                {
                    // Continue with next command
                }
            }
        }
        catch
        {
            // Ignore XFCE errors
        }
    }

    private bool HandleFullscreenWindows()
    {
        try
        {
            // Check for fullscreen windows and apply special handling
            var fullscreenWindows = GetFullscreenWindows();
            if (fullscreenWindows.Any())
            {
                return ApplyFullscreenOverlayStrategy(fullscreenWindows);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling fullscreen windows: {ex.Message}");
        }
        return false;
    }

    private List<string> GetFullscreenWindows()
    {
        var fullscreenWindows = new List<string>();

        try
        {
            // Use wmctrl to find fullscreen windows
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wmctrl",
                    Arguments = "-lG",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 8)
                    {
                        // Check if window is fullscreen (covers entire screen)
                        var screen = Screens.Primary;
                        if (screen != null)
                        {
                            int x = int.Parse(parts[2]);
                            int y = int.Parse(parts[3]);
                            int width = int.Parse(parts[4]);
                            int height = int.Parse(parts[5]);

                            // Check if window covers most of the screen (allowing for small borders)
                            if (width >= screen.WorkingArea.Width - 10 && height >= screen.WorkingArea.Height - 10)
                            {
                                fullscreenWindows.Add(parts[0]); // Window ID
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore errors in fullscreen detection
        }

        return fullscreenWindows;
    }

    private bool ApplyFullscreenOverlayStrategy(List<string> fullscreenWindowIds)
    {
        try
        {
            // Strategy 1: Try to make overlay window stay on top of fullscreen windows
            if (ApplyFullscreenTopmost())
                return true;

            // Strategy 2: Try to bypass fullscreen restrictions
            if (ApplyFullscreenBypass())
                return true;

            // Strategy 3: Try to force window layer
            return ApplyForceLayer();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in fullscreen overlay strategy: {ex.Message}");
            return false;
        }
    }

    private bool ApplyFullscreenTopmost()
    {
        try
        {
            // Use wmctrl to set window above all others including fullscreen
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wmctrl",
                    Arguments = $"-r \"{Title}\" -b add,above,sticky",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                // Also try to set the _NET_WM_STATE_STAYS_ON_TOP property
                TrySetStayOnTopProperty();
                return true;
            }
        }
        catch
        {
            // Continue to next method
        }
        return false;
    }

    private bool ApplyFullscreenBypass()
    {
        try
        {
            // Try to set window type to override redirect
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "xprop",
                    Arguments = $"-name \"{Title}\" -f _NET_WM_WINDOW_TYPE 32a -set _NET_WM_WINDOW_TYPE _NET_WM_WINDOW_TYPE_NOTIFICATION",
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

    private bool ApplyForceLayer()
    {
        try
        {
            // Force window to specific layer using wmctrl
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wmctrl",
                    Arguments = $"-r \"{Title}\" -b add,above -t 0",
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

    private void TrySetStayOnTopProperty()
    {
        try
        {
            // Set the _NET_WM_STATE_STAYS_ON_TOP property
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "xprop",
                    Arguments = $"-name \"{Title}\" -f _NET_WM_STATE 32a -set _NET_WM_STATE _NET_WM_STATE_STAYS_ON_TOP",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            process.WaitForExit();
        }
        catch
        {
            // Ignore errors
        }
    }

    private void TryAdvancedX11Method()
    {
        try
        {
            // Try multiple X11 properties for better overlay behavior
            var commands = new[]
            {
                // Set window type to utility (higher priority)
                $"xprop -name \"{Title}\" -f _NET_WM_WINDOW_TYPE 32a -set _NET_WM_WINDOW_TYPE _NET_WM_WINDOW_TYPE_UTILITY",
                // Set state to modal (should stay on top)
                $"xprop -name \"{Title}\" -f _NET_WM_STATE 32a -set _NET_WM_STATE _NET_WM_STATE_MODAL",
                // Try to set desktop to all desktops
                $"wmctrl -r \"{Title}\" -b add,sticky"
            };

            foreach (var command in commands)
            {
                try
                {
                    var parts = command.Split(' ', 2);
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = parts[0],
                            Arguments = parts[1],
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardError = true
                        }
                    };

                    process.Start();
                    process.WaitForExit();
                }
                catch
                {
                    // Continue with next command
                }
            }
        }
        catch
        {
            // Ignore errors in advanced method
        }
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

        _overlayMonitorTimer = new System.Timers.Timer(2000); // Check every 2 seconds for better responsiveness
        _overlayMonitorTimer.Elapsed += async (s, e) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    EnsureOverlayState();
                    CheckFullscreenState();
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

    private void CheckFullscreenState()
    {
        // Check if fullscreen state has changed and adapt accordingly
        var fullscreenWindows = GetFullscreenWindows();
        if (fullscreenWindows.Any() && !_fullscreenModeActive)
        {
            // Entering fullscreen mode - apply special handling
            _fullscreenModeActive = true;
            ApplyFullscreenOverlayStrategy(fullscreenWindows);
        }
        else if (!fullscreenWindows.Any() && _fullscreenModeActive)
        {
            // Exiting fullscreen mode - return to normal overlay handling
            _fullscreenModeActive = false;
            ApplyWindowManagerHints(); // Reapply normal hints
        }
    }

    private bool _fullscreenModeActive = false;

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

            // Check if we need special fullscreen handling
            if (_fullscreenModeActive)
            {
                ApplyFullscreenOverlayStrategy(new List<string>());
            }

            // Additional Wayland-specific checks
            if (_displayServer.Contains("wayland"))
            {
                EnsureWaylandOverlayState();
            }
        }
    }

    private void EnsureWaylandOverlayState()
    {
        // Additional checks for Wayland compositors
        try
        {
            if (_desktopEnvironment.Contains("GNOME"))
            {
                // Periodic GNOME Shell refresh
                var refreshProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "gdbus",
                        Arguments = "call --session --dest org.gnome.Shell --object-path /org/gnome/Shell --method org.gnome.Shell.Eval \"global.get_window_actors().forEach(actor => { const win = actor.get_meta_window(); if (win.get_title() && win.get_title().includes('" + Title.Replace("'", "\\'") + "')) { win.make_above(); } });\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };

                refreshProcess.Start();
                refreshProcess.WaitForExit();
            }
        }
        catch
        {
            // Ignore Wayland-specific errors
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
