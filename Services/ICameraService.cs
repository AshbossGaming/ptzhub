namespace ptz_hub.Services;

public interface ICameraService
{
    double Zoom { get; }
    int Focus { get; }
    int Rotation { get; }
    double DigitalCrop { get; }
    bool Vflip { get; }
    Task SetZoomAsync(double zoom);
    Task SetFocusAsync(int focus);
    Task SetRotationAsync(int rotation);
    Task SetDigitalCropAsync(double crop);
    Task SetVflipAsync(bool flip);
}
