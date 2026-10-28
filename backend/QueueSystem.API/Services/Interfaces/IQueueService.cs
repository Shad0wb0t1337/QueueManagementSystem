using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;

namespace QueueSystem.API.Services.Interfaces;

public interface IQueueService
{
    Task<TicketCalledResponseDto> CallNextAsync(CallNextRequestDto request, int appUserId, CancellationToken cancellationToken);
    Task<QueueTicketResponseDto> MarkServingAsync(int ticketId, int appUserId, CancellationToken cancellationToken);
    Task<QueueTicketResponseDto> MarkDoneAsync(int ticketId, int appUserId, CancellationToken cancellationToken);
    Task<QueueTicketResponseDto> MarkNoShowAsync(int ticketId, int appUserId, CancellationToken cancellationToken);
    Task<List<RecentTicketHistoryItemResponseDto>> GetRecentHistoryAsync(int counterId, int appUserId, CancellationToken cancellationToken);
    Task<RecallTicketResponseDto> RecallTicketAsync(int ticketId, int appUserId, CancellationToken cancellationToken);
    Task<RedirectTicketResponseDto> RedirectTicketAsync(int ticketId, int appUserId, RedirectTicketRequestDto request, CancellationToken cancellationToken);
}
