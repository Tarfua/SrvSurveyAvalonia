using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using SrvSurvey.Core;

namespace SrvSurvey.UI.Avalonia.Services;

public sealed class AvaloniaClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        var clipboard = ResolveClipboard();
        if (clipboard is not null)
        {
            clipboard.SetTextAsync(text);
            return;
        }
    }

    public async Task<string?> GetTextAsync(CancellationToken cancellationToken = default)
    {
        var clipboard = ResolveClipboard();
        if (clipboard is not null)
        {
            return await clipboard.GetTextAsync();
        }
        return null;
    }

    private static IClipboard? ResolveClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }
        return null;
    }
}

public sealed class NoopGlobalHotkeys : IGlobalHotkeys
{
    private readonly Dictionary<string, Action> _bindings = new();

    public void Register(string gesture, Action callback)
    {
        _bindings[gesture] = callback;
    }

    public void Unregister(string gesture)
    {
        _bindings.Remove(gesture);
    }

    public void Start() { }
    public void Stop() { }
}

public sealed class NoopInputDeviceService : IInputDeviceService
{
    public bool IsAvailable => false;
}

public sealed class AppWindowControl : IGameWindowControl
{
    public bool TryActivateGameWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            desktop.MainWindow.Activate();
            return true;
        }
        return false;
    }
}


