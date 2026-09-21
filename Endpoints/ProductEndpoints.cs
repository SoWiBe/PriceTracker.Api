using PriceTracker.Api.Data;
using PriceTracker.Api.Models.Request;

namespace PriceTracker.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products");
        group.MapGet("/{id:guid}", async (Guid id, ProductRepository repo, CancellationToken ct) 
            => await repo.GetByIdAsync(id, ct) is { } product 
                ? Results.Ok((object?)product)
                : Results.NotFound());
        
        group.MapGet("", async (ProductRepository repo, CancellationToken ct)
            => await repo.GetAllAsync(ct) is { } arr ? Results.Ok(arr) : Results.Empty);

        group.MapPost("", async (CreateProductRequest request, ProductRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("Name is required");

            var id = await repo.CreateAsync(request.Name, ct);
            return Results.Created($"/products/{id}", new { id });
        });
    }
}