using System.Diagnostics;
using AdsApi.Infrastructure.Logging;
using AdsApi.Repositories;

namespace AdsApi.Services;

public sealed class AdService
{
    private readonly IAdRepository _repo;
    private readonly ILogger<AdService> _log;

    public AdService(IAdRepository repo, ILogger<AdService> log) { _repo = repo; _log = log; }

    public Task<(IEnumerable<Ad> items, int total)> SearchAsync(
        string? q, string? category, decimal? minPrice, decimal? maxPrice,
        double? lat, double? lng, double? radiusKm, int page=1, int pageSize=20, string? sort=null)
    {
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="ads_search", ["search"]=q, ["category"]=category, ["page"]=page, ["pageSize"]=pageSize, ["sort"]=sort }))
        {
            var sw = Stopwatch.StartNew();
            IEnumerable<Ad> query = _repo.Snapshot().Where(a => a.IsActive);
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(a => a.Title.Contains(q, StringComparison.OrdinalIgnoreCase) || a.Body.Contains(q, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(a => a.Category == category);
            if (minPrice is not null) query = query.Where(a => a.Price >= minPrice);
            if (maxPrice is not null) query = query.Where(a => a.Price <= maxPrice);
            if (lat is not null && lng is not null && radiusKm is not null)
                query = query.Where(a => a.Location is not null && HaversineKm(lat.Value, lng.Value, a.Location!.Lat, a.Location!.Lng) <= radiusKm);
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
            return Task.FromResult(((IEnumerable<Ad>)items, total));
        }
    }

    public Task<Ad?> GetAsync(string id)
    {
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="ad_get", ["adId"]=id }))
        {
            _log.LogDebug("GetAsync invoked");
            return _repo.GetByIdAsync(id);
        }
    }

    public async Task<Ad> CreateAsync(CreateAdDto dto)
    {
        var sw = Stopwatch.StartNew();
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="ad_create", ["title"]=dto.Title }))
        using (AuditLog.Begin())
        {
            var ad = await _repo.CreateAsync(dto);
            sw.Stop();
            _log.LogInformation("Ad created {AdId} in {ElapsedMs} ms", ad.Id, sw.ElapsedMilliseconds);
            return ad;
        }
    }

    public async Task<bool> UpdateAsync(string id, UpdateAdDto dto)
    {
        var sw = Stopwatch.StartNew();
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="ad_update", ["adId"]=id }))
        using (AuditLog.Begin())
        {
            var ok = await _repo.UpdateAsync(id, dto);
            sw.Stop();
            _log.LogInformation("Ad update {AdId} status={Status} in {ElapsedMs} ms", id, ok, sw.ElapsedMilliseconds);
            return ok;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var sw = Stopwatch.StartNew();
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="ad_delete", ["adId"]=id }))
        using (AuditLog.Begin())
        {
            var ok = await _repo.DeleteAsync(id);
            sw.Stop();
            _log.LogInformation("Ad delete {AdId} status={Status} in {ElapsedMs} ms", id, ok, sw.ElapsedMilliseconds);
            return ok;
        }
    }

    public async Task<Comment?> AddCommentAsync(string adId, CreateCommentDto dto)
    {
        var sw = Stopwatch.StartNew();
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="comment_add", ["adId"]=adId }))
        {
            var c = await _repo.AddCommentAsync(adId, dto);
            sw.Stop();
            _log.LogInformation("Comment add status={Status} in {ElapsedMs} ms", c is not null, sw.ElapsedMilliseconds);
            return c;
        }
    }

    public async Task<Photo?> AddPhotoAsync(string adId, string serverFileName, string publicUrl)
    {
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="photo_link", ["adId"]=adId, ["file"]=serverFileName }))
        {
            var p = await _repo.AddPhotoAsync(adId, serverFileName, publicUrl);
            _log.LogInformation("Photo linked status={Status}", p is not null);
            return p;
        }
    }

    public static double HaversineKm(double lat1,double lon1,double lat2,double lon2)
    {
        const double R = 6371;
        double dLat = Math.PI/180*(lat2-lat1), dLon = Math.PI/180*(lon2-lon1);
        double a = Math.Sin(dLat/2)*Math.Sin(dLat/2) + Math.Cos(Math.PI/180*lat1)*Math.Cos(Math.PI/180*lat2) * Math.Sin(dLon/2)*Math.Sin(dLon/2);
        return 2*R*Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }
}
