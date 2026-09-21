using PriceTracker.Api.Models.Core;

namespace PriceTracker.Api.Models;

public class Product : BaseModel
{
    public required string Name { get; init; }
}