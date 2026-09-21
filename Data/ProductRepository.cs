using Dapper;
using Npgsql;

using PriceTracker.Api.Models;

namespace PriceTracker.Api.Data;

public sealed class ProductRepository(NpgsqlDataSource dataSource)
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        const string sql = "SELECT id, name FROM products WHERE id = @Id";

        await using var con = await dataSource.OpenConnectionAsync(ct);
        return await con.QuerySingleOrDefaultAsync<Product>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<Guid> CreateAsync(string name, CancellationToken ct)
    {
        const string sql = "insert into products (id, name) values (@Id, @Name)";

        var product = new Product { Name = name };
        
        await using var con = await dataSource.OpenConnectionAsync(ct);
        await con.ExecuteAsync(new CommandDefinition(sql, product, cancellationToken: ct));
        
        return product.Id;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct)
    {
        const string sql = "select id, name from products";
        await using var con = await dataSource.OpenConnectionAsync(ct);
        return await con.QueryAsync<Product>(sql);
    }
}