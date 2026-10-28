namespace QueueSystem.API.DTOs.Responses;

public class CounterServiceProfileItemResponseDto
{
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int WaitingCount { get; set; }
    public int PriorityLevel { get; set; }
    public bool IsFastTrack { get; set; }
    public int? MaxWaitMinutes { get; set; }
}
