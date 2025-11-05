using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using AdsApi.Middleware;
using AdsApi.Models.Converters;

namespace AdsApi.Repositories;

public sealed class AdRedisJsonRepository : IAdRepository
{
    private readonly IDatabase _db;
    private readonly string _index = "products:index";
    private readonly string _stream = "products-outbox";
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
    private readonly LoadedLuaScript _script;
    private readonly int _idemTtl;
    private readonly bool _forceRebuild;

    public AdRedisJsonRepository(IConnectionMultiplexer mux, IOptions<AdsRepositorySettings>? opts = null)
    {
        _db = mux.GetDatabase();
        var server = mux.GetServer(mux.GetEndPoints().First());
        _script = LuaScript.Prepare(RedisScripts.UpsertJsonWithOutboxAndIdem).Load(server);
        _idemTtl = opts?.Value?.IdempotencyTtlSeconds ?? 600;
        _forceRebuild = opts?.Value?.ForceRebuild ?? false;
        _json.Converters.Add(new StringOrNumberConverter());
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_forceRebuild)
        {
            await _db.ExecuteAsync("FLUSHDB");
        }

        if (await _db.SetLengthAsync(_index) == 0)
        {
            var path1 = Path.Combine(AppContext.BaseDirectory, "Data", "products.json");
            var path2 = Path.Combine(AppContext.BaseDirectory, "Data", "ads.json");
            string? pathToUse = File.Exists(path1) ? path1 : (File.Exists(path2) ? path2 : null);

            if (pathToUse != null)
            {
                using var fs = File.OpenRead(pathToUse);
                using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: ct);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("products", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in arr.EnumerateArray())
                    {
                        var p = JsonSerializer.Deserialize<Product>(el.GetRawText(), _json) ?? new Product();
                        if (el.TryGetProperty("imageUrl", out var imgEl) && imgEl.ValueKind == JsonValueKind.String)
                        {
                            var url = imgEl.GetString();
                            if (!string.IsNullOrWhiteSpace(url))
                            {
                                p.Photos = new List<Photo> { new Photo(Guid.NewGuid().ToString("N"), "import", url!) };
                            }
                        }
                        await _db.JsonSetAsync($"products:{p.Id}", "$", JsonSerializer.Serialize(p, _json));
                        await _db.SetAddAsync(_index, p.Id);
                    }
                }
            }
        }
        try { await _db.StreamCreateConsumerGroupAsync(_stream, "writer", "0-0", createStream: true); } catch {}
    }

    public IReadOnlyList<Product> Snapshot()
    {
        var ids = _db.SetMembers(_index).Select(v => (string)v).ToArray();
        var keys = ids.Select(id => (RedisKey)$"products:{id}").ToArray();
        if (keys.Length == 0) return new List<Product>();
        var results = _db.JsonMGetAsync(keys, "$").Result;
        var list = new List<Product>(keys.Length);
        foreach (var res in (RedisResult[])results!)
        {
            if (res.IsNull) continue;
            using var doc = JsonDocument.Parse((string)res!);
            var elem = doc.RootElement[0].GetRawText();
            list.Add(JsonSerializer.Deserialize<Product>(elem, _json)!);
        }
        return list;
    }

    public async Task<Product?> GetByIdAsync(string id)
    {
        var res = await _db.JsonGetAsync($"products:{id}", "$");
        if (res.IsNull) return null;
        using var doc = JsonDocument.Parse((string)res!);
        var elem = doc.RootElement[0].GetRawText();
        return JsonSerializer.Deserialize<Product>(elem, _json);
    }

    public async Task<Product> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            Price = dto.Price,
            Stock = dto.Stock
        };
        if (!string.IsNullOrWhiteSpace(dto.ImageUrl)) product.Photos = new List<Photo> { new Photo(Guid.NewGuid().ToString("N"), "import", dto.ImageUrl) };
        await UpsertWithIdemAsync(product, "create");
        return product;
    }

    public async Task<bool> UpdateAsync(string id, UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await GetByIdAsync(id);
        if (product is null) return false;
        product.Name = dto.Name ?? product.Name;
        product.Description = dto.Description ?? product.Description;
        product.Category = dto.Category ?? product.Category;
        product.Price = dto.Price ?? product.Price;
        if (dto.Stock is not null) product.Stock = dto.Stock.Value;
        if (!string.IsNullOrWhiteSpace(dto.ImageUrl)) product.Photos = new List<Photo> { new Photo(Guid.NewGuid().ToString("N"), "import", dto.ImageUrl) };
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await UpsertWithIdemAsync(product, "update");
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var product = await GetByIdAsync(id);
        if (product is null) return false;
        product.Stock = 0;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await UpsertWithIdemAsync(product, "delete");
        return true;
    }

    public async Task<Photo?> AddPhotoAsync(string productId, string serverFileName, string publicUrl, CancellationToken ct = default, string? thumbUrl = null, string? largeUrl = null)
    {
        var product = await GetByIdAsync(productId);
        if (product is null) return null;
        var p = new Photo(Guid.NewGuid().ToString("N"), serverFileName, publicUrl, thumbUrl, largeUrl);
        product.Photos = new List<Photo> { p }; // replace existing
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await UpsertWithIdemAsync(product, "photo");
        return p;
    }

    private async Task UpsertWithIdemAsync(Product product, string op)
    {
        var productKey = $"products:{product.Id}";
        var idemKey = $"idem:{(AdsApi.Middleware.RequestIdemAccessor.Current ?? Guid.NewGuid().ToString("N"))}";
        var payload = JsonSerializer.Serialize(product, _json);

        await _db.ScriptEvaluateAsync(
            RedisScripts.UpsertJsonWithOutboxAndIdem,
            new RedisKey[] { productKey, _index, _stream, idemKey },
            new RedisValue[] { payload, op, product.Id, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(), _idemTtl }
        );
    }
}
