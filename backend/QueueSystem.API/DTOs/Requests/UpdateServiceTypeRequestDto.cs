namespace QueueSystem.API.DTOs.Requests;

public class UpdateServiceTypeRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public int PriorityLevel { get; set; } = 3;
    public bool IsFastTrack { get; set; }
    public int? MaxWaitMinutes { get; set; }
}
