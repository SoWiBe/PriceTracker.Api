namespace PriceTracker.Api.Models.Request;

/*
 * TODO: probably need to use DateTimeOffset correctly filtering
 * But need to change repository also
 */
public sealed record GetProductPriceChangeHistory(DateTime? From, DateTime? To);