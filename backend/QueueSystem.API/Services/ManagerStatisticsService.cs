using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Common;
using QueueSystem.API.Data;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Services;

public class ManagerStatisticsService : IManagerStatisticsService
{
    private const int DefaultRecentEventsLimit = 50;
    private const int MaxRecentEventsLimit = 100;

    private readonly AppDbContext _dbContext;

    public ManagerStatisticsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ManagerStatisticsOverviewResponseDto> GetOverviewAsync(
        int appUserId,
        DateTime? from,
        DateTime? to,
        int? departmentId,
        CancellationToken cancellationToken)
    {
        (DateTime rangeFrom, DateTime rangeTo) = GetDateRange(from, to);
        List<int> allowedDepartmentIds = await GetAllowedDepartmentIdsAsync(appUserId, departmentId, cancellationToken);
        List<TicketEvent> events = await GetScopedEventsAsync(rangeFrom, rangeTo, allowedDepartmentIds, cancellationToken);
        int issuedCount = await CountScopedIssuedTicketsAsync(rangeFrom, rangeTo, allowedDepartmentIds, serviceTypeId: null, cancellationToken);

        return CreateOverview(rangeFrom, rangeTo, events, issuedCount);
    }

    public async Task<List<ManagerServiceStatisticsResponseDto>> GetServiceStatisticsAsync(
        int appUserId,
        DateTime? from,
        DateTime? to,
        int? departmentId,
        CancellationToken cancellationToken)
    {
        (DateTime rangeFrom, DateTime rangeTo) = GetDateRange(from, to);
        List<int> allowedDepartmentIds = await GetAllowedDepartmentIdsAsync(appUserId, departmentId, cancellationToken);
        if (allowedDepartmentIds.Count == 0)
        {
            return new List<ManagerServiceStatisticsResponseDto>();
        }

        List<TicketEvent> events = await GetScopedEventsAsync(rangeFrom, rangeTo, allowedDepartmentIds, cancellationToken);
        List<int> scopedServiceTypeIds = await _dbContext.CounterServiceTypes
            .Where(link => link.Counter != null && allowedDepartmentIds.Contains(link.Counter.DepartmentId))
            .Select(link => link.ServiceTypeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        List<ServiceType> serviceTypes = await _dbContext.ServiceTypes
            .Where(serviceType => scopedServiceTypeIds.Contains(serviceType.Id))
            .AsNoTracking()
            .OrderBy(serviceType => serviceType.Name)
            .ToListAsync(cancellationToken);

        List<ManagerServiceStatisticsResponseDto> result = new List<ManagerServiceStatisticsResponseDto>();
        foreach (ServiceType serviceType in serviceTypes)
        {
            List<TicketEvent> serviceEvents = events
                .Where(ticketEvent => ticketEvent.ServiceTypeId == serviceType.Id)
                .ToList();

            int issuedCount = await CountScopedIssuedTicketsAsync(
                rangeFrom,
                rangeTo,
                allowedDepartmentIds,
                serviceType.Id,
                cancellationToken);

            result.Add(new ManagerServiceStatisticsResponseDto
            {
                ServiceTypeId = serviceType.Id,
                ServiceTypeName = serviceType.Name,
                IssuedCount = issuedCount,
                CalledCount = CountEvents(serviceEvents, TicketEventType.Called),
                CompletedCount = CountEvents(serviceEvents, TicketEventType.Completed),
                NoShowCount = CountEvents(serviceEvents, TicketEventType.NoShow),
                RedirectedCount = CountEvents(serviceEvents, TicketEventType.Redirected),
                RecalledCount = CountEvents(serviceEvents, TicketEventType.Recalled),
                RecallPendingCount = CountEvents(serviceEvents, TicketEventType.RecallPending),
                AverageWaitMinutes = CalculateAverageWaitMinutes(serviceEvents),
                AverageServiceMinutes = CalculateAverageServiceMinutes(serviceEvents),
                SlaBreachCount = CountSlaBreaches(serviceEvents),
                SlaBreachPercent = CalculateSlaBreachPercent(serviceEvents)
            });
        }

        return result;
    }

    public async Task<List<ManagerCounterStatisticsResponseDto>> GetCounterStatisticsAsync(
        int appUserId,
        DateTime? from,
        DateTime? to,
        int? departmentId,
        CancellationToken cancellationToken)
    {
        (DateTime rangeFrom, DateTime rangeTo) = GetDateRange(from, to);
        List<int> allowedDepartmentIds = await GetAllowedDepartmentIdsAsync(appUserId, departmentId, cancellationToken);
        List<TicketEvent> events = await GetScopedEventsAsync(rangeFrom, rangeTo, allowedDepartmentIds, cancellationToken);
        List<Counter> counters = await _dbContext.Counters
            .Where(counter => allowedDepartmentIds.Contains(counter.DepartmentId))
            .AsNoTracking()
            .OrderBy(counter => counter.Name)
            .ToListAsync(cancellationToken);

        return counters.Select(counter =>
        {
            List<TicketEvent> counterEvents = events
                .Where(ticketEvent => ticketEvent.CounterId == counter.Id)
                .ToList();

            return new ManagerCounterStatisticsResponseDto
            {
                CounterId = counter.Id,
                CounterName = counter.Name,
                CalledCount = CountEvents(counterEvents, TicketEventType.Called),
                CompletedCount = CountEvents(counterEvents, TicketEventType.Completed),
                NoShowCount = CountEvents(counterEvents, TicketEventType.NoShow),
                RedirectedCount = CountEvents(counterEvents, TicketEventType.Redirected),
                RecalledCount = CountEvents(counterEvents, TicketEventType.Recalled),
                RecallPendingCount = CountEvents(counterEvents, TicketEventType.RecallPending),
                AverageServiceMinutes = CalculateAverageServiceMinutes(counterEvents)
            };
        }).ToList();
    }

    public async Task<List<ManagerOperatorStatisticsResponseDto>> GetOperatorStatisticsAsync(
        int appUserId,
        DateTime? from,
        DateTime? to,
        int? departmentId,
        CancellationToken cancellationToken)
    {
        (DateTime rangeFrom, DateTime rangeTo) = GetDateRange(from, to);
        List<int> allowedDepartmentIds = await GetAllowedDepartmentIdsAsync(appUserId, departmentId, cancellationToken);
        List<TicketEvent> events = await GetScopedEventsAsync(rangeFrom, rangeTo, allowedDepartmentIds, cancellationToken);
        List<Operator> operators = await _dbContext.Operators
            .Where(operatorProfile => operatorProfile.Counter != null
                && allowedDepartmentIds.Contains(operatorProfile.Counter.DepartmentId))
            .AsNoTracking()
            .OrderBy(operatorProfile => operatorProfile.Name)
            .ToListAsync(cancellationToken);

        return operators.Select(operatorProfile =>
        {
            List<TicketEvent> operatorEvents = events
                .Where(ticketEvent => ticketEvent.OperatorId == operatorProfile.Id)
                .ToList();

            return new ManagerOperatorStatisticsResponseDto
            {
                OperatorId = operatorProfile.Id,
                OperatorName = operatorProfile.Name,
                CalledCount = CountEvents(operatorEvents, TicketEventType.Called),
                CompletedCount = CountEvents(operatorEvents, TicketEventType.Completed),
                NoShowCount = CountEvents(operatorEvents, TicketEventType.NoShow),
                RedirectedCount = CountEvents(operatorEvents, TicketEventType.Redirected),
                RecalledCount = CountEvents(operatorEvents, TicketEventType.Recalled),
                RecallPendingCount = CountEvents(operatorEvents, TicketEventType.RecallPending),
                AverageServiceMinutes = CalculateAverageServiceMinutes(operatorEvents)
            };
        }).ToList();
    }

    public async Task<List<ManagerRedirectStatisticsResponseDto>> GetRedirectStatisticsAsync(
        int appUserId,
        DateTime? from,
        DateTime? to,
        int? departmentId,
        CancellationToken cancellationToken)
    {
        (DateTime rangeFrom, DateTime rangeTo) = GetDateRange(from, to);
        List<int> allowedDepartmentIds = await GetAllowedDepartmentIdsAsync(appUserId, departmentId, cancellationToken);
        if (allowedDepartmentIds.Count == 0)
        {
            return new List<ManagerRedirectStatisticsResponseDto>();
        }

        List<TicketRedirect> redirects = await _dbContext.TicketRedirects
            .Where(redirect => redirect.CreatedAt >= rangeFrom
                && redirect.CreatedAt < rangeTo
                && redirect.FromCounter != null
                && allowedDepartmentIds.Contains(redirect.FromCounter.DepartmentId))
            .Include(redirect => redirect.FromServiceType)
            .Include(redirect => redirect.ToServiceType)
            .Include(redirect => redirect.RedirectedByOperator)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return redirects
            .GroupBy(redirect => new
            {
                redirect.FromServiceTypeId,
                FromServiceTypeName = redirect.FromServiceType != null ? redirect.FromServiceType.Name : string.Empty,
                redirect.ToServiceTypeId,
                ToServiceTypeName = redirect.ToServiceType != null ? redirect.ToServiceType.Name : string.Empty,
                OperatorId = redirect.RedirectedByOperatorId,
                OperatorName = redirect.RedirectedByOperator != null ? redirect.RedirectedByOperator.Name : string.Empty
            })
            .Select(group => new ManagerRedirectStatisticsResponseDto
            {
                FromServiceTypeId = group.Key.FromServiceTypeId,
                FromServiceTypeName = group.Key.FromServiceTypeName,
                ToServiceTypeId = group.Key.ToServiceTypeId,
                ToServiceTypeName = group.Key.ToServiceTypeName,
                OperatorId = group.Key.OperatorId,
                OperatorName = group.Key.OperatorName,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ToList();
    }

    public async Task<List<ManagerRecallStatisticsResponseDto>> GetRecallStatisticsAsync(
        int appUserId,
        DateTime? from,
        DateTime? to,
        int? departmentId,
        CancellationToken cancellationToken)
    {
        (DateTime rangeFrom, DateTime rangeTo) = GetDateRange(from, to);
        List<int> allowedDepartmentIds = await GetAllowedDepartmentIdsAsync(appUserId, departmentId, cancellationToken);
        List<TicketEvent> events = await GetScopedEventsAsync(rangeFrom, rangeTo, allowedDepartmentIds, cancellationToken);

        return events
            .Where(ticketEvent => ticketEvent.EventType == TicketEventType.Recalled
                || ticketEvent.EventType == TicketEventType.RecallPending)
            .GroupBy(ticketEvent => new
            {
                ticketEvent.CounterId,
                CounterName = ticketEvent.Counter != null ? ticketEvent.Counter.Name : string.Empty,
                ticketEvent.OperatorId,
                OperatorName = ticketEvent.Operator != null ? ticketEvent.Operator.Name : string.Empty
            })
            .Select(group => new ManagerRecallStatisticsResponseDto
            {
                CounterId = group.Key.CounterId,
                CounterName = group.Key.CounterName,
                OperatorId = group.Key.OperatorId,
                OperatorName = group.Key.OperatorName,
                RecalledCount = group.Count(ticketEvent => ticketEvent.EventType == TicketEventType.Recalled),
                RecallPendingCount = group.Count(ticketEvent => ticketEvent.EventType == TicketEventType.RecallPending)
            })
            .OrderByDescending(item => item.RecalledCount + item.RecallPendingCount)
            .ToList();
    }

    public async Task<List<ManagerTicketEventResponseDto>> GetRecentTicketEventsAsync(
        int appUserId,
        DateTime? from,
        DateTime? to,
        int? departmentId,
        int? limit,
        CancellationToken cancellationToken)
    {
        (DateTime rangeFrom, DateTime rangeTo) = GetDateRange(from, to);
        List<int> allowedDepartmentIds = await GetAllowedDepartmentIdsAsync(appUserId, departmentId, cancellationToken);
        if (allowedDepartmentIds.Count == 0)
        {
            return new List<ManagerTicketEventResponseDto>();
        }

        int safeLimit = Math.Clamp(limit ?? DefaultRecentEventsLimit, 1, MaxRecentEventsLimit);

        List<TicketEvent> events = await _dbContext.TicketEvents
            .Where(ticketEvent => ticketEvent.CreatedAt >= rangeFrom
                && ticketEvent.CreatedAt < rangeTo
                && ticketEvent.Counter != null
                && allowedDepartmentIds.Contains(ticketEvent.Counter.DepartmentId))
            .Include(ticketEvent => ticketEvent.Counter)
            .Include(ticketEvent => ticketEvent.Operator)
            .Include(ticketEvent => ticketEvent.ServiceType)
            .Include(ticketEvent => ticketEvent.FromServiceType)
            .Include(ticketEvent => ticketEvent.ToServiceType)
            .AsNoTracking()
            .OrderByDescending(ticketEvent => ticketEvent.CreatedAt)
            .ThenByDescending(ticketEvent => ticketEvent.Id)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return events.Select(ticketEvent => new ManagerTicketEventResponseDto
        {
            EventId = ticketEvent.Id,
            TicketId = ticketEvent.TicketId,
            TicketNumber = ticketEvent.TicketNumber,
            EventType = ticketEvent.EventType.ToString(),
            EventLabel = GetEventLabel(ticketEvent.EventType),
            CreatedAt = ticketEvent.CreatedAt,
            CounterId = ticketEvent.CounterId,
            CounterName = ticketEvent.Counter?.Name ?? string.Empty,
            OperatorId = ticketEvent.OperatorId,
            OperatorName = ticketEvent.Operator?.Name ?? string.Empty,
            ServiceTypeId = ticketEvent.ServiceTypeId,
            ServiceTypeName = ticketEvent.ServiceType?.Name ?? string.Empty,
            FromStatus = ticketEvent.PreviousStatus?.ToString(),
            ToStatus = ticketEvent.NewStatus?.ToString(),
            FromServiceTypeName = ticketEvent.FromServiceType?.Name ?? string.Empty,
            ToServiceTypeName = ticketEvent.ToServiceType?.Name ?? string.Empty,
            Details = ticketEvent.Details
        }).ToList();
    }

    private async Task<List<int>> GetAllowedDepartmentIdsAsync(
        int appUserId,
        int? requestedDepartmentId,
        CancellationToken cancellationToken)
    {
        AppUser manager = await _dbContext.AppUsers
            .FirstOrDefaultAsync(user => user.Id == appUserId, cancellationToken)
            ?? throw new NotFoundException("Authenticated user was not found.");

        if (manager.Role != UserRole.Manager)
        {
            throw new ForbiddenException("Only managers can view manager statistics.");
        }

        List<int> assignedDepartmentIds = await _dbContext.ManagerDepartments
            .Where(link => link.AppUserId == appUserId
                && link.Department != null
                && link.Department.IsActive)
            .Select(link => link.DepartmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (requestedDepartmentId == null)
        {
            return assignedDepartmentIds;
        }

        if (!assignedDepartmentIds.Contains(requestedDepartmentId.Value))
        {
            throw new ForbiddenException("Manager is not assigned to the requested department.");
        }

        return new List<int> { requestedDepartmentId.Value };
    }

    private async Task<List<TicketEvent>> GetScopedEventsAsync(
        DateTime from,
        DateTime to,
        List<int> allowedDepartmentIds,
        CancellationToken cancellationToken)
    {
        if (allowedDepartmentIds.Count == 0)
        {
            return new List<TicketEvent>();
        }

        return await _dbContext.TicketEvents
            .Where(ticketEvent => ticketEvent.CreatedAt >= from
                && ticketEvent.CreatedAt < to
                && ticketEvent.Counter != null
                && allowedDepartmentIds.Contains(ticketEvent.Counter.DepartmentId))
            .Include(ticketEvent => ticketEvent.Ticket)
                .ThenInclude(ticket => ticket!.ServiceType)
            .Include(ticketEvent => ticketEvent.Counter)
            .Include(ticketEvent => ticketEvent.Operator)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private async Task<int> CountScopedIssuedTicketsAsync(
        DateTime from,
        DateTime to,
        List<int> allowedDepartmentIds,
        int? serviceTypeId,
        CancellationToken cancellationToken)
    {
        if (allowedDepartmentIds.Count == 0)
        {
            return 0;
        }

        List<int> scopedServiceTypeIds = await _dbContext.CounterServiceTypes
            .Where(link => link.Counter != null && allowedDepartmentIds.Contains(link.Counter.DepartmentId))
            .Select(link => link.ServiceTypeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (serviceTypeId.HasValue)
        {
            scopedServiceTypeIds = scopedServiceTypeIds
                .Where(id => id == serviceTypeId.Value)
                .ToList();
        }

        if (scopedServiceTypeIds.Count == 0)
        {
            return 0;
        }

        return await _dbContext.TicketEvents
            .Where(ticketEvent => ticketEvent.EventType == TicketEventType.Created
                && ticketEvent.CreatedAt >= from
                && ticketEvent.CreatedAt < to
                && scopedServiceTypeIds.Contains(ticketEvent.ServiceTypeId))
            .Select(ticketEvent => ticketEvent.TicketId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private static ManagerStatisticsOverviewResponseDto CreateOverview(
        DateTime rangeFrom,
        DateTime rangeTo,
        List<TicketEvent> events,
        int issuedCount)
    {
        return new ManagerStatisticsOverviewResponseDto
        {
            From = rangeFrom,
            To = rangeTo,
            IssuedCount = issuedCount,
            CalledCount = CountEvents(events, TicketEventType.Called),
            CompletedCount = CountEvents(events, TicketEventType.Completed),
            NoShowCount = CountEvents(events, TicketEventType.NoShow),
            RedirectedCount = CountEvents(events, TicketEventType.Redirected),
            RecalledCount = CountEvents(events, TicketEventType.Recalled),
            RecallPendingCount = CountEvents(events, TicketEventType.RecallPending),
            AverageWaitMinutes = CalculateAverageWaitMinutes(events),
            AverageServiceMinutes = CalculateAverageServiceMinutes(events),
            SlaBreachCount = CountSlaBreaches(events),
            SlaBreachPercent = CalculateSlaBreachPercent(events)
        };
    }

    private static (DateTime From, DateTime To) GetDateRange(DateTime? from, DateTime? to)
    {
        DateTime today = DateTime.UtcNow.Date;
        DateTime rangeFrom = from ?? today;
        DateTime rangeTo = to ?? rangeFrom.Date.AddDays(1);

        if (rangeTo <= rangeFrom)
        {
            rangeTo = rangeFrom.AddDays(1);
        }

        return (rangeFrom, rangeTo);
    }

    private static string GetEventLabel(TicketEventType eventType)
    {
        return eventType switch
        {
            TicketEventType.Created => "Bilietas išduotas",
            TicketEventType.Called => "Bilietas iškviestas",
            TicketEventType.ServingStarted => "Aptarnavimas pradėtas",
            TicketEventType.Completed => "Aptarnavimas užbaigtas",
            TicketEventType.NoShow => "Pažymėta kaip neatvykęs",
            TicketEventType.RecallPending => "Laukia pakartotinio kvietimo",
            TicketEventType.Recalled => "Pakartotinai iškviestas",
            TicketEventType.Redirected => "Nukreiptas į kitą paslaugą",
            _ => eventType.ToString()
        };
    }

    private static int CountEvents(List<TicketEvent> events, TicketEventType eventType)
    {
        return events.Count(ticketEvent => ticketEvent.EventType == eventType);
    }

    private static double CalculateAverageWaitMinutes(List<TicketEvent> events)
    {
        List<double> waitMinutes = events
            .Where(ticketEvent => (ticketEvent.EventType == TicketEventType.Called
                    || ticketEvent.EventType == TicketEventType.Recalled)
                && ticketEvent.Ticket?.CalledAt != null)
            .Select(ticketEvent => (ticketEvent.Ticket!.CalledAt!.Value - ticketEvent.Ticket.QueueEnteredAt).TotalMinutes)
            .Where(minutes => minutes >= 0)
            .ToList();

        return AverageOrZero(waitMinutes);
    }

    private static double CalculateAverageServiceMinutes(List<TicketEvent> events)
    {
        List<double> serviceMinutes = events
            .Where(ticketEvent => ticketEvent.EventType == TicketEventType.Completed
                && ticketEvent.Ticket != null)
            .Select(ticketEvent =>
            {
                DateTime? serviceStart = ticketEvent.Ticket!.ServedAt ?? ticketEvent.Ticket.CalledAt;
                return serviceStart == null ? 0 : (ticketEvent.CreatedAt - serviceStart.Value).TotalMinutes;
            })
            .Where(minutes => minutes >= 0)
            .ToList();

        return AverageOrZero(serviceMinutes);
    }

    private static int CountSlaBreaches(List<TicketEvent> events)
    {
        return GetSlaCheckedEvents(events)
            .Count(ticketEvent =>
            {
                int? maxWaitMinutes = ticketEvent.Ticket?.ServiceType?.MaxWaitMinutes;
                if (maxWaitMinutes == null || ticketEvent.Ticket?.CalledAt == null)
                {
                    return false;
                }

                double waitMinutes = (ticketEvent.Ticket.CalledAt.Value - ticketEvent.Ticket.QueueEnteredAt).TotalMinutes;
                return waitMinutes > maxWaitMinutes.Value;
            });
    }

    private static double CalculateSlaBreachPercent(List<TicketEvent> events)
    {
        List<TicketEvent> checkedEvents = GetSlaCheckedEvents(events);
        if (checkedEvents.Count == 0)
        {
            return 0;
        }

        return Math.Round(CountSlaBreaches(events) * 100.0 / checkedEvents.Count, 2);
    }

    private static List<TicketEvent> GetSlaCheckedEvents(List<TicketEvent> events)
    {
        return events
            .Where(ticketEvent => ticketEvent.EventType == TicketEventType.Called
                || ticketEvent.EventType == TicketEventType.Recalled)
            .ToList();
    }

    private static double AverageOrZero(List<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        return Math.Round(values.Average(), 2);
    }
}
