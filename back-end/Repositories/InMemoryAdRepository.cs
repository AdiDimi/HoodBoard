using System.Collections.Concurrent;

namespace AdsApi.Repositories;

public sealed class InMemoryAdRepository : IAdRepository
{
    private readonly ConcurrentDictionary<string, Product> _store = new();

    public Task InitializeAsync(CancellationToken ct = default)
    {
        // nothing to init
        return Task.CompletedTask;
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
        if (!string.IsNullOrWhiteSpace(dto.ImageUrl)) product.Photos.Add(new Photo(Guid.NewGuid().ToString("N"), "import", dto.ImageUrl));
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
        if (!string.IsNullOrWhiteSpace(dto.ImageUrl)) product.Photos.Insert(0, new Photo(Guid.NewGuid().ToString("N"), "import", dto.ImageUrl));
        product.UpdatedAt = DateTimeOffset.UtcNow;
        _store[id] = product;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(id, out var product)) return Task.FromResult(false);
        // mark as deleted by setting stock to 0
        product.Stock = 0;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        _store[id] = product;
        return Task.FromResult(true);
    }

    public Task<Photo?> AddPhotoAsync(string productId, string serverFileName, string publicUrl, CancellationToken ct = default, string? thumbUrl = null, string? largeUrl = null)
    {
        if (!_store.TryGetValue(productId, out var product)) return Task.FromResult<Photo?>(null);
        var p = new Photo(Guid.NewGuid().ToString("N"), serverFileName, publicUrl, thumbUrl, largeUrl);
        product.Photos.Add(p);
        product.UpdatedAt = DateTimeOffset.UtcNow;
        _store[productId] = product;
        return Task.FromResult<Photo?>(p);
    }
}
