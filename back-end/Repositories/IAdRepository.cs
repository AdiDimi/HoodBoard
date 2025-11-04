namespace AdsApi.Repositories;

public interface IAdRepository
{
    Task InitializeAsync(CancellationToken ct = default);
    IReadOnlyList<Product> Snapshot();
    Task<Product?> GetByIdAsync(string id);
    Task<Product> CreateAsync(CreateProductDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(string id, UpdateProductDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<Photo?> AddPhotoAsync(string productId, string serverFileName, string publicUrl, CancellationToken ct = default, string? thumbUrl = null, string? largeUrl = null);
}
