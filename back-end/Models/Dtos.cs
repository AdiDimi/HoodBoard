namespace AdsApi;

public record PhotoDto(string FileName, string Url);

public record CreateProductDto(
    string Name,
    string Description,
    string? Category,
    decimal? Price,
    int Stock,
    string? ImageUrl
);

public record UpdateProductDto(
    string? Name,
    string? Description,
    string? Category,
    decimal? Price,
    int? Stock,
    string? ImageUrl
);
