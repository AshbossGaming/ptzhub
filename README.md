# PTZ Hub

Raspberry Pi 4B PTZ camera controller with PCA9685 servo control and IMX519 camera support.

## Architecture

```
┌──────────────┐     ┌─────────────┐     ┌────────────┐
│  Web UI      │────▶│  ASP.NET    │────▶│  PCA9685   │──▶ Pan/Tilt servos
│  (desktop +  │     │  Core 8.0   │     │  (I2C)     │
│   mobile)    │◀────│  API        │     └────────────┘
└──────────────┘     └──────┬──────┘
                            │
                     ┌──────▼──────┐
                     │  Camera     │──▶ IMX519 (focus, zoom,
                     │  Service    │    digital crop, vflip)
                     └─────────────┘
```

## Endpoints

| Port | Path | Purpose |
|------|------|---------|
| 5000 | `/stream` | Adjusted camera stream state (JSON) |
| 5001 | `/ptz` | Desktop PTZ control UI |
| 5002 | `/mobile` | Mobile PTZ control UI with stream feed |

## API

- `GET /api/ptz/state` — current camera state
- `POST /api/ptz/state` — set position/zoom/focus/crop/vflip
- `POST /api/ptz/center` — center pan/tilt
- `POST /api/ptz/stop` — stop movement
- `POST /api/ptz/option` — rotation/vflip toggle
- `GET/POST /api/ptz/presets` — list/save presets
- `POST/DELETE /api/ptz/presets/{name}` — recall/delete preset

## Hardware

- **Pan/Tilt**: 0–180° range via PCA9685 (I2C addr `0x40`, bus 1), channel 0 = pan, channel 1 = tilt
- **Camera**: IMX519 focus/zoom/digital crop/rotation/vflip (software control, camera stream integration TBD)

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
```
