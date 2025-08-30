namespace SrvSurvey.Core;

public interface IClipboardService
{
    void SetText(string text);
    Task<string?> GetTextAsync(CancellationToken cancellationToken = default);
}

public interface IGlobalHotkeys
{
    void Register(string gesture, Action callback);
    void Unregister(string gesture);
    void Start();
    void Stop();
}

public interface IGameWindowControl
{
    bool TryActivateGameWindow();
}

public interface IInputDeviceService
{
    // Extend with controller/joystick if needed
    bool IsAvailable { get; }
}

public static class Platform
{
    public static bool IsLinux => OperatingSystem.IsLinux();
    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool IsMacOS => OperatingSystem.IsMacOS();
}
