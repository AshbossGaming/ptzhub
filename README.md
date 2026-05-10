# PTZ Hub

Raspberry Pi 4B PTZ camera controller. Controls pan/tilt servos via PCA9685 (I2C), proxies the camera feed from a separate camera service, applies software transforms (rotation, flip, digital crop), and serves the adjusted MJPEG stream + control UIs.

## Architecture

```
                    ┌─────────────────────┐
                    │  Camera Service     │
                    │  (separate process) │──▶ IMX519 camera
                    │  port 8080          │    (focus, zoom, etc.)
                    └─────────┬───────────┘
                              │ MJPEG stream
                              ▼
┌──────────────┐     ┌──────────────────┐     ┌────────────┐
│  Web UI      │────▶│  ASP.NET Core 8  │────▶│  PCA9685   │──▶ Pan/Tilt servos
│  (desktop +  │     │                  │     │  (I2C)     │
│   mobile)    │◀────│  API + Stream    │     └────────────┘
└──────────────┘     └────────┬─────────┘
                              │
                              ▼
                    ┌─────────────────────┐
                    │  /stream (port 5000)│
                    │  MJPEG with applied │
                    │  transforms         │
                    └─────────────────────┘
```

The Camera Service is a separate process on the Pi (e.g. `ustreamer`, `mjpeg-streamer`, or a custom application) that:
- Streams MJPEG video from the IMX519 at port 8080
- Handles all camera hardware control (exposure, gain, lens, sensor)

PTZ Hub pulls that stream, applies software transforms based on PTZ state, and re-serves it as a new MJPEG stream at port 5000.

**Transforms applied per frame:**

| Condition | Transform |
|-----------|-----------|
| `tilt > 90°` | Rotate 180° (camera physically inverted) |
| `rotation == 180` | Rotate 180° (user toggle, XOR with tilt) |
| `vflip == true` | Vertical flip |
| `digitalCrop > 1` | Center crop by `1/digitalCrop` |
| No transforms needed | Zero-copy passthrough (skips ImageSharp) |

## Endpoints

| Port | Path | Purpose |
|------|------|---------|
| 5000 | `/stream` | Adjusted camera stream (MJPEG, multipart/x-mixed-replace) |
| 5001 | `/ptz` | Desktop PTZ control UI |
| 5002 | `/mobile` | Mobile PTZ control UI with live stream viewer |

## API

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/ptz/state` | Current camera state (pan, tilt, zoom, focus, vflip, rotation, digitalCrop, autoFocus) |
| `POST` | `/api/ptz/state` | Set position / zoom / focus / digitalCrop / vflip / autoFocus |
| `POST` | `/api/ptz/center` | Center pan/tilt to 90° |
| `POST` | `/api/ptz/stop` | Stop servo movement |
| `POST` | `/api/ptz/option` | Toggle rotation (0/180) or vflip |
| `GET` | `/api/ptz/presets` | List saved presets |
| `POST` | `/api/ptz/presets` | Save current position as preset |
| `POST` | `/api/ptz/presets/{name}` | Recall a preset |
| `DELETE` | `/api/ptz/presets/{name}` | Delete a preset |

## Hardware

- **Pan/Tilt**: 0–180° range via PCA9685 PWM driver
  - I2C bus 1, address `0x40`
  - Channel 0: pan servo
  - Channel 1: tilt servo
  - 50Hz PWM, 0.5–2.5ms pulse width
- **Camera**: IMX519 (controlled by external camera service on port 8080)
  - Autofocus toggle forwarded to camera service API

## Configuration (`appsettings.json`)

```json
{
  "CameraService": {
    "StreamUrl": "http://localhost:8080/stream",
    "ApiUrl": "http://localhost:8080/api"
  }
}
```

- `StreamUrl`: MJPEG source from camera service
- `ApiUrl`: Control API for autofocus (`POST /api/autofocus` with `on`/`off`)

## Build & Deploy

```bash
# Build for Pi
dotnet publish -r linux-arm64 -c Release --self-contained

# Copy to Pi, then run
./ptz\ hub
```

## Pi Setup

```bash
# Enable I2C
sudo raspi-config nonint do_i2c 0
sudo usermod -aG i2c $USER

# Install .NET 8 runtime
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0

# Install libgdiplus (for System.Drawing fallback, not required with ImageSharp)
sudo apt install -y libgdiplus
```
