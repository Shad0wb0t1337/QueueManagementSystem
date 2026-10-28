namespace QueueSystem.API.DTOs.Responses;

public class ServiceLoadSummaryResponseDto
{
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public int WaitingCount { get; set; }
    public int OldestWaitingMinutes { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public int ActiveEnabledCounterCount { get; set; }
    public int EstimatedWorkloadMinutes { get; set; }
    public int EstimatedWaitMinutes { get; set; }
    public int SlaBreachCount { get; set; }
    public double ServicePressure { get; set; }
    public string LoadStatus { get; set; } = string.Empty;
    public string? RecommendationMessage { get; set; }
    public bool IsCurrentlyAvailable { get; set; }
}
