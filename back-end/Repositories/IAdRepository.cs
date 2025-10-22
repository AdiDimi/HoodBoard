namespace AdsApi.Repositories;

public interface IAdRepository
{
    Task InitializeAsync(CancellationToken ct = default);
    IReadOnlyList<Ad> Snapshot();
    Task<Ad?> GetByIdAsync(string id);
    Task<Ad> CreateAsync(CreateAdDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(string id, UpdateAdDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<Comment?> AddCommentAsync(string adId, CreateCommentDto dto, CancellationToken ct = default);
    Task<Photo?> AddPhotoAsync(string adId, string serverFileName, string publicUrl, CancellationToken ct = default, string? thumbUrl = null, string? largeUrl = null);
    Task<bool> RemoveCommentAsync(string adId, string commentId, CancellationToken ct = default);
}
