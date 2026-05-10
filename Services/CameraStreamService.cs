using System.Buffers;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace ptz_hub.Services;

public class CameraStreamService : BackgroundService
{
    private readonly Channel<byte[]> _channel = Channel.CreateBounded<byte[]>(
        new BoundedChannelOptions(3) { FullMode = BoundedChannelFullMode.DropOldest });
    private readonly IServoService _servo;
    private readonly ICameraService _camera;
    private readonly IConfiguration _config;
    private static readonly Regex BoundaryRx = new(@"boundary=(.+?)(?:;|$)", RegexOptions.Compiled);

    public ChannelReader<byte[]> Reader => _channel.Reader;

    public CameraStreamService(IServoService servo, ICameraService camera, IConfiguration config)
    {
        _servo = servo;
        _camera = camera;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var url = _config["CameraService:StreamUrl"] ?? "http://localhost:8080/stream";
        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                resp.EnsureSuccessStatusCode();
                using var stream = await resp.Content.ReadAsStreamAsync(ct);

                var ctStr = resp.Content.Headers.ContentType?.ToString() ?? "";
                var m = BoundaryRx.Match(ctStr);
                var boundary = m.Success ? m.Groups[1].Value : "--frame";
                using var reader = new StreamReader(stream);

                await ProcessFrames(reader, stream, boundary, ct);
            }
            catch when (!ct.IsCancellationRequested)
            {
                await Task.Delay(2000, ct);
            }
        }
    }

    private async Task ProcessFrames(StreamReader reader, Stream raw, string boundary, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (!line.AsSpan().TrimStart().StartsWith(boundary.AsSpan())) continue;

            int contentLength = 0;
            while ((line = await reader.ReadLineAsync(ct)) is not null)
            {
                if (line.Length == 0) break;
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(line.AsSpan("Content-Length:".Length), out contentLength);
            }
            if (contentLength <= 0) continue;

            var jpeg = ArrayPool<byte>.Shared.Rent(contentLength);
            try
            {
                int offset = 0;
                while (offset < contentLength)
                {
                    var read = await raw.ReadAsync(jpeg.AsMemory(offset, contentLength - offset), ct);
                    if (read == 0) break;
                    offset += read;
                }
                if (offset < contentLength) continue;

                var result = ProcessFrame(jpeg, contentLength);
                await _channel.Writer.WriteAsync(result, ct);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(jpeg);
            }
        }
    }

    private byte[] ProcessFrame(byte[] buffer, int length)
    {
        var tilt = _servo.Tilt;
        var vflip = _camera.Vflip;
        var crop = _camera.DigitalCrop;
        var rotation = _camera.Rotation;

        bool need180 = (tilt > 90) ^ (rotation == 180);
        bool needFlip = vflip;
        bool needCrop = crop > 1.01;

        if (!need180 && !needFlip && !needCrop)
        {
            var result = new byte[length];
            Buffer.BlockCopy(buffer, 0, result, 0, length);
            return result;
        }

        using var image = Image.Load<Rgba32>(buffer.AsSpan(0, length));
        image.Mutate(ctx =>
        {
            if (need180) ctx.Rotate(180);
            if (needFlip) ctx.Flip(FlipMode.Vertical);
            if (needCrop)
            {
                var size = (int)(Math.Min(image.Width, image.Height) / crop);
                var x = (image.Width - size) / 2;
                var y = (image.Height - size) / 2;
                ctx.Crop(new Rectangle(x, y, size, size));
            }
        });

        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }
}
