namespace ptz_hub.Services;

public interface IServoService
{
    double Pan { get; }
    double Tilt { get; }
    Task MovePanAsync(double angle, int speed = 45);
    Task MoveTiltAsync(double angle, int speed = 45);
    Task MoveAsync(double pan, double tilt, int speed = 45);
    Task MoveCenterAsync(int speed = 45);
    Task StopAsync();
}
