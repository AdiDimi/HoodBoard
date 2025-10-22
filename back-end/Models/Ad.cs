namespace AdsApi;

public record Location(double Lat, double Lng, string? Address);
public record Contact(string? Name, string? Email, string? Phone);

public record Comment(string Id, string AuthorName, string Text, DateTimeOffset CreatedAt);

public record Photo(
    string Id,
    string FileName,
    string Url,
    string? ThumbUrl = null,
    string? LargeUrl = null
);

public class Ad
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public List<string> Tags { get; set; } = new();
    public Location? Location { get; set; }
    public Contact? Contact { get; set; }
    public List<Comment> Comments { get; set; } = new();
    public List<Photo> Photos { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
