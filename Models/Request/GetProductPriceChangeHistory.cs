using Microsoft.AspNetCore.Mvc;

namespace PriceTracker.Api.Models.Request;

public sealed record GetProductPriceChangeHistory(Guid Id, DateTime? From, DateTime? To);