namespace PriceTracker.Api.Models.Core;

public abstract class BaseModel
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
}