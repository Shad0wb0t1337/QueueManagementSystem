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

public class QueueService : IQueueService
{
    private const int RecentHistoryLimit = 10;
    private const string RecallModeImmediate = "Immediate";
    private const string RecallModeAfterCurrent = "AfterCurrent";
    private const string RecallModeAlreadyPending = "AlreadyPending";
    private const string RecallModeNotAllowed = "NotAllowed";

    private readonly ITicketRepository _ticketRepository;
    private readonly IRepository<Counter> _counterRepository;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<QueueHub> _hubContext;

    public QueueService(
        ITicketRepository ticketRepository,
        IRepository<Counter> counterRepository,
        AppDbContext dbContext,
        IHubContext<QueueHub> hubContext)
    {
        _ticketRepository = ticketRepository;
        _counterRepository = counterRepository;
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task<TicketCalledResponseDto> CallNextAsync(CallNextRequestDto request, int appUserId, CancellationToken cancellationToken)
    {
        Operator operatorProfile = await GetOperatorProfileAsync(appUserId, cancellationToken);
        EnsureOperatorCounterAccess(operatorProfile, request.CounterId);

        Counter counter = await _dbContext.Counters
            .Include(item => item.CounterServiceTypes)
                .ThenInclude(item => item.ServiceType)
            .FirstOrDefaultAsync(item => item.Id == request.CounterId, cancellationToken)
            ?? throw new NotFoundException($"Counter {request.CounterId} was not found.");

        if (counter.Status != CounterStatus.Active)
        {
            throw new BadRequestException("Call Next is allowed only when counter status is Active.");
        }

        DateTime now = DateTime.UtcNow;
        TicketSelectionResult selectionResult = await SelectNextTicketForCallAsync(counter, now, cancellationToken)
            ?? throw new NotFoundException("No waiting ticket found for this counter.");
        Ticket nextTicket = selectionResult.Ticket;

        TicketStatus previousStatus = nextTicket.Status;
        nextTicket.CounterId = counter.Id;
        nextTicket.Status = TicketStatus.Called;
        nextTicket.CalledAt = now;
        nextTicket.ServedAt = null;

        AddTicketEvent(
            nextTicket,
            TicketEventType.Called,
            now,
            operatorProfile.Id,
            counter.Id,
            previousStatus,
            TicketStatus.Called,
            "Ticket called by operator.");

        _ticketRepository.Update(nextTicket);
        await _ticketRepository.SaveChanges(cancellationToken);

        await _hubContext.Clients.Group(SignalRGroups.Display)
            .SendAsync(SignalREvents.TicketCalled, new
            {
                ticketNumber = nextTicket.TicketNumber,
                counterName = counter.Name,
                serviceTypeName = nextTicket.ServiceType?.Name ?? string.Empty
            }, cancellationToken);

        await BroadcastQueueUpdatedAsync(new[] { nextTicket.ServiceTypeId }, cancellationToken);

        await BroadcastDisplayUpdatedAsync(cancellationToken);
        await BroadcastCounterDisplayUpdatedAsync(counter.Id, cancellationToken);

        return new TicketCalledResponseDto
        {
            TicketId = nextTicket.Id,
            TicketNumber = nextTicket.TicketNumber,
            CounterId = counter.Id,
            CounterName = counter.Name,
            ServiceTypeId = nextTicket.ServiceTypeId,
            ServiceTypeName = nextTicket.ServiceType?.Name ?? string.Empty,
            Status = nextTicket.Status.ToString(),
            SelectionExplanation = selectionResult.SelectionExplanation
        };
    }

    public async Task<QueueTicketResponseDto> MarkServingAsync(int ticketId, int appUserId, CancellationToken cancellationToken)
    {
        Operator operatorProfile = await GetOperatorProfileAsync(appUserId, cancellationToken);

        Ticket ticket = await _dbContext.Tickets
            .Include(item => item.ServiceType)
            .FirstOrDefaultAsync(item => item.Id == ticketId, cancellationToken)
            ?? throw new NotFoundException($"Ticket {ticketId} was not found.");

        EnsureTicketCounterAccess(ticket, operatorProfile.CounterId);

        if (ticket.Status != TicketStatus.Called)
        {
            throw new BadRequestException("Ticket can be moved to Serving only from Called status.");
        }

        TicketStatus previousStatus = ticket.Status;
        DateTime now = DateTime.UtcNow;
        ticket.Status = TicketStatus.Serving;
        ticket.ServedAt = now;

        AddTicketEvent(
            ticket,
            TicketEventType.ServingStarted,
            now,
            operatorProfile.Id,
            ticket.CounterId,
            previousStatus,
            TicketStatus.Serving,
            "Ticket service started.");

        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChanges(cancellationToken);

        await _hubContext.Clients.Group(SignalRGroups.Service(ticket.ServiceTypeId))
            .SendAsync(SignalREvents.QueueUpdated, new { serviceTypeId = ticket.ServiceTypeId }, cancellationToken);

        await BroadcastDisplayUpdatedAsync(cancellationToken);
        await BroadcastCounterDisplayUpdatedAsync(ticket.CounterId, cancellationToken);

        return MapQueueTicket(ticket);
    }

    public async Task<QueueTicketResponseDto> MarkDoneAsync(int ticketId, int appUserId, CancellationToken cancellationToken)
    {
        Operator operatorProfile = await GetOperatorProfileAsync(appUserId, cancellationToken);

        Ticket ticket = await _dbContext.Tickets
            .Include(item => item.ServiceType)
            .FirstOrDefaultAsync(item => item.Id == ticketId, cancellationToken)
            ?? throw new NotFoundException($"Ticket {ticketId} was not found.");

        EnsureTicketCounterAccess(ticket, operatorProfile.CounterId);

        if (ticket.Status != TicketStatus.Called && ticket.Status != TicketStatus.Serving)
        {
            throw new BadRequestException("Ticket can be marked Done only from Called or Serving status.");
        }

        TicketStatus previousStatus = ticket.Status;
        DateTime now = DateTime.UtcNow;
        ticket.Status = TicketStatus.Done;

        AddTicketEvent(
            ticket,
            TicketEventType.Completed,
            now,
            operatorProfile.Id,
            ticket.CounterId,
            previousStatus,
            TicketStatus.Done,
            "Ticket completed.");

        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChanges(cancellationToken);

        await _hubContext.Clients.Group(SignalRGroups.Service(ticket.ServiceTypeId))
            .SendAsync(SignalREvents.QueueUpdated, new { serviceTypeId = ticket.ServiceTypeId }, cancellationToken);

        await BroadcastDisplayUpdatedAsync(cancellationToken);
        await BroadcastCounterDisplayUpdatedAsync(ticket.CounterId, cancellationToken);

        return MapQueueTicket(ticket);
    }

    public async Task<QueueTicketResponseDto> MarkNoShowAsync(int ticketId, int appUserId, CancellationToken cancellationToken)
    {
        Operator operatorProfile = await GetOperatorProfileAsync(appUserId, cancellationToken);

        Ticket ticket = await _dbContext.Tickets
            .Include(item => item.ServiceType)
            .FirstOrDefaultAsync(item => item.Id == ticketId, cancellationToken)
            ?? throw new NotFoundException($"Ticket {ticketId} was not found.");

        EnsureTicketCounterAccess(ticket, operatorProfile.CounterId);

        if (ticket.Status != TicketStatus.Called && ticket.Status != TicketStatus.Serving)
        {
            throw new BadRequestException("Ticket can be marked NoShow only from Called or Serving status.");
        }

        TicketStatus previousStatus = ticket.Status;
        DateTime now = DateTime.UtcNow;
        ticket.Status = TicketStatus.NoShow;

        AddTicketEvent(
            ticket,
            TicketEventType.NoShow,
            now,
            operatorProfile.Id,
            ticket.CounterId,
            previousStatus,
            TicketStatus.NoShow,
            "Ticket marked no-show.");

        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChanges(cancellationToken);

        Counter counter = await _dbContext.Counters
            .Include(item => item.CounterServiceTypes)
                .ThenInclude(item => item.ServiceType)
            .FirstOrDefaultAsync(item => item.Id == operatorProfile.CounterId, cancellationToken)
            ?? throw new NotFoundException($"Counter {operatorProfile.CounterId} was not found.");

        HashSet<int> affectedServiceTypeIds = new HashSet<int> { ticket.ServiceTypeId };

        if (counter.Status == CounterStatus.Active)
        {
            Ticket? autoCalled = await SelectNextRecallPendingTicketAsync(counter.Id, cancellationToken)
                ?? await SelectNextWaitingTicketAsync(counter, now, throwIfNoEnabledServices: false, cancellationToken);
            if (autoCalled != null)
            {
                TicketStatus autoCalledPreviousStatus = autoCalled.Status;
                autoCalled.CounterId = operatorProfile.CounterId;
                autoCalled.Status = TicketStatus.Called;
                autoCalled.CalledAt = now;
                autoCalled.ServedAt = null;

                AddTicketEvent(
                    autoCalled,
                    TicketEventType.Called,
                    now,
                    operatorProfile.Id,
                    counter.Id,
                    autoCalledPreviousStatus,
                    TicketStatus.Called,
                    "Ticket auto-called after no-show.");

                _ticketRepository.Update(autoCalled);
                await _ticketRepository.SaveChanges(cancellationToken);

                await _hubContext.Clients.Group(SignalRGroups.Display)
                    .SendAsync(SignalREvents.TicketCalled, new
                    {
                        ticketNumber = autoCalled.TicketNumber,
                        counterName = counter.Name,
                        serviceTypeName = autoCalled.ServiceType?.Name ?? string.Empty
                    }, cancellationToken);

                affectedServiceTypeIds.Add(autoCalled.ServiceTypeId);
            }
        }

        await BroadcastQueueUpdatedAsync(affectedServiceTypeIds, cancellationToken);

        await BroadcastDisplayUpdatedAsync(cancellationToken);
        await BroadcastCounterDisplayUpdatedAsync(operatorProfile.CounterId, cancellationToken);

        return MapQueueTicket(ticket);
    }

    public async Task<List<RecentTicketHistoryItemResponseDto>> GetRecentHistoryAsync(
        int counterId,
        int appUserId,
        CancellationToken cancellationToken)
    {
        Operator operatorProfile = await GetOperatorProfileAsync(appUserId, cancellationToken);
        EnsureOperatorCounterAccess(operatorProfile, counterId);

        Counter counter = await _dbContext.Counters
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == counterId, cancellationToken)
            ?? throw new NotFoundException($"Counter {counterId} was not found.");

        bool counterHasActiveTicket = await CounterHasActiveTicketAsync(counterId, cancellationToken);

        List<Ticket> tickets = await _dbContext.Tickets
            .Where(ticket => ticket.CounterId == counterId
                && (ticket.Status == TicketStatus.Done
                    || ticket.Status == TicketStatus.NoShow
                    || ticket.Status == TicketStatus.RecallPending))
            .Include(ticket => ticket.ServiceType)
            .Include(ticket => ticket.Counter)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return tickets
            .OrderByDescending(GetRecentHistorySortTime)
            .Take(RecentHistoryLimit)
            .Select(ticket => MapRecentHistoryItem(ticket, counter, counterHasActiveTicket))
            .ToList();
    }

    public async Task<RecallTicketResponseDto> RecallTicketAsync(
        int ticketId,
        int appUserId,
        CancellationToken cancellationToken)
    {
        Operator operatorProfile = await GetOperatorProfileAsync(appUserId, cancellationToken);

        Ticket ticket = await _dbContext.Tickets
            .Include(item => item.ServiceType)
            .Include(item => item.Counter)
            .FirstOrDefaultAsync(item => item.Id == ticketId, cancellationToken)
            ?? throw new NotFoundException($"Ticket {ticketId} was not found.");

        if (ticket.CounterId != operatorProfile.CounterId)
        {
            throw new ForbiddenException("Šis bilietas nepriklauso jūsų langeliui.");
        }

        if (ticket.Status == TicketStatus.RecallPending)
        {
            throw new BadRequestException("Bilietas jau laukia pakartotinio kvietimo.");
        }

        if (ticket.Status != TicketStatus.Done && ticket.Status != TicketStatus.NoShow)
        {
            throw new BadRequestException("Pakartotinai galima kviesti tik užbaigtą arba neatvykusį bilietą.");
        }

        Counter counter = await _dbContext.Counters
            .FirstOrDefaultAsync(item => item.Id == operatorProfile.CounterId, cancellationToken)
            ?? throw new NotFoundException($"Counter {operatorProfile.CounterId} was not found.");

        if (counter.Status != CounterStatus.Active)
        {
            throw new BadRequestException("Pakartotinai kviesti galima tik aktyviame langelyje.");
        }

        DateTime now = DateTime.UtcNow;
        bool counterHasActiveTicket = await CounterHasActiveTicketAsync(counter.Id, cancellationToken);
        string recallMode;

        TicketStatus previousStatus = ticket.Status;
        ticket.RecallRequestedAt = now;

        if (counterHasActiveTicket)
        {
            ticket.Status = TicketStatus.RecallPending;
            recallMode = RecallModeAfterCurrent;
            AddTicketEvent(
                ticket,
                TicketEventType.RecallPending,
                now,
                operatorProfile.Id,
                counter.Id,
                previousStatus,
                TicketStatus.RecallPending,
                "Ticket queued for recall after current service.");
        }
        else
        {
            ticket.Status = TicketStatus.Called;
            ticket.CalledAt = now;
            ticket.ServedAt = null;
            recallMode = RecallModeImmediate;
            AddTicketEvent(
                ticket,
                TicketEventType.Recalled,
                now,
                operatorProfile.Id,
                counter.Id,
                previousStatus,
                TicketStatus.Called,
                "Ticket recalled immediately.");
        }

        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChanges(cancellationToken);

        await BroadcastQueueUpdatedAsync(new[] { ticket.ServiceTypeId }, cancellationToken);
        await BroadcastDisplayUpdatedAsync(cancellationToken);
        await BroadcastCounterDisplayUpdatedAsync(counter.Id, cancellationToken);

        return new RecallTicketResponseDto
        {
            TicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            ServiceTypeId = ticket.ServiceTypeId,
            ServiceTypeName = ticket.ServiceType?.Name ?? string.Empty,
            CounterId = counter.Id,
            CounterName = counter.Name,
            Status = ticket.Status.ToString(),
            RecallMode = recallMode,
            Message = recallMode == RecallModeImmediate
                ? "Bilietas pakartotinai iškviestas."
                : "Bilietas bus pakviestas po dabartinio aptarnavimo."
        };
    }

    public async Task<RedirectTicketResponseDto> RedirectTicketAsync(
        int ticketId,
        int appUserId,
        RedirectTicketRequestDto request,
        CancellationToken cancellationToken)
    {
        Operator operatorProfile = await GetOperatorProfileAsync(appUserId, cancellationToken);

        string reason = request.Reason.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BadRequestException("Nukreipimo priežastis yra privaloma.");
        }

        Ticket ticket = await _dbContext.Tickets
            .Include(item => item.ServiceType)
            .FirstOrDefaultAsync(item => item.Id == ticketId, cancellationToken)
            ?? throw new NotFoundException($"Ticket {ticketId} was not found.");

        if (ticket.CounterId != operatorProfile.CounterId)
        {
            throw new ForbiddenException("Šis bilietas nepriklauso jūsų langeliui.");
        }

        if (ticket.Status != TicketStatus.Called && ticket.Status != TicketStatus.Serving)
        {
            throw new BadRequestException("Nukreipti galima tik šiuo metu iškviestą arba aptarnaujamą bilietą.");
        }

        if (ticket.ServiceTypeId == request.TargetServiceTypeId)
        {
            throw new BadRequestException("Bilietas jau priklauso pasirinktai paslaugai.");
        }

        ServiceType targetService = await _dbContext.ServiceTypes
            .FirstOrDefaultAsync(item => item.Id == request.TargetServiceTypeId, cancellationToken)
            ?? throw new NotFoundException($"Service type {request.TargetServiceTypeId} was not found.");

        if (!targetService.IsActive)
        {
            throw new BadRequestException("Paslauga šiuo metu neaptarnaujama, todėl į ją nukreipti negalima.");
        }

        bool targetServiceAvailable = await _dbContext.CounterServiceTypes
            .AnyAsync(item => item.ServiceTypeId == targetService.Id
                && item.IsEnabled
                && item.Counter != null
                && item.Counter.Status == CounterStatus.Active,
                cancellationToken);

        if (!targetServiceAvailable)
        {
            throw new BadRequestException("Paslauga šiuo metu neaptarnaujama, todėl į ją nukreipti negalima.");
        }

        TicketStatus previousStatus = ticket.Status;
        int fromServiceTypeId = ticket.ServiceTypeId;
        string fromServiceTypeName = ticket.ServiceType?.Name ?? string.Empty;
        int? fromCounterId = ticket.CounterId;
        DateTime now = DateTime.UtcNow;

        TicketRedirect redirect = new TicketRedirect
        {
            TicketId = ticket.Id,
            FromServiceTypeId = fromServiceTypeId,
            ToServiceTypeId = targetService.Id,
            FromCounterId = fromCounterId,
            RedirectedByOperatorId = operatorProfile.Id,
            Reason = reason,
            CreatedAt = now
        };

        ticket.ServiceTypeId = targetService.Id;
        ticket.Status = TicketStatus.Waiting;
        ticket.CounterId = null;
        ticket.QueueEnteredAt = now;
        ticket.CalledAt = null;
        ticket.ServedAt = null;
        ticket.RecallRequestedAt = null;

        AddTicketEvent(
            ticket,
            TicketEventType.Redirected,
            now,
            operatorProfile.Id,
            fromCounterId,
            previousStatus,
            TicketStatus.Waiting,
            reason,
            fromServiceTypeId,
            targetService.Id);

        _dbContext.TicketRedirects.Add(redirect);
        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChanges(cancellationToken);

        await BroadcastQueueUpdatedAsync(new[] { fromServiceTypeId, targetService.Id }, cancellationToken);
        await BroadcastDisplayUpdatedAsync(cancellationToken);
        await BroadcastCounterDisplayUpdatedAsync(fromCounterId, cancellationToken);

        return new RedirectTicketResponseDto
        {
            TicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            FromServiceTypeId = fromServiceTypeId,
            FromServiceTypeName = fromServiceTypeName,
            ToServiceTypeId = targetService.Id,
            ToServiceTypeName = targetService.Name,
            Status = ticket.Status.ToString(),
            QueueEnteredAt = ticket.QueueEnteredAt,
            Message = "Bilietas nukreiptas į pasirinktą paslaugą."
        };
    }

    private static QueueTicketResponseDto MapQueueTicket(Ticket ticket)
    {
        return new QueueTicketResponseDto
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
        };
    }

    private async Task<Ticket?> SelectNextRecallPendingTicketAsync(int counterId, CancellationToken cancellationToken)
    {
        return await _dbContext.Tickets
            .Where(ticket => ticket.CounterId == counterId && ticket.Status == TicketStatus.RecallPending)
            .Include(ticket => ticket.ServiceType)
            .OrderBy(ticket => ticket.RecallRequestedAt ?? ticket.ServedAt ?? ticket.CalledAt ?? ticket.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<TicketSelectionResult?> SelectNextTicketForCallAsync(
        Counter counter,
        DateTime now,
        CancellationToken cancellationToken)
    {
        Ticket? recallPendingTicket = await SelectNextRecallPendingTicketAsync(counter.Id, cancellationToken);
        if (recallPendingTicket != null)
        {
            return new TicketSelectionResult
            {
                Ticket = recallPendingTicket,
                SelectionExplanation = BuildRecallPendingSelectionExplanation()
            };
        }

        Ticket? waitingTicket = await SelectNextWaitingTicketAsync(counter, now, throwIfNoEnabledServices: true, cancellationToken);
        if (waitingTicket == null)
        {
            return null;
        }

        return new TicketSelectionResult
        {
            Ticket = waitingTicket,
            SelectionExplanation = BuildDynamicScoreSelectionExplanation(waitingTicket, now)
        };
    }

    private async Task<Ticket?> SelectNextWaitingTicketAsync(
        Counter counter,
        DateTime now,
        bool throwIfNoEnabledServices,
        CancellationToken cancellationToken)
    {
        List<ServiceType> supportedServiceTypes = counter.CounterServiceTypes
            .Where(item => item.IsEnabled && item.ServiceType != null)
            .Select(item => item.ServiceType!)
            .ToList();

        List<int> supportedServiceTypeIds = supportedServiceTypes
            .Select(serviceType => serviceType.Id)
            .ToList();

        if (supportedServiceTypeIds.Count == 0)
        {
            if (throwIfNoEnabledServices)
            {
                throw new BadRequestException("No services are enabled for this counter.");
            }

            return null;
        }

        Dictionary<int, ServiceType> serviceTypesById = supportedServiceTypes.ToDictionary(serviceType => serviceType.Id);

        List<Ticket> waitingTickets = await _dbContext.Tickets
            .Where(ticket => supportedServiceTypeIds.Contains(ticket.ServiceTypeId) && ticket.Status == TicketStatus.Waiting)
            .Include(ticket => ticket.ServiceType)
            .ToListAsync(cancellationToken);

        return waitingTickets
            .OrderByDescending(ticket => QueueScoringHelper.CalculateScore(ticket, serviceTypesById[ticket.ServiceTypeId], now))
            .ThenBy(ticket => ticket.QueueEnteredAt)
            .FirstOrDefault();
    }

    private static TicketSelectionExplanationResponseDto BuildDynamicScoreSelectionExplanation(Ticket ticket, DateTime now)
    {
        ServiceType serviceType = ticket.ServiceType
            ?? throw new InvalidOperationException("Selected ticket service type was not loaded.");
        QueueScoreDetails scoreDetails = QueueScoringHelper.CalculateScoreDetails(ticket, serviceType, now);

        return new TicketSelectionExplanationResponseDto
        {
            SelectionType = "DynamicScore",
            Message = "Bilietas parinktas pagal didžiausią prioritetinį įvertį.",
            PriorityLevel = scoreDetails.PriorityLevel,
            WaitingMinutes = Convert.ToInt32(Math.Floor(scoreDetails.WaitingMinutes)),
            FastTrackBonus = Math.Round(scoreDetails.FastTrackBonus, 2),
            SlaBoost = Math.Round(scoreDetails.SlaBoost, 2),
            EstimatedDurationMinutes = scoreDetails.EstimatedDurationMinutes,
            FinalScore = Math.Round(scoreDetails.FinalScore, 2)
        };
    }

    private static TicketSelectionExplanationResponseDto BuildRecallPendingSelectionExplanation()
    {
        return new TicketSelectionExplanationResponseDto
        {
            SelectionType = "RecallPending",
            Message = "Bilietas parinktas pirmiau, nes laukė pakartotinio kvietimo šiame langelyje."
        };
    }

    private async Task BroadcastQueueUpdatedAsync(IEnumerable<int> serviceTypeIds, CancellationToken cancellationToken)
    {
        foreach (int serviceTypeId in serviceTypeIds.Distinct())
        {
            await _hubContext.Clients.Group(SignalRGroups.Service(serviceTypeId))
                .SendAsync(SignalREvents.QueueUpdated, new { serviceTypeId }, cancellationToken);
        }
    }

    private async Task<Operator> GetOperatorProfileAsync(int appUserId, CancellationToken cancellationToken)
    {
        AppUser appUser = await _dbContext.AppUsers
            .Include(user => user.Operator)
            .FirstOrDefaultAsync(user => user.Id == appUserId, cancellationToken)
            ?? throw new NotFoundException("Authenticated user was not found.");

        if (appUser.Role != UserRole.Operator)
        {
            throw new ForbiddenException("Only operators can perform queue actions.");
        }

        if (appUser.Operator == null)
        {
            throw new ForbiddenException("Operator account is not linked to an operator profile.");
        }

        return appUser.Operator;
    }

    private static void EnsureOperatorCounterAccess(Operator operatorProfile, int requestedCounterId)
    {
        if (operatorProfile.CounterId != requestedCounterId)
        {
            throw new ForbiddenException("Operator can call next ticket only for own counter.");
        }
    }

    private static void EnsureTicketCounterAccess(Ticket ticket, int operatorCounterId)
    {
        if (ticket.CounterId != operatorCounterId)
        {
            throw new ForbiddenException("Operator can act only on tickets assigned to own counter.");
        }
    }

    private async Task<bool> CounterHasActiveTicketAsync(int counterId, CancellationToken cancellationToken)
    {
        return await _dbContext.Tickets
            .AnyAsync(ticket => ticket.CounterId == counterId
                && (ticket.Status == TicketStatus.Called || ticket.Status == TicketStatus.Serving),
                cancellationToken);
    }

    private static RecentTicketHistoryItemResponseDto MapRecentHistoryItem(
        Ticket ticket,
        Counter counter,
        bool counterHasActiveTicket)
    {
        string recallMode = GetRecallMode(ticket, counter, counterHasActiveTicket);

        return new RecentTicketHistoryItemResponseDto
        {
            TicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            TicketStatus = ticket.Status.ToString(),
            ServiceTypeId = ticket.ServiceTypeId,
            ServiceTypeName = ticket.ServiceType?.Name ?? string.Empty,
            CounterId = ticket.CounterId,
            CounterName = ticket.Counter?.Name ?? counter.Name,
            CompletedAt = ticket.Status == TicketStatus.Done || ticket.Status == TicketStatus.NoShow
                ? ticket.ServedAt ?? ticket.CalledAt
                : null,
            RecallRequestedAt = ticket.RecallRequestedAt,
            LastUpdatedAt = GetRecentHistorySortTime(ticket),
            CanRecall = recallMode == RecallModeImmediate || recallMode == RecallModeAfterCurrent,
            RecallMode = recallMode
        };
    }

    private static string GetRecallMode(Ticket ticket, Counter counter, bool counterHasActiveTicket)
    {
        if (ticket.Status == TicketStatus.RecallPending)
        {
            return RecallModeAlreadyPending;
        }

        if ((ticket.Status != TicketStatus.Done && ticket.Status != TicketStatus.NoShow) || counter.Status != CounterStatus.Active)
        {
            return RecallModeNotAllowed;
        }

        return counterHasActiveTicket ? RecallModeAfterCurrent : RecallModeImmediate;
    }

    private static DateTime GetRecentHistorySortTime(Ticket ticket)
    {
        return ticket.RecallRequestedAt ?? ticket.ServedAt ?? ticket.CalledAt ?? ticket.CreatedAt;
    }

    private async Task BroadcastDisplayUpdatedAsync(CancellationToken cancellationToken)
    {
        List<ActiveTicketResponseDto> calledTickets = await _dbContext.Tickets
            .Where(ticket => ticket.Status == TicketStatus.Called || ticket.Status == TicketStatus.Serving)
            .Include(ticket => ticket.Counter)
            .Include(ticket => ticket.ServiceType)
            .OrderByDescending(ticket => ticket.CalledAt)
            .Select(ticket => new ActiveTicketResponseDto
            {
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                CounterId = ticket.CounterId,
                CounterName = ticket.Counter != null ? ticket.Counter.Name : string.Empty,
                ServiceTypeId = ticket.ServiceTypeId,
                ServiceTypeName = ticket.ServiceType != null ? ticket.ServiceType.Name : string.Empty,
                Status = ticket.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        await _hubContext.Clients.Group(SignalRGroups.Display)
            .SendAsync(SignalREvents.DisplayUpdated, new { calledTickets }, cancellationToken);
    }

    private async Task BroadcastCounterDisplayUpdatedAsync(int? counterId, CancellationToken cancellationToken)
    {
        if (counterId == null)
        {
            return;
        }

        await _hubContext.Clients.Group(SignalRGroups.Counter(counterId.Value))
            .SendAsync(SignalREvents.CounterDisplayUpdated, new { counterId = counterId.Value }, cancellationToken);
    }

    private void AddTicketEvent(
        Ticket ticket,
        TicketEventType eventType,
        DateTime createdAt,
        int? operatorId,
        int? counterId,
        TicketStatus? previousStatus,
        TicketStatus? newStatus,
        string details,
        int? fromServiceTypeId = null,
        int? toServiceTypeId = null)
    {
        _dbContext.TicketEvents.Add(new TicketEvent
        {
            TicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            EventType = eventType,
            OperatorId = operatorId,
            CounterId = counterId,
            ServiceTypeId = ticket.ServiceTypeId,
            FromServiceTypeId = fromServiceTypeId,
            ToServiceTypeId = toServiceTypeId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            CreatedAt = createdAt,
            Details = details
        });
    }

    private sealed class TicketSelectionResult
    {
        public Ticket Ticket { get; init; } = null!;
        public TicketSelectionExplanationResponseDto SelectionExplanation { get; init; } = null!;
    }
}
