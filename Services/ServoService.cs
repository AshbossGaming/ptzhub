namespace ptz_hub.Services;

public class ServoService : IServoService
{
    private readonly object _lock = new();
    private readonly IPca9685Service _pca9685;
    private double _pan = 90;
    private double _tilt = 90;
    private CancellationTokenSource? _cts;

    public ServoService(IPca9685Service pca9685)
    {
        _pca9685 = pca9685;
        _pca9685.SetServoAngle(0, _pan);
        _pca9685.SetServoAngle(1, _tilt);
    }

    public double Pan
    {
        get { lock (_lock) return _pan; }
    }

    public double Tilt
    {
        get { lock (_lock) return _tilt; }
    }

    public Task MovePanAsync(double angle, int speed = 45)
    {
        return MoveAsync(angle, Tilt, speed);
    }

    public Task MoveTiltAsync(double angle, int speed = 45)
    {
        return MoveAsync(Pan, angle, speed);
    }

    public async Task MoveAsync(double pan, double tilt, int speed = 45)
    {
        var clampedPan = Math.Clamp(pan, 0, 180);
        var clampedTilt = Math.Clamp(tilt, 0, 180);

        lock (_lock)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
        }

        CancellationToken token;
        double startPan, startTilt;
        lock (_lock)
        {
            token = _cts.Token;
            startPan = _pan;
            startTilt = _tilt;
        }

        var panDist = Math.Abs(clampedPan - startPan);
        var tiltDist = Math.Abs(clampedTilt - startTilt);
        var maxDist = Math.Max(panDist, tiltDist);
        var duration = maxDist / Math.Max(speed, 1);
        if (duration < 0.01)
        {
            lock (_lock)
            {
                _pan = clampedPan;
                _tilt = clampedTilt;
                _pca9685.SetServoAngle(0, _pan);
                _pca9685.SetServoAngle(1, _tilt);
            }
            return;
        }

        var interval = 0.02;
        var steps = Math.Max(1, (int)(duration / interval));

        for (var i = 0; i <= steps; i++)
        {
            if (token.IsCancellationRequested)
                return;

            var t = (double)i / steps;
            var eased = t * t * (3 - 2 * t);
            lock (_lock)
            {
                _pan = startPan + (clampedPan - startPan) * eased;
                _tilt = startTilt + (clampedTilt - startTilt) * eased;
                _pca9685.SetServoAngle(0, _pan);
                _pca9685.SetServoAngle(1, _tilt);
            }

            await Task.Delay((int)(interval * 1000), token).ContinueWith(_ => { });
        }

        lock (_lock)
        {
            _pan = clampedPan;
            _tilt = clampedTilt;
            _pca9685.SetServoAngle(0, _pan);
            _pca9685.SetServoAngle(1, _tilt);
        }
    }

    public Task MoveCenterAsync(int speed = 45)
    {
        return MoveAsync(90, 90, speed);
    }

    public Task StopAsync()
    {
        lock (_lock)
        {
            _cts?.Cancel();
            _cts = null;
        }

        return Task.CompletedTask;
    }
}
