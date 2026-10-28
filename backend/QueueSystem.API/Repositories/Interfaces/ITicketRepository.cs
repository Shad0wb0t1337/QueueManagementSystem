using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;

namespace QueueSystem.API.Repositories.Interfaces;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<List<Ticket>> GetByServiceTypeAndDate(int serviceTypeId, DateOnly date, CancellationToken cancellationToken);
    Task<Ticket?> GetNextWaiting(int serviceTypeId, CancellationToken cancellationToken);
    Task<TodayStatsResponseDto> GetTodayStats(CancellationToken cancellationToken);
}
