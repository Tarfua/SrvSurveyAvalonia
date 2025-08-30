using Newtonsoft.Json;

namespace SrvSurvey.Core;

public sealed class AppSettings
{
    public string? JournalFolder { get; set; }
}

public static class AppConfig
{
    private static readonly object _sync = new();
    private static AppSettings? _cached;
    public static event Action<AppSettings>? SettingsChanged;

    public static string GetConfigFilepath()
    {
        string folder;
        if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            folder = Path.Combine(home, ".config", "srvsurvey");
        }
        else if (OperatingSystem.IsWindows())
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            folder = Path.Combine(appdata, "SrvSurvey");
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            folder = Path.Combine(home, ".config", "srvsurvey");
        }

        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "config.json");
    }

    public static AppSettings Load()
    {
        lock (_sync)
        {
            if (_cached != null)
                return _cached;

            var path = GetConfigFilepath();
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    _cached = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    _cached = new AppSettings();
                }
            }
            else
            {
                _cached = new AppSettings();
            }

            return _cached;
        }
    }

    public static void Save(AppSettings settings)
    {
        lock (_sync)
        {
            _cached = settings;
            var path = GetConfigFilepath();
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(path, json);
            SettingsChanged?.Invoke(settings);
        }
    }
}


