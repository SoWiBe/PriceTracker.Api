using Dapper;
using Npgsql;
using PriceTracker.Api.Models;
using PriceTracker.Api.Models.Core;
using PriceTracker.Api.Models.Enums;

namespace PriceTracker.Api.Data;

public sealed class PriceChangeRepository(NpgsqlDataSource dataSource)
{
    public async Task<Result<Guid>> ChangePrice(
        Guid productId, decimal price, 
        DateTimeOffset? effectiveFrom, CancellationToken ct)
    {
        const string sql = """
            insert into price_changes (id, product_id, effective_from, price)
            values (@Id, @ProductId, @EffectiveFrom, @Price) 
            """;

        var priceChange = new PriceChange
        {
            Price = price,
            ProductId = productId,
            EffectiveFrom = (effectiveFrom ?? DateTimeOffset.UtcNow).ToUniversalTime()
        };

        try
        {
            await using var con = await dataSource.OpenConnectionAsync(ct);
            await con.ExecuteAsync(new CommandDefinition(sql, priceChange, cancellationToken: ct));

            return Result<Guid>.Success(priceChange.Id);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Result<Guid>.Failure(new Error(ErrorType.NotFound, $"Product {productId} not found"));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Result<Guid>.Failure(new Error(ErrorType.Conflict,"Price for this date already exists"));
        }
    }
}