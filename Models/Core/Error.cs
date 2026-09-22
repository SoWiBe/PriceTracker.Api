using PriceTracker.Api.Models.Enums;

namespace PriceTracker.Api.Models.Core;

public sealed record Error(ErrorType Type, string Message);