using AdsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdsApi.Endpoints;

public static class AdsEndpoints
{
    public static IEndpointRouteBuilder MapAdsEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithOpenApi();
        var ads = api.MapGroup("/ads").WithTags("Ads");

        ads.MapGet("/", async ([AsParameters] Query query, AdService svc, HttpResponse res) =>
        {
            var (items, total) = await svc.SearchAsync(query.q, query.category, query.minPrice, query.maxPrice, query.lat, query.lng, query.radiusKm, query.page, query.pageSize, query.sort);
            res.Headers["X-Total-Count"] = total.ToString();
            res.Headers["X-Page"] = query.page.ToString();
            res.Headers["X-Page-Size"] = query.pageSize.ToString();
            return Results.Ok(new ApiResponse<IEnumerable<Ad>>(items, new { total, page = query.page, pageSize = query.pageSize }));
        })
        .WithSummary("Search ads")
        .Produces<ApiResponse<IEnumerable<Ad>>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .WithOpenApi();

        ads.MapGet("/{id}", async (string id, HttpRequest req, AdService svc) =>
        {
            var ad = await svc.GetAsync(id);
            if (ad is null) return Results.NotFound();
            var etag = ToEtag(ad.UpdatedAt);
            if (req.Headers.IfNoneMatch.Contains(etag)) return Results.StatusCode(StatusCodes.Status304NotModified);
            return Results.Ok(ad).WithEtag(etag);
        })
        .WithSummary("Get ad by id")
        .Produces<Ad>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status304NotModified)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithOpenApi();

        ads.MapPost("/", async (CreateAdDto dto, AdService svc, HttpContext ctx) =>
        {
            var ad = await svc.CreateAsync(dto);
            var location = $"/api/ads/{ad.Id}";
            var etag = ToEtag(ad.UpdatedAt);
            ctx.Response.Headers.ETag = etag;
            ctx.Response.Headers.Location = location;
            return Results.Created(location, ad);
        })
        .WithSummary("Create ad")
        .Produces<Ad>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithOpenApi();

        ads.MapPut("/{id}", async (string id, UpdateAdDto dto, HttpRequest req, AdService svc) =>
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
        .WithSummary("Update ad")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status412PreconditionFailed)
        .WithOpenApi();

        ads.MapDelete("/{id}", async (string id, AdService svc) =>
        {
            var ok = await svc.DeleteAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Delete ad")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithOpenApi();

        ads.MapGet("/{id}/comments", async (string id, ICommentService comments) =>
        {
            var list = await comments.ListAsync(id);
            return Results.Ok(list);
        })
        .WithSummary("List comments for an ad")
        .Produces<IEnumerable<Comment>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithOpenApi();

        ads.MapPost("/{id}/comments", async (string id, CreateCommentDto dto, ICommentService comments) =>
        {
            var created = await comments.AddAsync(id, dto);
            return Results.Ok(created);
        })
        .WithSummary("Add a comment to an ad")
        .Produces<Comment>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithOpenApi();

        ads.MapDelete("/{id}/comments/{commentId}", async (string id, string commentId, ICommentService comments) =>
        {
            var ok = await comments.DeleteAsync(id, commentId);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Delete a specific comment from an ad")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithOpenApi();

        return app;
    }

    public static string ToEtag(DateTimeOffset updatedAt) => $"\"{updatedAt.ToUnixTimeMilliseconds()}\"";

    public record ApiResponse<T>(T Data, object? Meta = null, object? Links = null);

    public record Query(string? q, string? category, decimal? minPrice, decimal? maxPrice,
        double? lat, double? lng, double? radiusKm, int page = 1, int pageSize = 20, string? sort = null);
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
