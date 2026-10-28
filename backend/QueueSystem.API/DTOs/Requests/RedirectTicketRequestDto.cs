namespace QueueSystem.API.DTOs.Requests;

public class RedirectTicketRequestDto
{
    public int TargetServiceTypeId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
