using System.Diagnostics;
using AdsApi.Errors;
using AdsApi.Repositories;

namespace AdsApi.Services;

public sealed class CommentService : ICommentService
{
    private readonly IAdRepository _repo;
    private readonly ILogger<CommentService> _log;

    public CommentService(IAdRepository repo, ILogger<CommentService> log) { _repo = repo; _log = log; }

    public async Task<IReadOnlyList<Comment>> ListAsync(string adId, CancellationToken ct = default)
    {
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="comment_list", ["adId"]=adId }))
        {
            var sw = Stopwatch.StartNew();
            var ad = await _repo.GetByIdAsync(adId);
            if (ad is null) { _log.LogWarning("Ad not found"); throw new NotFoundException($"Ad '{adId}' not found."); }
            sw.Stop();
            _log.LogInformation("Returned {Count} comments in {ElapsedMs} ms", ad.Comments.Count, sw.ElapsedMilliseconds);
            return ad.Comments.AsReadOnly();
        }
    }

    public async Task<Comment> AddAsync(string adId, CreateCommentDto dto, CancellationToken ct = default)
    {
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="comment_add", ["adId"]=adId }))
        {
            var sw = Stopwatch.StartNew();
            var created = await _repo.AddCommentAsync(adId, dto, ct);
            if (created is null) { _log.LogWarning("Ad not found while adding comment"); throw new NotFoundException($"Ad '{adId}' not found."); }
            sw.Stop();
            _log.LogInformation("Comment {CommentId} added in {ElapsedMs} ms", created.Id, sw.ElapsedMilliseconds);
            return created;
        }
    }

    public async Task<bool> DeleteAsync(string adId, string commentId, CancellationToken ct = default)
    {
        using (_log.BeginScope(new Dictionary<string, object?> { ["op"]="comment_delete", ["adId"]=adId, ["commentId"]=commentId }))
        using (AdsApi.Infrastructure.Logging.AuditLog.Begin())
        {
            var sw = Stopwatch.StartNew();
            var ok = await _repo.RemoveCommentAsync(adId, commentId, ct);
            sw.Stop();
            _log.LogInformation("Comment delete status={Status} in {ElapsedMs} ms", ok, sw.ElapsedMilliseconds);
            return ok;
        }
    }
}
