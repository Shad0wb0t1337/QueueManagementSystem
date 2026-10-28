namespace QueueSystem.API.DTOs.Responses;

public class CounterDisplayGridResponseDto
{
    public List<CounterDisplayTileResponseDto> Counters { get; set; } = new List<CounterDisplayTileResponseDto>();
    public DateTime LastUpdatedAt { get; set; }
}
