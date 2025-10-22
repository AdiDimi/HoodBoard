using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using AdsApi.Middleware;

namespace AdsApi.Repositories;

public sealed class AdRedisJsonRepository : IAdRepository
{
    private readonly IDatabase _db;
    private readonly string _index = "ads:index";
    private readonly string _stream = "ads-outbox";
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
    private readonly LoadedLuaScript _script;
    private readonly int _idemTtl;

    public AdRedisJsonRepository(IConnectionMultiplexer mux, IOptions<AdsRepositorySettings>? opts = null)
    {
        _db = mux.GetDatabase();
        var server = mux.GetServer(mux.GetEndPoints().First());
        _script = LuaScript.Prepare(RedisScripts.UpsertJsonWithOutboxAndIdem).Load(server);
        _idemTtl = opts?.Value?.IdempotencyTtlSeconds ?? 600;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (await _db.SetLengthAsync(_index) == 0)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "ads.json");
            if (File.Exists(path))
            {
                using var fs = File.OpenRead(path);
                var root = await JsonSerializer.DeserializeAsync<Root>(fs, _json, ct) ?? new();
                foreach (var ad in root.Ads)
                {
                    await _db.JsonSetAsync($"ads:{ad.Id}", "$", JsonSerializer.Serialize(ad, _json));
                    await _db.SetAddAsync(_index, ad.Id);
                }
            }
        }
        try { await _db.StreamCreateConsumerGroupAsync(_stream, "writer", "0-0", createStream: true); } catch {}
    }

    public IReadOnlyList<Ad> Snapshot()
    {
        var ids = _db.SetMembers(_index).Select(v => (string)v).ToArray();
        var keys = ids.Select(id => (RedisKey)$"ads:{id}").ToArray();
        if (keys.Length == 0) return new List<Ad>();
        var results = _db.JsonMGetAsync(keys, "$").Result;
        var list = new List<Ad>(keys.Length);
        foreach (var res in (RedisResult[])results!)
        {
            if (res.IsNull) continue;
            using var doc = JsonDocument.Parse((string)res!);
            var elem = doc.RootElement[0].GetRawText();
            list.Add(JsonSerializer.Deserialize<Ad>(elem, _json)!);
        }
        return list;
    }

    public async Task<Ad?> GetByIdAsync(string id)
    {
        var res = await _db.JsonGetAsync($"ads:{id}", "$");
        if (res.IsNull) return null;
        using var doc = JsonDocument.Parse((string)res!);
        var elem = doc.RootElement[0].GetRawText();
        return JsonSerializer.Deserialize<Ad>(elem, _json);
    }

    public async Task<Ad> CreateAsync(CreateAdDto dto, CancellationToken ct = default)
    {
        var ad = new Ad
        {
            Title = dto.Title, Body = dto.Body, Category = dto.Category, Price = dto.Price,
            Tags = dto.Tags?.ToList() ?? new(),
            Location = dto.Location is null ? null : new(dto.Location.Lat, dto.Location.Lng, dto.Location.Address),
            Contact  = dto.Contact  is null ? null : new(dto.Contact.Name, dto.Contact.Email, dto.Contact.Phone)
        };
        await UpsertWithIdemAsync(ad, "create");
        return ad;
    }

    public async Task<bool> UpdateAsync(string id, UpdateAdDto dto, CancellationToken ct = default)
    {
        var ad = await GetByIdAsync(id);
        if (ad is null) return false;
        ad.Title = dto.Title ?? ad.Title;
        ad.Body  = dto.Body  ?? ad.Body;
        ad.Category = dto.Category ?? ad.Category;
        ad.Price = dto.Price ?? ad.Price;
        if (dto.Tags is not null) ad.Tags = dto.Tags.ToList();
        ad.Location = dto.Location is null ? ad.Location : new(dto.Location.Lat, dto.Location.Lng, dto.Location.Address);
        ad.Contact  = dto.Contact  is null ? ad.Contact  : new(dto.Contact.Name, dto.Contact.Email, dto.Contact.Phone);
        if (dto.IsActive is not null) ad.IsActive = dto.IsActive.Value;
        ad.UpdatedAt = DateTimeOffset.UtcNow;
        await UpsertWithIdemAsync(ad, "update");
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var ad = await GetByIdAsync(id);
        if (ad is null) return false;
        ad.IsActive = false;
        ad.UpdatedAt = DateTimeOffset.UtcNow;
        await UpsertWithIdemAsync(ad, "delete");
        return true;
    }

    public async Task<Comment?> AddCommentAsync(string adId, CreateCommentDto dto, CancellationToken ct = default)
    {
        var ad = await GetByIdAsync(adId);
        if (ad is null) return null;
        var c = new Comment(Guid.NewGuid().ToString("N"), dto.AuthorName, dto.Text, DateTimeOffset.UtcNow);
        ad.Comments.Add(c); ad.UpdatedAt = DateTimeOffset.UtcNow;
        await UpsertWithIdemAsync(ad, "comment");
        return c;
    }

    public async Task<Photo?> AddPhotoAsync(string adId, string serverFileName, string publicUrl, CancellationToken ct = default, string? thumbUrl = null, string? largeUrl = null)
    {
        var ad = await GetByIdAsync(adId);
        if (ad is null) return null;
        var p = new Photo(Guid.NewGuid().ToString("N"), serverFileName, publicUrl, thumbUrl, largeUrl);
        ad.Photos.Add(p); ad.UpdatedAt = DateTimeOffset.UtcNow;
        await UpsertWithIdemAsync(ad, "photo");
        return p;
    }

    public async Task<bool> RemoveCommentAsync(string adId, string commentId, CancellationToken ct = default)
    {
        var ad = await GetByIdAsync(adId);
        if (ad is null) return false;
        var removed = ad.Comments.RemoveAll(c => c.Id == commentId) > 0;
        if (!removed) return false;
        ad.UpdatedAt = DateTimeOffset.UtcNow;
        await UpsertWithIdemAsync(ad, "comment_delete");
        return true;
    }

    private async Task UpsertWithIdemAsync(Ad ad, string op)
    {
        var adKey = $"ads:{ad.Id}";
        var idemKey = $"idem:{(AdsApi.Middleware.RequestIdemAccessor.Current ?? Guid.NewGuid().ToString("N"))}";
        var payload = JsonSerializer.Serialize(ad, _json);

        await _db.ScriptEvaluateAsync(
            RedisScripts.UpsertJsonWithOutboxAndIdem,
            new RedisKey[] { adKey, _index, _stream, idemKey },
            new RedisValue[] { payload, op, ad.Id, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(), _idemTtl }
        );
    }

    private sealed class Root { public List<Ad> Ads { get; set; } = new(); }
}
