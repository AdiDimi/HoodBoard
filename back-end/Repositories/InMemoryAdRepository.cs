using System.Collections.Concurrent;

namespace AdsApi.Repositories;

public sealed class InMemoryAdRepository : IAdRepository
{
    private readonly ConcurrentDictionary<string, Ad> _store = new();

    public Task InitializeAsync(CancellationToken ct = default)
    {
        // nothing to init
        return Task.CompletedTask;
    }

    public IReadOnlyList<Ad> Snapshot() => _store.Values.OrderByDescending(a => a.CreatedAt).ToList();

    public Task<Ad?> GetByIdAsync(string id) => Task.FromResult(_store.TryGetValue(id, out var a) ? a : null as Ad);

    public Task<Ad> CreateAsync(CreateAdDto dto, CancellationToken ct = default)
    {
        var ad = new Ad
        {
            Title = dto.Title,
            Body = dto.Body,
            Category = dto.Category,
            Price = dto.Price,
            Tags = dto.Tags?.ToList() ?? new(),
            Location = dto.Location is null ? null : new Location(dto.Location.Lat, dto.Location.Lng, dto.Location.Address),
            Contact = dto.Contact is null ? null : new Contact(dto.Contact.Name, dto.Contact.Email, dto.Contact.Phone)
        };
        _store[ad.Id] = ad;
        return Task.FromResult(ad);
    }

    public Task<bool> UpdateAsync(string id, UpdateAdDto dto, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(id, out var ad)) return Task.FromResult(false);
        ad.Title = dto.Title ?? ad.Title;
        ad.Body = dto.Body ?? ad.Body;
        ad.Category = dto.Category ?? ad.Category;
        ad.Price = dto.Price ?? ad.Price;
        if (dto.Tags is not null) ad.Tags = dto.Tags.ToList();
        ad.Location = dto.Location is null ? ad.Location : new Location(dto.Location.Lat, dto.Location.Lng, dto.Location.Address);
        ad.Contact = dto.Contact is null ? ad.Contact : new Contact(dto.Contact.Name, dto.Contact.Email, dto.Contact.Phone);
        if (dto.IsActive is not null) ad.IsActive = dto.IsActive.Value;
        ad.UpdatedAt = DateTimeOffset.UtcNow;
        _store[id] = ad;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(id, out var ad)) return Task.FromResult(false);
        ad.IsActive = false;
        ad.UpdatedAt = DateTimeOffset.UtcNow;
        _store[id] = ad;
        return Task.FromResult(true);
    }

    public Task<Comment?> AddCommentAsync(string adId, CreateCommentDto dto, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(adId, out var ad)) return Task.FromResult<Comment?>(null);
        var c = new Comment(Guid.NewGuid().ToString("N"), dto.AuthorName, dto.Text, DateTimeOffset.UtcNow);
        ad.Comments.Add(c);
        ad.UpdatedAt = DateTimeOffset.UtcNow;
        _store[adId] = ad;
        return Task.FromResult<Comment?>(c);
    }

    public Task<Photo?> AddPhotoAsync(string adId, string serverFileName, string publicUrl, CancellationToken ct = default, string? thumbUrl = null, string? largeUrl = null)
    {
        if (!_store.TryGetValue(adId, out var ad)) return Task.FromResult<Photo?>(null);
        var p = new Photo(Guid.NewGuid().ToString("N"), serverFileName, publicUrl, thumbUrl, largeUrl);
        ad.Photos.Add(p);
        ad.UpdatedAt = DateTimeOffset.UtcNow;
        _store[adId] = ad;
        return Task.FromResult<Photo?>(p);
    }

    public Task<bool> RemoveCommentAsync(string adId, string commentId, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(adId, out var ad)) return Task.FromResult(false);
        var removed = ad.Comments.RemoveAll(c => c.Id == commentId) > 0;
        if (!removed) return Task.FromResult(false);
        ad.UpdatedAt = DateTimeOffset.UtcNow;
        _store[adId] = ad;
        return Task.FromResult(true);
    }
}
