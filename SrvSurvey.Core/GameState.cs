using Newtonsoft.Json.Linq;

namespace SrvSurvey.Core;

public sealed class GameState
{
    public string? StarSystem { get; private set; }
    public string? Body { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public event Action<GameState>? Changed;

    public void UpdateFromEvent(JObject evt)
    {
        var evtName = evt["event"]?.Value<string>();
        switch (evtName)
        {
            case "Location":
            case "FSDJump":
                StarSystem = evt["StarSystem"]?.Value<string>() ?? StarSystem;
                Body = evt["Body"]?.Value<string>() ?? Body;
                Latitude = evt["Latitude"]?.Value<double?>();
                Longitude = evt["Longitude"]?.Value<double?>();
                break;
            case "SupercruiseExit":
            case "ApproachBody":
                Body = evt["Body"]?.Value<string>() ?? Body;
                break;
            case "LeaveBody":
                Body = null;
                break;
            case "Touchdown":
            case "Liftoff":
                Latitude = evt["Latitude"]?.Value<double?>();
                Longitude = evt["Longitude"]?.Value<double?>();
                break;
            default:
                break;
        }

        Changed?.Invoke(this);
    }
}

public sealed class JournalProcessor
{
    private readonly GameState _state;
    private readonly Dictionary<string, long> _positions = new();
    private readonly object _sync = new();

    public GameState State => _state;

    public JournalProcessor(GameState state)
    {
        _state = state;
    }

    public void ProcessDelta(string filepath)
    {
        try
        {
            lock (_sync)
            {
                var pos = _positions.TryGetValue(filepath, out var p) ? p : 0L;
                using var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (pos > fs.Length) pos = 0; // file rotated
                fs.Seek(pos, SeekOrigin.Begin);
                using var sr = new StreamReader(fs);
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var jo = JObject.Parse(line);
                        _state.UpdateFromEvent(jo);
                    }
                    catch (Exception ex)
                    {
                        Logging.Info($"Journal parse error: {ex.Message}");
                    }
                }
                _positions[filepath] = fs.Position;
            }
        }
        catch (Exception ex)
        {
            Logging.Info($"ProcessDelta error: {ex.Message}");
        }
    }
}


