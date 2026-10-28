namespace QueueSystem.API.DTOs.Requests;

public class UpdateOperatorRequestDto
{
    public string Name { get; set; } = string.Empty;
    public int CounterId { get; set; }
    public bool IsActive { get; set; }
}
