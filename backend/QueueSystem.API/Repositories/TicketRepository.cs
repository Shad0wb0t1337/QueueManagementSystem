using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Data;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Repositories.Interfaces;

namespace QueueSystem.API.Repositories;

public class TicketRepository : Repository<Ticket>, ITicketRepository
{
    public TicketRepository(AppDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<List<Ticket>> GetByServiceTypeAndDate(int serviceTypeId, DateOnly date, CancellationToken cancellationToken)
    {
        return await DbContext.Tickets
            .Where(ticket => ticket.ServiceTypeId == serviceTypeId && DateOnly.FromDateTime(ticket.CreatedAt) == date)
            .OrderBy(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ticket?> GetNextWaiting(int serviceTypeId, CancellationToken cancellationToken)
    {
        return await DbContext.Tickets
            .Where(ticket => ticket.ServiceTypeId == serviceTypeId && ticket.Status == TicketStatus.Waiting)
            .OrderBy(ticket => ticket.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TodayStatsResponseDto> GetTodayStats(CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        List<Ticket> tickets = await DbContext.Tickets
            .Where(ticket => DateOnly.FromDateTime(ticket.CreatedAt) == today)
            .ToListAsync(cancellationToken);

        return new TodayStatsResponseDto
        {
            Date = today,
            TotalIssued = tickets.Count,
            WaitingCount = tickets.Count(ticket => ticket.Status == TicketStatus.Waiting),
            CalledCount = tickets.Count(ticket => ticket.Status == TicketStatus.Called),
            ServingCount = tickets.Count(ticket => ticket.Status == TicketStatus.Serving),
            DoneCount = tickets.Count(ticket => ticket.Status == TicketStatus.Done),
            NoShowCount = tickets.Count(ticket => ticket.Status == TicketStatus.NoShow)
        };
    }
}
