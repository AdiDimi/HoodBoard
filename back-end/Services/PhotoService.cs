using System.Diagnostics;
using AdsApi.Repositories;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Webp;
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
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="photo_upload", ["productId"]=adId, ["count"]=files?.Count }))
        using (AdsApi.Infrastructure.Logging.AuditLog.Begin())
        {
            var swOverall = Stopwatch.StartNew();
            if (string.IsNullOrWhiteSpace(adId)) throw new ArgumentException("productId is required.", nameof(adId));
            if (files is null || files.Count == 0) throw new InvalidOperationException("No files were provided.");

            var file = files[0]; // enforce single image per product
            var created = new List<Photo>(1);

            using (_log.BeginScope(new Dictionary<string, object?> { ["fileName"]=file.FileName, ["size"]=file.Length }))
            {
                ct.ThrowIfCancellationRequested();
                if (file.Length == 0) throw new InvalidOperationException("File is empty.");
                if (file.Length > _maxBytes) throw new InvalidOperationException($"File '{file.FileName}' exceeds max size.");

                var ext = Path.GetExtension(file.FileName);
                if (!AllowedExtensions.Contains(ext)) throw new InvalidOperationException($"File type '{ext}' is not allowed.");
                if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType)) throw new InvalidOperationException($"Content-Type '{file.ContentType}' is not allowed.");

                // Normalize common aliases
                ext = NormalizeExtension(ext);

                var webroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var uploads = Path.Combine(webroot, "uploads");
                var thumbs  = Path.Combine(uploads, "thumbs");
                var large   = Path.Combine(uploads, "large");
                Directory.CreateDirectory(uploads); Directory.CreateDirectory(thumbs); Directory.CreateDirectory(large);

                // Remove previous files for this product id (any known ext)
                foreach (var e in AllowedExtensions)
                {
                    TryDelete(Path.Combine(uploads,     $"{adId}{e}"));
                    TryDelete(Path.Combine(thumbs,      $"{adId}{e}"));
                    TryDelete(Path.Combine(large,       $"{adId}{e}"));
                }

                var originalName = $"{adId}{ext}";
                var originalPath = Path.Combine(uploads, originalName);
                var originalTemp = originalPath + ".tmp";

                await using (var input = file.OpenReadStream())
                await using (var outStream = File.Create(originalTemp))
                {
                    await input.CopyToAsync(outStream, ct);
                }
                File.Move(originalTemp, originalPath, overwrite: true);

                // Create resized variants, same extension
                await CreateResizedAsync(originalPath, large,  adId, LargeMaxEdge, ext, ct);
                await CreateResizedAsync(originalPath, thumbs, adId, ThumbMaxEdge, ext, ct);

                var photo = await _repo.AddPhotoAsync(adId, originalName, $"/uploads/{originalName}", ct, thumbUrl: $"/uploads/thumbs/{adId}{ext}", largeUrl: $"/uploads/large/{adId}{ext}");
                if (photo is not null) created.Add(photo);
            }

            swOverall.Stop();
            _log.LogInformation("Uploaded and replaced product image in {ElapsedMs} ms", swOverall.ElapsedMilliseconds);
            return created;
        }
    }

    private static string NormalizeExtension(string ext)
    {
        ext = ext.ToLowerInvariant();
        if (ext == ".jpeg") return ".jpg";
        return ext;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static async Task CreateResizedAsync(string sourcePath, string targetFolder, string productId, int maxEdge, string ext, CancellationToken ct)
    {
        Directory.CreateDirectory(targetFolder);
        var fileName = $"{productId}{ext}";
        var tempPath = Path.Combine(targetFolder, fileName + ".tmp");
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

        IImageEncoder encoder = ext switch
        {
            ".png" => new PngEncoder(),
            ".gif" => new GifEncoder(),
            ".webp" => new WebpEncoder(),
            _ => new JpegEncoder { Quality = 80 },
        };
        await image.SaveAsync(tempPath, encoder, ct);
        File.Move(tempPath, finalPath, overwrite: true);
    }

    private static (int width, int height) GetContainSize(int w, int h, int maxEdge)
    {
        var max = Math.Max(w, h);
        if (max <= maxEdge) return (w, h);
        var scale = (double)maxEdge / max;
        return ((int)Math.Round(w * scale), (int)Math.Round(h * scale));
    }
}
