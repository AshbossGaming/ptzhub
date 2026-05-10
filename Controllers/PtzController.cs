using Microsoft.AspNetCore.Mvc;
using ptz_hub.Services;

namespace ptz_hub.Controllers;

[ApiController]
[Route("api/ptz")]
public class PtzController : ControllerBase
{
    private readonly IServoService _servo;
    private readonly ICameraService _camera;
    private readonly IPresetService _presets;

    public PtzController(IServoService servo, ICameraService camera, IPresetService presets)
    {
        _servo = servo;
        _camera = camera;
        _presets = presets;
    }

    [HttpGet("state")]
    public IActionResult GetState()
    {
        return Ok(MakeState());
    }

    [HttpPost("state")]
    public async Task<IActionResult> SetState([FromBody] PtzStateRequest request)
    {
        var speed = request.Speed ?? 45;

        if (request.Pan.HasValue && request.Tilt.HasValue)
            await _servo.MoveAsync(request.Pan.Value, request.Tilt.Value, speed);
        else if (request.Pan.HasValue)
            await _servo.MovePanAsync(request.Pan.Value, speed);
        else if (request.Tilt.HasValue)
            await _servo.MoveTiltAsync(request.Tilt.Value, speed);

        if (request.Zoom.HasValue)
            await _camera.SetZoomAsync(request.Zoom.Value);
        if (request.Focus.HasValue)
            await _camera.SetFocusAsync(request.Focus.Value);
        if (request.DigitalCrop.HasValue)
            await _camera.SetDigitalCropAsync(request.DigitalCrop.Value);
        if (request.Vflip.HasValue)
            await _camera.SetVflipAsync(request.Vflip.Value);

        return Ok(MakeState());
    }

    private object MakeState()
    {
        return new
        {
            pan = Math.Round(_servo.Pan, 1),
            tilt = Math.Round(_servo.Tilt, 1),
            zoom = Math.Round(_camera.Zoom, 1),
            focus = _camera.Focus,
            vflip = _camera.Vflip,
            rotation = _camera.Rotation,
            digitalCrop = Math.Round(_camera.DigitalCrop, 1)
        };
    }

    [HttpPost("center")]
    public async Task<IActionResult> Center([FromBody] CenterRequest? request = null)
    {
        await _servo.MoveCenterAsync(request?.Speed ?? 45);
        return Ok(new { status = "centering" });
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop()
    {
        await _servo.StopAsync();
        return Ok(new { status = "stopped" });
    }

    [HttpPost("option")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> SetOption([FromForm] OptionRequest request)
    {
        if (request.Rotation.HasValue)
            await _camera.SetRotationAsync(request.Rotation.Value);
        if (request.Vflip.HasValue)
            await _camera.SetVflipAsync(request.Vflip.Value);
        return Ok(MakeState());
    }

    [HttpGet("presets")]
    public async Task<IActionResult> ListPresets()
    {
        var list = await _presets.ListAsync();
        return Ok(list);
    }

    [HttpPost("presets")]
    public async Task<IActionResult> SavePreset([FromBody] SavePresetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "name required" });

        var pan = request.Pan ?? _servo.Pan;
        var tilt = request.Tilt ?? _servo.Tilt;
        await _presets.SaveAsync(request.Name, pan, tilt);
        return Ok(new { status = "saved", name = request.Name });
    }

    [HttpPost("presets/{name}")]
    public async Task<IActionResult> RecallPreset(string name, [FromBody] RecallPresetRequest? request = null)
    {
        var p = await _presets.RecallAsync(name);
        if (p is null)
            return NotFound(new { error = $"preset '{name}' not found" });

        var speed = request?.Speed ?? 45;
        await _servo.MoveAsync(p.Value.Pan, p.Value.Tilt, speed);
        return Ok(new { status = "recalling", name, pan = p.Value.Pan, tilt = p.Value.Tilt });
    }

    [HttpDelete("presets/{name}")]
    public async Task<IActionResult> DeletePreset(string name)
    {
        var ok = await _presets.DeleteAsync(name);
        return ok ? Ok(new { deleted = true }) : NotFound(new { deleted = false });
    }
}

public class PtzStateRequest
{
    public double? Pan { get; init; }
    public double? Tilt { get; init; }
    public double? Zoom { get; init; }
    public int? Focus { get; init; }
    public int? Speed { get; init; }
    public double? DigitalCrop { get; init; }
    public bool? Vflip { get; init; }
}

public class CenterRequest
{
    public int? Speed { get; init; }
}

public class SavePresetRequest
{
    public string Name { get; init; } = "";
    public double? Pan { get; init; }
    public double? Tilt { get; init; }
}

public class RecallPresetRequest
{
    public int? Speed { get; init; }
}

public class OptionRequest
{
    public int? Rotation { get; init; }
    public bool? Vflip { get; init; }
}
