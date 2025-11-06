using System.Diagnostics;
using System.Globalization;
using AdsApi.Infrastructure.Logging;
using AdsApi.Repositories;

namespace AdsApi.Services;

public sealed class ProductService
{
    private readonly IAdRepository _repo;
    private readonly ILogger<ProductService> _log;

    public ProductService(IAdRepository repo, ILogger<ProductService> log) { _repo = repo; _log = log; }

    public Task<(IEnumerable<Product> items, int total)> SearchAsync(
        string? q, string? category, decimal? minPrice, decimal? maxPrice,
        double? lat, double? lng, double? radiusKm, int page=1, int pageSize=20, string? sort=null)
    {
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"] = "products_search", ["search"] = q, ["category"] = category, ["page"] = page, ["pageSize"] = pageSize, ["sort"] = sort }))
        {
            var sw = Stopwatch.StartNew();
            IEnumerable<Product> query = _repo.Snapshot().Where(a => a.Stock > 0);
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(a => a.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || a.Description.Contains(q, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(a => a.Category == category);
            if (minPrice is not null) query = query.Where(a => a.Price >= minPrice);
            if (maxPrice is not null) query = query.Where(a => a.Price <= maxPrice);
            query = sort switch
            {
                "priceAsc"  => query.OrderBy(a => a.Price ?? decimal.MaxValue),
                "priceDesc" => query.OrderByDescending(a => a.Price ?? decimal.MinValue),
                _ => query.OrderByDescending(a => a.CreatedAt)
            };
            var total = query.Count();
            var items = query.Skip((page-1)*pageSize).Take(pageSize).ToArray();
            sw.Stop();
            _log.LogInformation("Search returned {Count} of {Total} in {ElapsedMs} ms", items.Length, total, sw.ElapsedMilliseconds);
            return Task.FromResult(((IEnumerable<Product>)items, total));
        }
    }

    public Task<Product?> GetAsync(string id)
    {
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"] = "product_get", ["productId"] = id }))
        {
            _log.LogDebug("GetAsync invoked");
            return _repo.GetByIdAsync(id);
        }
    }

    public async Task<Product> CreateAsync(CreateProductDto dto)
    {
        var sw = Stopwatch.StartNew();
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"] = "product_create", ["name"] = dto.Name }))
        using (AuditLog.Begin())
        {
            var product = await _repo.CreateAsync(dto);
            sw.Stop();
            _log.LogInformation("Product created {ProductId} in {ElapsedMs} ms", product.Id, sw.ElapsedMilliseconds);
            return product;
        }
    }

    public async Task<bool> UpdateAsync(string id, UpdateProductDto dto)
    {
        var sw = Stopwatch.StartNew();
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"] = "product_update", ["productId"] = id }))
        using (AuditLog.Begin())
        {
            var ok = await _repo.UpdateAsync(id, dto);
            sw.Stop();
            _log.LogInformation("Product update {ProductId} status={Status} in {ElapsedMs} ms", id, ok, sw.ElapsedMilliseconds);
            return ok;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var sw = Stopwatch.StartNew();
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"] = "product_delete", ["productId"] = id }))
        using (AuditLog.Begin())
        {
            var ok = await _repo.DeleteAsync(id);
            sw.Stop();
            _log.LogInformation("Product delete {ProductId} status={Status} in {ElapsedMs} ms", id, ok, sw.ElapsedMilliseconds);
            return ok;
        }
    }

    public async Task<Photo?> AddPhotoAsync(string productId, string serverFileName, string publicUrl)
    {
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"] = "photo_link", ["productId"] = productId, ["file"] = serverFileName }))
        {
            var p = await _repo.AddPhotoAsync(productId, serverFileName, publicUrl);
            _log.LogInformation("Photo linked status={Status}", p is not null);
            return p;
        }
    }

    public async Task<System.IO.Stream> ExportCsvAsync()
    {
        var products = _repo.Snapshot();
        // Build CSV with UTF-8 BOM to support Hebrew in Excel and browsers
        var csv = new System.Text.StringBuilder();
        // Header
        csv.AppendLine("ID,Name,Description,Price,Stock,Category,ImageFile");
        foreach (var p in products)
        {
            string Esc(string? s) => "\"" + (s ?? string.Empty).Replace("\"", "\"\"") + "\"";
            var price = p.Price.HasValue ? p.Price.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
            csv.Append(Esc(p.Id)).Append(',')
               .Append(Esc(p.Name))
               .Append(',').Append(Esc(p.Description))
               .Append(',').Append(price)
               .Append(',').Append(p.Stock.ToString(CultureInfo.InvariantCulture))
               .Append(',').Append(Esc(p.Category))
               .Append(',').Append(Esc(p.ImageUrl))
               .AppendLine();
        }
        var content = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
            .ToArray();
        return new System.IO.MemoryStream(content);
    }
}
