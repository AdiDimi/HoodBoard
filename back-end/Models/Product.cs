namespace AdsApi;

public record Photo(
    string Id,
    string FileName,
    string Url,
    string? ThumbUrl = null,
    string? LargeUrl = null
);

public class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public int Stock { get; set; } = 0;
    public List<Photo> Photos { get; set; } = new();
    // Return the saved photo file name (productId + extension) for front-end usage
    public string? ImageUrl => Photos.FirstOrDefault()?.FileName;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
