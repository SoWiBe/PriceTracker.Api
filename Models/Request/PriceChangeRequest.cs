namespace PriceTracker.Api.Models.Request;

public sealed record PriceChangeRequest(Guid ProductId, decimal Price, DateTimeOffset? EffectiveFrom);