using PriceTracker.Api.Data;

using Dapper;
using PriceTracker.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Default") ?? "");
builder.Services.AddScoped<ProductRepository>();
// builder.Services.AddOpenApi();

DefaultTypeMap.MatchNamesWithUnderscores = true;

var app = builder.Build();

// app.MapOpenApi();
app.MapProductEndpoints();

app.Run();
