namespace ptz_hub.Services;

public interface IPca9685Service
{
    bool IsConnected { get; }
    void SetServoAngle(int channel, double angle);
}
