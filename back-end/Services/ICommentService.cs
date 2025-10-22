namespace AdsApi.Services;

public interface ICommentService
{
    Task<IReadOnlyList<Comment>> ListAsync(string adId, CancellationToken ct = default);
    Task<Comment> AddAsync(string adId, CreateCommentDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string adId, string commentId, CancellationToken ct = default);
}
