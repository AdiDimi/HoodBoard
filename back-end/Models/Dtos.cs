namespace AdsApi;

public record LocationDto(double Lat, double Lng, string? Address);
public record ContactDto(string? Name, string? Email, string? Phone);

public record CreateAdDto(
    string Title,
    string Body,
    string? Category,
    decimal? Price,
    string[]? Tags,
    LocationDto? Location,
    ContactDto? Contact
);

public record UpdateAdDto(
    string? Title,
    string? Body,
    string? Category,
    decimal? Price,
    string[]? Tags,
    LocationDto? Location,
    ContactDto? Contact,
    bool? IsActive
);

public record CreateCommentDto(string AuthorName, string Text);
