using System.Net;
using ptz_hub.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IPca9685Service, Pca9685Service>();
builder.Services.AddSingleton<IServoService, ServoService>();
builder.Services.AddSingleton<ICameraService, CameraService>();
builder.Services.AddSingleton<IPresetService, PresetService>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, 5000);
    options.Listen(IPAddress.Any, 5001);
    options.Listen(IPAddress.Any, 5002);
});

var app = builder.Build();

app.UseDefaultFiles(new DefaultFilesOptions { RequestPath = "/ptz" });
app.UseStaticFiles(new StaticFileOptions { RequestPath = "/ptz" });
app.UseDefaultFiles(new DefaultFilesOptions { RequestPath = "/mobile" });
app.UseStaticFiles(new StaticFileOptions { RequestPath = "/mobile" });
app.MapControllers();

app.MapGet("/stream", (ICameraService camera, IServoService servo) =>
{
    var state = new
    {
        pan = Math.Round(servo.Pan, 1),
        tilt = Math.Round(servo.Tilt, 1),
        zoom = Math.Round(camera.Zoom, 1),
        focus = camera.Focus,
        rotation = camera.Rotation,
        digitalCrop = Math.Round(camera.DigitalCrop, 1),
        vflip = camera.Vflip
    };
    return Results.Json(state);
});

app.Run();
