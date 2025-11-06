using AdsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdsApi.Endpoints;

public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithOpenApi();
        var products = api.MapGroup("products").WithTags("Products");

        products.MapGet("/", async ([AsParameters] Query query, ProductService svc, HttpResponse res) =>
        {
            var (items, total) = await svc.SearchAsync(query.q, query.category, query.minPrice, query.maxPrice, query.lat, query.lng, query.radiusKm, query.page, query.pageSize, query.sort);
            res.Headers["X-Total-Count"] = total.ToString();
            res.Headers["X-Page"] = query.page.ToString();
            res.Headers["X-Page-Size"] = query.pageSize.ToString();
            return Results.Ok(new ApiResponse<IEnumerable<Product>>(items, new { total, page = query.page, pageSize = query.pageSize }));
        })
        .WithSummary("Search products")
        .Produces<ApiResponse<IEnumerable<Product>>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .WithOpenApi();

        products.MapGet("/{id}", async (string id, HttpRequest req, ProductService svc) =>
        {
            var product = await svc.GetAsync(id);
            if (product is null) return Results.NotFound();
            var etag = ToEtag(product.UpdatedAt);
            if (req.Headers.IfNoneMatch.Contains(etag)) return Results.StatusCode(StatusCodes.Status304NotModified);
            return Results.Ok(product).WithEtag(etag);
        })
        .WithSummary("Get product by id")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status304NotModified)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithOpenApi();

        products.MapPost("/", async (CreateProductDto dto, ProductService svc, HttpContext ctx) =>
        {
            var product = await svc.CreateAsync(dto);
            var location = $"/api/products/{product.Id}";
            var etag = ToEtag(product.UpdatedAt);
            ctx.Response.Headers.ETag = etag;
            ctx.Response.Headers.Location = location;
            return Results.Created(location, product);
        })
        .WithSummary("Create product")
        .Produces<Product>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithOpenApi();

        products.MapPut("/{id}", async (string id, UpdateProductDto dto, HttpRequest req, ProductService svc) =>
        {
            var current = await svc.GetAsync(id);
            if (current is null) return Results.NotFound();
            var ifMatch = req.Headers.IfMatch.FirstOrDefault();
            var currentEtag = ToEtag(current.UpdatedAt);
            if (!string.IsNullOrEmpty(ifMatch) && ifMatch != currentEtag)
                return Results.Problem(statusCode: StatusCodes.Status412PreconditionFailed, title: "Precondition failed (ETag mismatch).");
            var ok = await svc.UpdateAsync(id, dto);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Update product")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status412PreconditionFailed)
        .WithOpenApi();

        products.MapDelete("/{id}", async (string id, ProductService svc) =>
        {
            var ok = await svc.DeleteAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Delete product")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithOpenApi();

        products.MapGet("/export", async (ProductService svc) =>
        {
            var stream = await svc.ExportCsvAsync();
            return Results.File(stream, "text/csv; charset=utf-8", "products.csv");
        })
        .WithSummary("Export products as CSV (UTF-8 BOM)")
        .Produces(StatusCodes.Status200OK)
        .WithOpenApi();

        // Photos upload endpoint (multipart/form-data)
        products.MapPost("/{id}/photos", async (string id, HttpRequest req, IPhotoService photos, ProductService svc) =>
        {
            var form = await req.ReadFormAsync();
            if (form.Files is null || form.Files.Count == 0)
                return Results.BadRequest(new { message = "No files were provided." });
            _ = await photos.SaveAsync(id, form.Files);
            // Return the updated product so ImageUrl reflects the new file name (productId + extension)
            var updated = await svc.GetAsync(id);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .Accepts<IFormFileCollection>("multipart/form-data")
        .WithSummary("Upload product image(s)")
        .Produces<Product>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithOpenApi();
   
        return app;
    }

    public static string ToEtag(DateTimeOffset updatedAt) => $"\"{updatedAt.ToUnixTimeMilliseconds()}\"";

    public record ApiResponse<T>(T Data, object? Meta = null, object? Links = null);

    public record Query(string? q, string? category, decimal? minPrice, decimal? maxPrice,
        double? lat, double? lng, double? radiusKm, int page = 1, int pageSize = 10, string? sort = null);
}

public static class ResultHeaderExtensions
{
    public static IResult WithEtag(this IResult result, string etag)
        => new HeaderResult(result, "ETag", etag);

    private sealed class HeaderResult : IResult
    {
        private readonly IResult _inner; private readonly string _name; private readonly string _value;
        public HeaderResult(IResult inner, string name, string value) { _inner = inner; _name = name; _value = value; }
        public Task ExecuteAsync(HttpContext context) { context.Response.Headers[_name] = _value; return _inner.ExecuteAsync(context); }
    }
}
