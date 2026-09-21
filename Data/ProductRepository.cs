using Dapper;
using Npgsql;
using PriceTracker.Api.Models;

namespace PriceTracker.Api.Data;

public sealed class ProductRepository(NpgsqlDataSource dataSource)
{
    public async Task<Product?> GetByIdAsync(long id, CancellationToken ct)
    {
        const string sql = "SELECT id, name FROM products WHERE id = @Id";

        await using var con = await dataSource.OpenConnectionAsync(ct);
        return await con.QuerySingleOrDefaultAsync<Product>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}