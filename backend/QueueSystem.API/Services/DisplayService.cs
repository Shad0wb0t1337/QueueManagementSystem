using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Common;
using QueueSystem.API.Data;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Services;

public class DisplayService : IDisplayService
{
    private const int RecentHistoryLimit = 15;

    private readonly AppDbContext _dbContext;

    public DisplayService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DisplayOverviewResponseDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        List<Ticket> activeTickets = await _dbContext.Tickets
            .Where(ticket => ticket.Status == TicketStatus.Called || ticket.Status == TicketStatus.Serving)
            .Include(ticket => ticket.Counter)
            .Include(ticket => ticket.ServiceType)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<Ticket> recentHistory = await _dbContext.Tickets
            .Where(ticket => ticket.Status == TicketStatus.Done || ticket.Status == TicketStatus.NoShow)
            .Include(ticket => ticket.Counter)
            .Include(ticket => ticket.ServiceType)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<DisplayTicketResponseDto> activeTicketResponses = activeTickets
            .OrderByDescending(GetTicketLastUpdatedAt)
            .Select(MapDisplayTicket)
            .ToList();

        List<DisplayTicketResponseDto> recentHistoryResponses = recentHistory
            .OrderByDescending(GetTicketLastUpdatedAt)
            .Take(RecentHistoryLimit)
            .Select(MapDisplayTicket)
            .ToList();

        DateTime lastUpdatedAt = activeTicketResponses
            .Concat(recentHistoryResponses)
            .Select(ticket => ticket.LastUpdatedAt)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Max();

        return new DisplayOverviewResponseDto
        {
            ActiveTickets = activeTicketResponses,
            RecentHistory = recentHistoryResponses,
            LastUpdatedAt = lastUpdatedAt
        };
    }

    public async Task<CounterDisplayGridResponseDto> GetCounterGridAsync(
        int? from,
        int? to,
        CancellationToken cancellationToken)
    {
        if (from.HasValue != to.HasValue)
        {
            throw new BadRequestException("Both from and to range values are required.");
        }

        if ((from.HasValue && from.Value <= 0)
            || (to.HasValue && to.Value <= 0)
            || (from.HasValue && to.HasValue && from.Value > to.Value))
        {
            throw new BadRequestException("Counter display range is invalid.");
        }

        IQueryable<Counter> query = _dbContext.Counters.AsNoTracking();

        if (from.HasValue && to.HasValue)
        {
            query = query.Where(counter => counter.DisplayOrder >= from.Value && counter.DisplayOrder <= to.Value);
        }

        List<Counter> counters = await query
            .OrderBy(counter => counter.DisplayOrder)
            .ThenBy(counter => counter.Id)
            .ToListAsync(cancellationToken);

        List<int> counterIds = counters
            .Select(counter => counter.Id)
            .ToList();

        List<Ticket> activeCounterTickets = await _dbContext.Tickets
            .Where(ticket => ticket.CounterId != null
                && counterIds.Contains(ticket.CounterId.Value)
                && (ticket.Status == TicketStatus.Called || ticket.Status == TicketStatus.Serving))
            .Include(ticket => ticket.ServiceType)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Dictionary<int, Ticket> currentTicketByCounterId = activeCounterTickets
            .GroupBy(ticket => ticket.CounterId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(GetTicketLastUpdatedAt)
                    .First());

        List<CounterDisplayTileResponseDto> counterTiles = counters
            .Select(counter =>
            {
                currentTicketByCounterId.TryGetValue(counter.Id, out Ticket? currentTicket);

                return new CounterDisplayTileResponseDto
                {
                    CounterId = counter.Id,
                    CounterName = counter.Name,
                    DisplayOrder = counter.DisplayOrder,
                    CounterStatus = counter.Status.ToString(),
                    CurrentTicketId = currentTicket?.Id,
                    CurrentTicketNumber = currentTicket?.TicketNumber,
                    CurrentTicketStatus = currentTicket?.Status.ToString(),
                    ServiceTypeId = currentTicket?.ServiceTypeId,
                    ServiceTypeName = currentTicket?.ServiceType?.Name,
                    LastUpdatedAt = currentTicket == null ? DateTime.UtcNow : GetTicketLastUpdatedAt(currentTicket)
                };
            })
            .ToList();

        DateTime lastUpdatedAt = counterTiles
            .Select(counter => counter.LastUpdatedAt)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Max();

        return new CounterDisplayGridResponseDto
        {
            Counters = counterTiles,
            LastUpdatedAt = lastUpdatedAt
        };
    }

    public async Task<CounterDisplayStateResponseDto> GetCounterDisplayStateAsync(
        int counterId,
        CancellationToken cancellationToken)
    {
        Counter counter = await _dbContext.Counters
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == counterId, cancellationToken)
            ?? throw new NotFoundException($"Counter {counterId} was not found.");

        Ticket? currentTicket = await _dbContext.Tickets
            .Where(ticket => ticket.CounterId == counterId
                && (ticket.Status == TicketStatus.Called || ticket.Status == TicketStatus.Serving))
            .Include(ticket => ticket.ServiceType)
            .AsNoTracking()
            .OrderByDescending(ticket => ticket.ServedAt ?? ticket.CalledAt ?? ticket.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new CounterDisplayStateResponseDto
        {
            CounterId = counter.Id,
            CounterName = counter.Name,
            CounterStatus = counter.Status.ToString(),
            CurrentTicketId = currentTicket?.Id,
            CurrentTicketNumber = currentTicket?.TicketNumber,
            CurrentTicketStatus = currentTicket?.Status.ToString(),
            ServiceTypeName = currentTicket?.ServiceType?.Name,
            LastUpdatedAt = currentTicket == null ? DateTime.UtcNow : GetTicketLastUpdatedAt(currentTicket)
        };
    }

    private static DisplayTicketResponseDto MapDisplayTicket(Ticket ticket)
    {
        return new DisplayTicketResponseDto
        {
            TicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            TicketStatus = ticket.Status.ToString(),
            CounterId = ticket.CounterId,
            CounterName = ticket.Counter?.Name ?? string.Empty,
            ServiceTypeId = ticket.ServiceTypeId,
            ServiceTypeName = ticket.ServiceType?.Name ?? string.Empty,
            CreatedAt = ticket.CreatedAt,
            CalledAt = ticket.CalledAt,
            ServingStartedAt = ticket.ServedAt,
            CompletedAt = ticket.Status == TicketStatus.Done || ticket.Status == TicketStatus.NoShow
                ? GetTicketLastUpdatedAt(ticket)
                : null,
            LastUpdatedAt = GetTicketLastUpdatedAt(ticket)
        };
    }

    private static DateTime GetTicketLastUpdatedAt(Ticket ticket)
    {
        return ticket.ServedAt ?? ticket.CalledAt ?? ticket.CreatedAt;
    }
}
