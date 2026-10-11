namespace PriceTracker.Api.Models.Request;

public sealed record GetActualPriceRequest(Guid ProductId, DateTime? CurrentDate);