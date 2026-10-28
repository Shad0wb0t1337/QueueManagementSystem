namespace QueueSystem.API.DTOs.Responses;

public class TodayStatsResponseDto
{
    public DateOnly Date { get; set; }
    public int TotalIssued { get; set; }
    public int WaitingCount { get; set; }
    public int CalledCount { get; set; }
    public int ServingCount { get; set; }
    public int DoneCount { get; set; }
    public int NoShowCount { get; set; }
}
