using PriceTracker.Api.Data;
using PriceTracker.Api.Models.Request;

namespace PriceTracker.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products");
        
        // получение продукта
        group.MapGet("/{id:guid}", async (Guid id, ProductRepository repo, CancellationToken ct) => 
            await repo.GetByIdAsync(id, ct) is { } product 
                ? Results.Ok((object?)product)
                : Results.NotFound());
        
        // получение всех продуктов
        group.MapGet("", async (ProductRepository repo, CancellationToken ct) => 
            Results.Ok(await repo.GetAllAsync(ct)));

        // создание продукта
        group.MapPost("", async (CreateProductRequest request, ProductRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("Name is required");

            var id = await repo.CreateAsync(request.Name, ct);
            return Results.Created($"/products/{id}", new { id });
        });

        group.MapPost("/{id:guid}/prices",
            async (PriceChangeRequest request, PriceChangeRepository repo, CancellationToken ct) => 
                await repo.ChangePriceAsync(request.ProductId, request.Price, request.EffectiveFrom, ct));

        group.MapGet("/{id:guid}/prices/actual/",
            async ([AsParameters] GetActualPriceRequest request, PriceChangeRepository repo, CancellationToken ct) =>
            {
                var actualPriceResult = await repo.GetActualPriceAsync(request.ProductId, request.CurrentDate, ct);
                return actualPriceResult.IsSuccess 
                    ? Results.Ok(actualPriceResult.Value)
                    : Results.NotFound(actualPriceResult.ErrorInfo);
            });

        group.MapGet("/{id:guid}/prices",
            async ([AsParameters] GetProductPriceChangeHistory request, PriceChangeRepository repo, CancellationToken ct) => 
                Results.Ok((object?)await repo.GetProductPriceHistoryAsync(request.Id, request.From, request.To, ct)));
    }
}