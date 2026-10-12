using Dapper;
using Npgsql;
using PriceTracker.Api.Models;
using PriceTracker.Api.Models.Core;
using PriceTracker.Api.Models.Enums;

namespace PriceTracker.Api.Data;

public sealed class PriceChangeRepository(NpgsqlDataSource dataSource, TimeProvider timeProvider)
{
    public async Task<Result<decimal>> GetActualPrice(Guid productId, DateTime? actualDate, CancellationToken ct)
    {
        const string sql = "select * from price_changes where product_id = @ProductId";
        
        try
        {
            await using var con = await dataSource.OpenConnectionAsync(ct);
            var priceChanges = await con.QueryAsync<PriceChange>(sql, new { ProductId = productId });
            
            var bestPrice = 0M;
            var bestEffectiveFrom = DateTimeOffset.MinValue;
            actualDate ??= timeProvider.GetUtcNow().DateTime;
            
            foreach (var price in priceChanges)
            {
                if (price.EffectiveFrom > actualDate || price.EffectiveFrom <= bestEffectiveFrom) continue;

                bestPrice = price.Price;
                bestEffectiveFrom = price.EffectiveFrom;
            }

            return Result<decimal>.Success(bestPrice);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Result<decimal>.Failure(new Error(ErrorType.NotFound, $"Product {productId} not found"));
        }
    }
    
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
            EffectiveFrom = (effectiveFrom ?? timeProvider.GetUtcNow()).ToUniversalTime()
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

    public async Task<Result<IReadOnlyList<PriceChange>>> GetProductPriceHistory(
        Guid productId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        const string sql = "select * from  price_changes where effective_from >= @From && effective_from <= @To";

        try
        {
            await using var con = await dataSource.OpenConnectionAsync(ct);
            var priceChanges = await con
                .QueryAsync<PriceChange>(new CommandDefinition(sql, new { From = from, To = to}, cancellationToken: ct));

            return Result<IReadOnlyList<PriceChange>>.Success(priceChanges.AsList());
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Result<IReadOnlyList<PriceChange>>
                .Failure(new Error(ErrorType.NotFound, $"Product {productId} not found"));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Result<IReadOnlyList<PriceChange>>
                .Failure(new Error(ErrorType.Conflict,"Price for this date already exists"));
        }
    }
}