using Dapper;
using PriceTracker.Api.Data;
using PriceTracker.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Connection string 'Default' not found");

builder.Services.AddNpgsqlDataSource(connectionString);
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddOpenApi();

DefaultTypeMap.MatchNamesWithUnderscores = true;

var app = builder.Build();

app.MapOpenApi();
app.MapProductEndpoints();

app.Run();