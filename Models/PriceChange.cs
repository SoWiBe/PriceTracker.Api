using PriceTracker.Api.Models.Core;

namespace PriceTracker.Api.Models;

public class PriceChange : BaseModel
{
    public required Guid ProductId { get; init; }
    public required DateTimeOffset EffectiveFrom { get; init; }
    public required decimal Price { get; init; }
}