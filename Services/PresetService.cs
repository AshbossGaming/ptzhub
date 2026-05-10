using System.Text.Json;

namespace ptz_hub.Services;

public class PresetService : IPresetService
{
    private readonly string _path;
    private Dictionary<string, (double Pan, double Tilt)> _presets = new();

    public PresetService(string path = "presets.json")
    {
        _path = path;
        Load();
    }

    public Task SaveAsync(string name, double pan, double tilt)
    {
        _presets[name] = (pan, tilt);
        Save();
        return Task.CompletedTask;
    }

    public Task<(double Pan, double Tilt)?> RecallAsync(string name)
    {
        if (_presets.TryGetValue(name, out var p))
            return Task.FromResult<(double Pan, double Tilt)?>(p);
        return Task.FromResult<(double Pan, double Tilt)?>(null);
    }

    public Task<bool> DeleteAsync(string name)
    {
        var ok = _presets.Remove(name);
        if (ok) Save();
        return Task.FromResult(ok);
    }

    public Task<Dictionary<string, object>> ListAsync()
    {
        var result = new Dictionary<string, object>();
        foreach (var (k, (pan, tilt)) in _presets)
            result[k] = new { pan, tilt };
        return Task.FromResult(result);
    }

    private void Load()
    {
        if (File.Exists(_path))
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, double[]>>(File.ReadAllText(_path));
            if (raw is not null)
                foreach (var (k, v) in raw)
                    if (v.Length == 2)
                        _presets[k] = (v[0], v[1]);
        }
    }

    private void Save()
    {
        var raw = _presets.ToDictionary(kv => kv.Key, kv => new[] { kv.Value.Pan, kv.Value.Tilt });
        File.WriteAllText(_path, JsonSerializer.Serialize(raw, new JsonSerializerOptions { WriteIndented = true }));
    }
}
