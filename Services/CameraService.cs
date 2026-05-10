namespace ptz_hub.Services;

public class CameraService : ICameraService
{
    private readonly object _lock = new();
    private readonly HttpClient _http;
    private readonly string _apiUrl;
    private double _zoom = 1;
    private int _focus = 50;
    private int _rotation = 0;
    private double _digitalCrop = 1;
    private bool _vflip = false;
    private bool _autoFocus = false;

    public CameraService(IConfiguration config)
    {
        _apiUrl = config["CameraService:ApiUrl"] ?? "http://localhost:8080/api";
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    }

    public double Zoom
    {
        get { lock (_lock) return _zoom; }
    }

    public int Focus
    {
        get { lock (_lock) return _focus; }
    }

    public int Rotation
    {
        get { lock (_lock) return _rotation; }
    }

    public double DigitalCrop
    {
        get { lock (_lock) return _digitalCrop; }
    }

    public bool Vflip
    {
        get { lock (_lock) return _vflip; }
    }

    public bool AutoFocus
    {
        get { lock (_lock) return _autoFocus; }
    }

    public Task SetZoomAsync(double zoom)
    {
        lock (_lock) _zoom = Math.Clamp(zoom, 1, 10);
        return Task.CompletedTask;
    }

    public Task SetFocusAsync(int focus)
    {
        lock (_lock) _focus = Math.Clamp(focus, 0, 100);
        return Task.CompletedTask;
    }

    public Task SetRotationAsync(int rotation)
    {
        lock (_lock)
        {
            _rotation = rotation % 360;
            if (_rotation < 0) _rotation += 360;
        }
        return Task.CompletedTask;
    }

    public Task SetDigitalCropAsync(double crop)
    {
        lock (_lock) _digitalCrop = Math.Clamp(crop, 1, 4);
        return Task.CompletedTask;
    }

    public Task SetVflipAsync(bool flip)
    {
        lock (_lock) _vflip = flip;
        return Task.CompletedTask;
    }

    public async Task SetAutoFocusAsync(bool enabled)
    {
        lock (_lock) _autoFocus = enabled;
        try
        {
            var val = enabled ? "on" : "off";
            using var resp = await _http.PostAsync($"{_apiUrl}/autofocus",
                new StringContent(val));
            resp.EnsureSuccessStatusCode();
        }
        catch
        {
            // camera service unreachable — state still tracked locally
        }
    }
}
