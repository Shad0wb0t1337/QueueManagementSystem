namespace QueueSystem.API.DTOs.Responses;

public class OperatorResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CounterId { get; set; }
    public string CounterName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
