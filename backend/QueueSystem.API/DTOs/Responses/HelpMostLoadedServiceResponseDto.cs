namespace QueueSystem.API.DTOs.Responses;

public class HelpMostLoadedServiceResponseDto
{
    public int? EnabledServiceTypeId { get; set; }
    public string? EnabledServiceTypeName { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<CounterServiceProfileItemResponseDto> ServiceProfile { get; set; } = new();
}
