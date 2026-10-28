namespace QueueSystem.API.DTOs.Responses;

public class SupportedServiceTypeResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
