using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;

namespace QueueSystem.API.Services.Interfaces;

public interface ITicketService
{
    Task<IssueTicketResponseDto> IssueTicketAsync(IssueTicketRequestDto request, CancellationToken cancellationToken);
    Task<List<QueueTicketResponseDto>> GetQueueAsync(int serviceTypeId, CancellationToken cancellationToken);
    Task<List<QueueTicketResponseDto>> GetCounterQueueAsync(int counterId, CancellationToken cancellationToken);
    Task<List<ActiveTicketResponseDto>> GetActiveTicketsAsync(CancellationToken cancellationToken);
    Task<TodayStatsResponseDto> GetTodayStatsAsync(CancellationToken cancellationToken);
}
