using System.Collections.Concurrent;
using System.Text.Json;
using AdsApi.Models.Converters;

namespace AdsApi.Repositories;

public sealed class InMemoryAdRepository : IAdRepository
{
    private readonly ConcurrentDictionary<string, Product> _store = new();

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        // Load initial data from Data/products.json (preferred) or Data/ads.json if present
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var productsPath = Path.Combine(baseDir, "Data", "products.json");
            var adsPath = Path.Combine(baseDir, "Data", "ads.json");
            var path = File.Exists(productsPath) ? productsPath : (File.Exists(adsPath) ? adsPath : null);
            if (path is null) return;

            await using var fs = File.OpenRead(path);
            using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: ct);
            var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            opts.Converters.Add(new StringOrNumberConverter()); // allow numeric ids

            if (doc.RootElement.TryGetProperty("products", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in arr.EnumerateArray())
                {
                    var json = el.GetRawText();
                    var p = JsonSerializer.Deserialize<Product>(json, opts);
                    if (p is null) continue;
                    if (el.TryGetProperty("imageUrl", out var imgEl) && imgEl.ValueKind == JsonValueKind.String)
                    {
                        var url = imgEl.GetString();
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            p.Photos = new List<Photo> { new Photo(Guid.NewGuid().ToString("N"), "import", url!) };
                        }
                    }
                    _store[p.Id] = p;
                }
            }
        }
        catch
        {
            // best-effort seed; ignore errors
        }
    }

    public IReadOnlyList<Product> Snapshot() => _store.Values.OrderByDescending(a => a.CreatedAt).ToList();

    public Task<Product?> GetByIdAsync(string id) => Task.FromResult(_store.TryGetValue(id, out var a) ? a : null as Product);

    public Task<Product> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            Price = dto.Price,
            Stock = dto.Stock,
        };
        if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
            product.Photos = new List<Photo> { new Photo(Guid.NewGuid().ToString("N"), "import", dto.ImageUrl) };

        _store[product.Id] = product;
        return Task.FromResult(product);
    }

    public Task<bool> UpdateAsync(string id, UpdateProductDto dto, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(id, out var product)) return Task.FromResult(false);
        product.Name = dto.Name ?? product.Name;
        product.Description = dto.Description ?? product.Description;
        product.Category = dto.Category ?? product.Category;
        product.Price = dto.Price ?? product.Price;
        if (dto.Stock is not null) product.Stock = dto.Stock.Value;
        if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
            product.Photos = new List<Photo> { new Photo(Guid.NewGuid().ToString("N"), "import", dto.ImageUrl) };
        product.UpdatedAt = DateTimeOffset.UtcNow;
        _store[id] = product;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(id, out var product)) return Task.FromResult(false);
        product.Stock = 0; // mark unavailable
        product.UpdatedAt = DateTimeOffset.UtcNow;
        _store[id] = product;
        return Task.FromResult(true);
    }

    public Task<Photo?> AddPhotoAsync(string productId, string serverFileName, string publicUrl, CancellationToken ct = default, string? thumbUrl = null, string? largeUrl = null)
    {
        if (!_store.TryGetValue(productId, out var product)) return Task.FromResult<Photo?>(null);
        var p = new Photo(Guid.NewGuid().ToString("N"), serverFileName, publicUrl, thumbUrl, largeUrl);
        product.Photos = new List<Photo> { p }; // replace existing
        product.UpdatedAt = DateTimeOffset.UtcNow;
        _store[productId] = product;
        return Task.FromResult<Photo?>(p);
    }
}
