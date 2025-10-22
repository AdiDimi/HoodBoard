using System.Diagnostics;
using AdsApi.Repositories;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace AdsApi.Services;

public sealed class PhotoService : IPhotoService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase){ ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase){ "image/jpeg", "image/png", "image/gif", "image/webp" };
    private const long DefaultMaxBytes = 5 * 1024 * 1024;
    private const int LargeMaxEdge = 1280;
    private const int ThumbMaxEdge = 320;

    private readonly IWebHostEnvironment _env;
    private readonly IAdRepository _repo;
    private readonly long _maxBytes;
    private readonly ILogger<PhotoService> _log;

    public PhotoService(IWebHostEnvironment env, IAdRepository repo, ILogger<PhotoService> log, long maxBytes = DefaultMaxBytes)
    { _env = env; _repo = repo; _log = log; _maxBytes = maxBytes; }

    public async Task<IReadOnlyList<Photo>> SaveAsync(string adId, IFormFileCollection files, CancellationToken ct = default)
    {
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="photo_upload", ["adId"]=adId, ["count"]=files?.Count }))
        using (AdsApi.Infrastructure.Logging.AuditLog.Begin())
        {
            var swOverall = Stopwatch.StartNew();
            if (string.IsNullOrWhiteSpace(adId)) throw new ArgumentException("adId is required.", nameof(adId));
            if (files is null || files.Count == 0) throw new InvalidOperationException("No files were provided.");

            var ad = await _repo.GetByIdAsync(adId);
            if (ad is null) { _log.LogWarning("Ad not found for photo upload"); throw new KeyNotFoundException($"Ad '{adId}' was not found."); }

            var webroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploads = Path.Combine(webroot, "uploads");
            var thumbs  = Path.Combine(uploads, "thumbs");
            var large   = Path.Combine(uploads, "large");
            Directory.CreateDirectory(uploads); Directory.CreateDirectory(thumbs); Directory.CreateDirectory(large);

            var created = new List<Photo>(files.Count);

            foreach (var file in files)
            {
                var sw = Stopwatch.StartNew();
                using (_log.BeginScope(new Dictionary<string, object?> { ["fileName"]=file.FileName, ["size"]=file.Length }))
                {
                    ct.ThrowIfCancellationRequested();
                    if (file.Length == 0) throw new InvalidOperationException("One or more files are empty.");
                    if (file.Length > _maxBytes) throw new InvalidOperationException($"File '{file.FileName}' exceeds max size.");

                    var ext = Path.GetExtension(file.FileName);
                    if (!AllowedExtensions.Contains(ext)) throw new InvalidOperationException($"File type '{ext}' is not allowed.");
                    if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType)) throw new InvalidOperationException($"Content-Type '{file.ContentType}' is not allowed.");

                    await using var uploadStream = file.OpenReadStream();
                    var (okMagic, normalizedExt) = await ValidateMagicAsync(uploadStream, ext, ct);
                    if (!okMagic) throw new InvalidOperationException($"'{file.FileName}' is not a recognized image file.");
                    uploadStream.Position = 0;

                    var photoId = Guid.NewGuid().ToString("N");
                    var baseName = $"{adId}_{photoId}";
                    var originalFileName = $"{baseName}{normalizedExt}";
                    var originalTemp = Path.Combine(uploads, $"{originalFileName}.tmp");
                    var originalPath = Path.Combine(uploads, originalFileName);

                    await using (var outStream = File.Create(originalTemp)) await uploadStream.CopyToAsync(outStream, ct);
                    File.Move(originalTemp, originalPath, overwrite: true);

                    var (largeName, _) = await CreateResizedAsync(originalPath, large, baseName, LargeMaxEdge, ct);
                    var (thumbName, _) = await CreateResizedAsync(originalPath, thumbs, baseName, ThumbMaxEdge, ct);

                    var publicUrl = $"/uploads/{originalFileName}";
                    var photo = await _repo.AddPhotoAsync(adId, originalFileName, publicUrl, ct, thumbUrl: $"/uploads/thumbs/{thumbName}", largeUrl: $"/uploads/large/{largeName}");
                    if (photo is not null) created.Add(photo);

                    sw.Stop();
                    _log.LogInformation("Stored photo {PhotoId} in {ElapsedMs} ms", photo?.Id, sw.ElapsedMilliseconds);
                }
            }

            swOverall.Stop();
            _log.LogInformation("Uploaded {Count} files in {ElapsedMs} ms", created.Count, swOverall.ElapsedMilliseconds);
            return created;
        }
    }

    private static async Task<(bool ok, string normalizedExt)> ValidateMagicAsync(Stream stream, string ext, CancellationToken ct)
    {
        var header = new byte[16];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (read < 12) return (false, ext);
        // JPEG
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return (true, ".jpg");
        // PNG
        if (header[0]==0x89 && header[1]==0x50 && header[2]==0x4E && header[3]==0x47 &&
            header[4]==0x0D && header[5]==0x0A && header[6]==0x1A && header[7]==0x0A) return (true, ".png");
        // GIF87a / GIF89a
        if (header[0]==0x47 && header[1]==0x49 && header[2]==0x46 && header[3]==0x38 &&
            (header[4]==0x37 || header[4]==0x39) && header[5]==0x61) return (true, ".gif");
        // WEBP (RIFF....WEBP)
        if (header[0]==0x52 && header[1]==0x49 && header[2]==0x46 && header[3]==0x46 &&
            header[8]==0x57 && header[9]==0x45 && header[10]==0x42 && header[11]==0x50) return (true, ".webp");
        return (false, ext);
    }

    private static async Task<(string fileName, string publicName)> CreateResizedAsync(string sourcePath, string targetFolder, string baseName, int maxEdge, CancellationToken ct)
    {
        Directory.CreateDirectory(targetFolder);
        var targetExt = ".jpg";
        var fileName = $"{baseName}_{maxEdge}{targetExt}";
        var tempPath = Path.Combine(targetFolder, $"{fileName}.tmp");
        var finalPath = Path.Combine(targetFolder, fileName);

        await using var fs = File.OpenRead(sourcePath);
        using var image = await Image.LoadAsync(fs, ct);
        var size = GetContainSize(image.Width, image.Height, maxEdge);
        image.Mutate(op =>
        {
            op.AutoOrient();
            if (size.width < image.Width || size.height < image.Height)
                op.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(size.width, size.height), Sampler = KnownResamplers.Lanczos3 });
        });
        var encoder = new JpegEncoder { Quality = 80 };
        await image.SaveAsync(tempPath, encoder, ct);
        File.Move(tempPath, finalPath, overwrite: true);
        return (fileName, fileName);
    }

    private static (int width, int height) GetContainSize(int w, int h, int maxEdge)
    {
        var max = Math.Max(w, h);
        if (max <= maxEdge) return (w, h);
        var scale = (double)maxEdge / max;
        return ((int)Math.Round(w * scale), (int)Math.Round(h * scale));
    }
}
