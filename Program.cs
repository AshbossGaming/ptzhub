using System.Net;
using ptz_hub.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IPca9685Service, Pca9685Service>();
builder.Services.AddSingleton<ICameraService, CameraService>();
builder.Services.AddSingleton<IPresetService, PresetService>();
builder.Services.AddSingleton<IServoService, ServoService>();
builder.Services.AddSingleton<CameraStreamService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CameraStreamService>());

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

app.MapGet("/stream", async (HttpContext ctx, CameraStreamService stream) =>
{
    ctx.Response.ContentType = "multipart/x-mixed-replace; boundary=frame";
    ctx.Response.Headers.Append("Cache-Control", "no-cache");
    ctx.Response.Headers.Append("Connection", "keep-alive");

    var ct = ctx.RequestAborted;
    await foreach (var frame in stream.Reader.ReadAllAsync(ct))
    {
        var header = $"--frame\r\nContent-Type: image/jpeg\r\nContent-Length: {frame.Length}\r\n\r\n";
        await ctx.Response.WriteAsync(header, ct);
        await ctx.Response.Body.WriteAsync(frame, ct);
        await ctx.Response.WriteAsync("\r\n", ct);
        await ctx.Response.Body.FlushAsync(ct);
    }
});

app.Run();
