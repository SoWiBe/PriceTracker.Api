using Dapper;
using Npgsql;

using PriceTracker.Api.Models;
using PriceTracker.Api.Models.Core;
using PriceTracker.Api.Models.Enums;

namespace PriceTracker.Api.Data;

public sealed class PriceChangeRepository(NpgsqlDataSource dataSource, TimeProvider timeProvider)
{
    public async Task<Result<decimal>> GetActualPriceAsync(Guid productId, DateTime? actualDate, CancellationToken ct)
    {
        const string sql = """
                           select price
                           from price_changes 
                           where product_id = @ProductId and effective_from <= @ActualDate
                           order by effective_from desc limit 1;
                           """;
        
        var date = (actualDate ?? timeProvider.GetUtcNow()).ToUniversalTime();
        
        await using var con = await dataSource.OpenConnectionAsync(ct);
        var actualPrice = await con.QuerySingleOrDefaultAsync<decimal?>(new CommandDefinition(
            sql, new { ProductId = productId, ActualDate = date }, cancellationToken: ct));
        
        return actualPrice is not null
            ? Result<decimal>.Success(actualPrice.Value)
            : Result<decimal>.Failure(new Error(ErrorType.NotFound, $"Actual price for {productId} was not found"));
    }
    
    public async Task<Result<Guid>> ChangePriceAsync(
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

    public async Task<Result<IReadOnlyList<PriceChange>>> GetProductPriceHistoryAsync(
        Guid productId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        const string sql = """
                           select * 
                           from price_changes
                           where product_id = @ProductId 
                             and (@From::timestamptz is null or effective_from >= @From) 
                             and (@To::timestamptz is null or effective_from <= @To)
                           order by effective_from;
                           """;
        
        await using var con = await dataSource.OpenConnectionAsync(ct);
        var priceChanges = await con
            .QueryAsync<PriceChange>(new CommandDefinition(sql, new { ProductId = productId, From = from, To = to }, 
                cancellationToken: ct));

        return Result<IReadOnlyList<PriceChange>>.Success(priceChanges.AsList());
    }
}