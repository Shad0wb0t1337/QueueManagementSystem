namespace QueueSystem.API.DTOs.Responses;

public class ServiceTypeResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public int PriorityLevel { get; set; }
    public bool IsFastTrack { get; set; }
    public int? MaxWaitMinutes { get; set; }
    public int CurrentQueueLength { get; set; }
    public int EstimatedWaitMinutes { get; set; }
    public bool HasActiveCounter { get; set; }
}
