using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Common;
using QueueSystem.API.Constants;
using QueueSystem.API.Data;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Hubs;
using QueueSystem.API.Models;
using QueueSystem.API.Repositories.Interfaces;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<QueueHub> _hubContext;

    public TicketService(ITicketRepository ticketRepository, AppDbContext dbContext, IHubContext<QueueHub> hubContext)
    {
        _ticketRepository = ticketRepository;
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task<IssueTicketResponseDto> IssueTicketAsync(IssueTicketRequestDto request, CancellationToken cancellationToken)
    {
        ServiceType serviceType = await _dbContext.ServiceTypes
            .FirstOrDefaultAsync(item => item.Id == request.ServiceTypeId, cancellationToken)
            ?? throw new NotFoundException($"Service type {request.ServiceTypeId} was not found.");

        if (!serviceType.IsActive)
        {
            throw new BadRequestException("Cannot issue ticket for disabled service type.");
        }

        int activeCounterCount = await _dbContext.CounterServiceTypes
            .Where(item => item.ServiceTypeId == request.ServiceTypeId
                && item.IsEnabled
                && item.Counter != null
                && item.Counter.Status == CounterStatus.Active)
            .Select(item => item.CounterId)
            .Distinct()
            .CountAsync(cancellationToken);

        if (activeCounterCount == 0)
        {
            throw new BadRequestException("Cannot issue ticket because this service type has no active enabled counters.");
        }

        DateTime now = DateTime.UtcNow;
        DateOnly today = DateOnly.FromDateTime(now);
        List<Ticket> todayTickets = await _ticketRepository.GetByServiceTypeAndDate(request.ServiceTypeId, today, cancellationToken);

        int lastNumber = 0;
        if (todayTickets.Count > 0)
        {
            Ticket latestTicket = todayTickets
                .Where(ticket => ticket.TicketNumber.StartsWith(serviceType.Prefix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(ticket => ticket.Id)
                .First();

            string numericPart = latestTicket.TicketNumber[serviceType.Prefix.Length..];
            if (!int.TryParse(numericPart, out lastNumber))
            {
                throw new BadRequestException("Stored ticket number has invalid format.");
            }
        }

        int nextNumber = lastNumber + 1;
        int waitingCount = await _dbContext.Tickets
            .Where(ticket => ticket.ServiceTypeId == request.ServiceTypeId && ticket.Status == TicketStatus.Waiting)
            .CountAsync(cancellationToken);

        Ticket ticket = new Ticket
        {
            ServiceTypeId = request.ServiceTypeId,
            TicketNumber = $"{serviceType.Prefix}{nextNumber:D3}",
            Status = TicketStatus.Waiting,
            CreatedAt = now,
            QueueEnteredAt = now
        };

        await _ticketRepository.Add(ticket, cancellationToken);
        await _ticketRepository.SaveChanges(cancellationToken);

        _dbContext.TicketEvents.Add(new TicketEvent
        {
            TicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            EventType = TicketEventType.Created,
            ServiceTypeId = ticket.ServiceTypeId,
            PreviousStatus = null,
            NewStatus = TicketStatus.Waiting,
            CreatedAt = now,
            Details = "Ticket issued from kiosk."
        });
        await _ticketRepository.SaveChanges(cancellationToken);

        int queuePosition = waitingCount + 1;
        int estimatedWaitMinutes = (int)Math.Ceiling(queuePosition * serviceType.EstimatedDurationMinutes / (double)activeCounterCount);

        await _hubContext.Clients.Group(SignalRGroups.Service(request.ServiceTypeId))
            .SendAsync(SignalREvents.TicketIssued, new
            {
                ticketNumber = ticket.TicketNumber,
                serviceTypeName = serviceType.Name,
                queuePosition,
                estimatedWaitMinutes
            }, cancellationToken);

        await _hubContext.Clients.Group(SignalRGroups.Service(request.ServiceTypeId))
            .SendAsync(SignalREvents.QueueUpdated, new { serviceTypeId = request.ServiceTypeId }, cancellationToken);

        return new IssueTicketResponseDto
        {
            TicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            ServiceTypeId = ticket.ServiceTypeId,
            ServiceTypeName = serviceType.Name,
            QueuePosition = queuePosition,
            EstimatedWaitMinutes = estimatedWaitMinutes,
            Status = ticket.Status.ToString()
        };
    }

    public async Task<List<QueueTicketResponseDto>> GetQueueAsync(int serviceTypeId, CancellationToken cancellationToken)
    {
        bool serviceTypeExists = await _dbContext.ServiceTypes.AnyAsync(item => item.Id == serviceTypeId, cancellationToken);
        if (!serviceTypeExists)
        {
            throw new NotFoundException($"Service type {serviceTypeId} was not found.");
        }

        List<Ticket> tickets = await _dbContext.Tickets
            .Where(ticket => ticket.ServiceTypeId == serviceTypeId && ticket.Status == TicketStatus.Waiting)
            .Include(ticket => ticket.ServiceType)
            .OrderBy(ticket => ticket.QueueEnteredAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return tickets
            .Select(ticket => new QueueTicketResponseDto
            {
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                ServiceTypeId = ticket.ServiceTypeId,
                CounterId = ticket.CounterId,
                Status = ticket.Status.ToString(),
                CreatedAt = ticket.CreatedAt,
                QueueEnteredAt = ticket.QueueEnteredAt,
                CalledAt = ticket.CalledAt,
                ServedAt = ticket.ServedAt,
                ServiceTypeName = ticket.ServiceType?.Name ?? string.Empty,
                IsFastTrack = ticket.ServiceType?.IsFastTrack ?? false,
                MaxWaitMinutes = ticket.ServiceType?.MaxWaitMinutes
            })
            .ToList();
    }

    public async Task<List<QueueTicketResponseDto>> GetCounterQueueAsync(int counterId, CancellationToken cancellationToken)
    {
        Counter counter = await _dbContext.Counters
            .Include(item => item.CounterServiceTypes)
                .ThenInclude(item => item.ServiceType)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == counterId, cancellationToken)
            ?? throw new NotFoundException($"Counter {counterId} was not found.");

        List<ServiceType> supportedServiceTypes = counter.CounterServiceTypes
            .Where(item => item.IsEnabled && item.ServiceType != null)
            .Select(item => item.ServiceType!)
            .ToList();

        List<int> supportedServiceTypeIds = supportedServiceTypes
            .Select(serviceType => serviceType.Id)
            .ToList();

        Dictionary<int, ServiceType> serviceTypesById = supportedServiceTypes.ToDictionary(serviceType => serviceType.Id);
        DateTime now = DateTime.UtcNow;

        List<Ticket> tickets = await _dbContext.Tickets
            .Where(ticket => supportedServiceTypeIds.Contains(ticket.ServiceTypeId) && ticket.Status == TicketStatus.Waiting)
            .Include(ticket => ticket.ServiceType)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return tickets
            .OrderByDescending(ticket => QueueScoringHelper.CalculateScore(ticket, serviceTypesById[ticket.ServiceTypeId], now))
            .ThenBy(ticket => ticket.QueueEnteredAt)
            .Select(ticket => new QueueTicketResponseDto
            {
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                ServiceTypeId = ticket.ServiceTypeId,
                ServiceTypeName = ticket.ServiceType?.Name ?? string.Empty,
                CounterId = ticket.CounterId,
                Status = ticket.Status.ToString(),
                CreatedAt = ticket.CreatedAt,
                QueueEnteredAt = ticket.QueueEnteredAt,
                CalledAt = ticket.CalledAt,
                ServedAt = ticket.ServedAt,
                IsFastTrack = ticket.ServiceType?.IsFastTrack ?? false,
                MaxWaitMinutes = ticket.ServiceType?.MaxWaitMinutes
            })
            .ToList();
    }

    public async Task<List<ActiveTicketResponseDto>> GetActiveTicketsAsync(CancellationToken cancellationToken)
    {
        List<Ticket> tickets = await _dbContext.Tickets
            .Where(ticket => ticket.Status == TicketStatus.Called || ticket.Status == TicketStatus.Serving)
            .Include(ticket => ticket.Counter)
            .Include(ticket => ticket.ServiceType)
            .OrderByDescending(ticket => ticket.CalledAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return tickets
            .Select(ticket => new ActiveTicketResponseDto
            {
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                CounterId = ticket.CounterId,
                CounterName = ticket.Counter?.Name ?? string.Empty,
                ServiceTypeId = ticket.ServiceTypeId,
                ServiceTypeName = ticket.ServiceType?.Name ?? string.Empty,
                Status = ticket.Status.ToString()
            })
            .ToList();
    }

    public async Task<TodayStatsResponseDto> GetTodayStatsAsync(CancellationToken cancellationToken)
    {
        return await _ticketRepository.GetTodayStats(cancellationToken);
    }
}
