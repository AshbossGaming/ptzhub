using System.Device.I2c;

namespace ptz_hub.Services;

public class Pca9685Service : IPca9685Service, IDisposable
{
    private I2cDevice? _device;
    private const int Frequency = 50;

    public bool IsConnected => _device is not null;

    public Pca9685Service()
    {
        try
        {
            var settings = new I2cConnectionSettings(1, 0x40);
            _device = I2cDevice.Create(settings);
            Init();
        }
        catch
        {
            _device = null;
        }
    }

    private void Init()
    {
        if (_device is null) return;

        WriteReg(0x00, 0x10);
        WriteReg(0xFE, 121);
        WriteReg(0x00, 0x00);
        WriteReg(0x01, 0x04);

        Thread.Sleep(1);
    }

    public void SetServoAngle(int channel, double angle)
    {
        if (_device is null || channel is < 0 or > 15) return;

        var clamped = Math.Clamp(angle, 0, 180);
        var pulseMs = 0.5 + (clamped / 180.0) * 2.0;
        var counts = (int)(pulseMs * 4096 / (1000.0 / Frequency));

        _device.Write(new byte[]
        {
            (byte)(0x06 + 4 * channel),
            0x00,
            0x00,
            (byte)(counts & 0xFF),
            (byte)((counts >> 8) & 0x0F)
        });
    }

    private void WriteReg(byte reg, byte value)
    {
        _device?.Write(new[] { reg, value });
    }

    public void Dispose()
    {
        _device?.Dispose();
    }
}
