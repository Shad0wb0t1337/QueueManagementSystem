namespace QueueSystem.API.Models;

public class ServiceType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public string Prefix { get; set; } = string.Empty;
    public int PriorityLevel { get; set; } = 3;
    public bool IsFastTrack { get; set; }
    public int? MaxWaitMinutes { get; set; }

    public ICollection<CounterServiceType> CounterServiceTypes { get; set; } = new List<CounterServiceType>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
