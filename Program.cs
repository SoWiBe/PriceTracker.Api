using Dapper;
using PriceTracker.Api.Data;
using PriceTracker.Api.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Connection string 'Default' not found");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddNpgsqlDataSource(connectionString);
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<PriceChangeRepository>();
builder.Services.AddOpenApi();

DefaultTypeMap.MatchNamesWithUnderscores = true;

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapProductEndpoints();

app.Run();