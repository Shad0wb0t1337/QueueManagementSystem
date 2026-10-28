namespace QueueSystem.API.Models;

public class CounterServiceType
{
    public int CounterId { get; set; }
    public int ServiceTypeId { get; set; }
    public bool IsEnabled { get; set; } = true;

    public Counter? Counter { get; set; }
    public ServiceType? ServiceType { get; set; }
}
