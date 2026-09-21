using PriceTracker.Api.Data;

namespace PriceTracker.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products");
        group.MapGet("/{id:long}", async (long id, ProductRepository repo, CancellationToken ct) 
            => await repo.GetByIdAsync(id, ct) is { } product 
                ? Results.Ok((object?)product)
                : Results.NotFound());
    }
}