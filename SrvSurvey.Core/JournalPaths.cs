using System.Runtime.InteropServices;

namespace SrvSurvey.Core;

public static class JournalPaths
{
    public static IEnumerable<string> EnumerateLikelyFolders()
    {
        if (OperatingSystem.IsWindows())
        {
            var saved = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var basePath = Path.Combine(saved, "Saved Games", "Frontier Developments", "Elite Dangerous");
            if (Directory.Exists(basePath))
                yield return basePath;
            yield break;
        }

        if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            // Proton default compatdata path for ED (replace <APPID> as needed). We search all compatdata entries
            var compat = Path.Combine(home, ".steam", "steam", "steamapps", "compatdata");
            if (Directory.Exists(compat))
            {
                foreach (var dir in Directory.EnumerateDirectories(compat))
                {
                    var pfx = Path.Combine(dir, "pfx", "drive_c", "users", "steamuser", "Saved Games", "Frontier Developments", "Elite Dangerous");
                    if (Directory.Exists(pfx))
                        yield return pfx;
                }
            }

            // Alternative path for non-default Steam installations
            var compatAlt = Path.Combine(home, ".local", "share", "Steam", "steamapps", "compatdata");
            if (Directory.Exists(compatAlt))
            {
                foreach (var dir in Directory.EnumerateDirectories(compatAlt))
                {
                    var pfx = Path.Combine(dir, "pfx", "drive_c", "users", "steamuser", "Saved Games", "Frontier Developments", "Elite Dangerous");
                    if (Directory.Exists(pfx))
                        yield return pfx;
                }
            }
        }
    }
}


