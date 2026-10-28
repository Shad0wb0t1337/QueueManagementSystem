namespace QueueSystem.API.DTOs.Responses;

public class TicketSelectionExplanationResponseDto
{
    public string SelectionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? PriorityLevel { get; set; }
    public int? WaitingMinutes { get; set; }
    public double? FastTrackBonus { get; set; }
    public double? SlaBoost { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public double? FinalScore { get; set; }
}
