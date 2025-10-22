namespace AdsApi.Services;

public interface IPhotoService
{
    Task<IReadOnlyList<Photo>> SaveAsync(string adId, IFormFileCollection files, CancellationToken ct = default);
}
