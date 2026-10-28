using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Common;
using QueueSystem.API.Data;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Services;

public class ServiceLoadService : IServiceLoadService
{
    private const string NoHelpCandidateMessage = "Šiuo metu nėra papildomos apkrautos paslaugos, kurią būtų galima įjungti.";

    private readonly AppDbContext _dbContext;
    private readonly ICounterService _counterService;

    public ServiceLoadService(AppDbContext dbContext, ICounterService counterService)
    {
        _dbContext = dbContext;
        _counterService = counterService;
    }

    public async Task<List<ServiceLoadSummaryResponseDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await GetSummariesAsync(serviceTypeIds: null, cancellationToken);
    }

    public async Task<List<ServiceLoadSummaryResponseDto>> GetForOperatorCounterAsync(
        int counterId,
        int appUserId,
        CancellationToken cancellationToken)
    {
        await EnsureOperatorCounterAccessAsync(counterId, appUserId, cancellationToken);

        List<int> serviceTypeIds = await _dbContext.CounterServiceTypes
            .Where(item => item.CounterId == counterId)
            .Select(item => item.ServiceTypeId)
            .ToListAsync(cancellationToken);

        return await GetSummariesAsync(serviceTypeIds, cancellationToken);
    }

    public async Task<HelpMostLoadedServiceResponseDto> HelpMostLoadedAsync(
        int counterId,
        int appUserId,
        CancellationToken cancellationToken)
    {
        await EnsureOperatorCounterAccessAsync(counterId, appUserId, cancellationToken);

        Counter counter = await _dbContext.Counters
            .Include(item => item.CounterServiceTypes)
                .ThenInclude(item => item.ServiceType)
            .FirstOrDefaultAsync(item => item.Id == counterId, cancellationToken)
            ?? throw new NotFoundException($"Counter {counterId} was not found.");

        List<int> supportedServiceTypeIds = counter.CounterServiceTypes
            .Select(item => item.ServiceTypeId)
            .ToList();

        List<ServiceLoadSummaryResponseDto> summaries = await GetSummariesAsync(supportedServiceTypeIds, cancellationToken);
        Dictionary<int, ServiceLoadSummaryResponseDto> summariesByServiceTypeId = summaries
            .ToDictionary(item => item.ServiceTypeId);

        CounterServiceType? candidate = counter.CounterServiceTypes
            .Where(item => !item.IsEnabled
                && item.ServiceType != null
                && item.ServiceType.IsActive
                && summariesByServiceTypeId.GetValueOrDefault(item.ServiceTypeId)?.WaitingCount > 0)
            .OrderByDescending(item => summariesByServiceTypeId[item.ServiceTypeId].ServicePressure)
            .ThenBy(item => item.ServiceType!.Name)
            .FirstOrDefault();

        if (candidate == null)
        {
            List<CounterServiceProfileItemResponseDto> currentProfile =
                await _counterService.GetServiceProfileAsync(counterId, appUserId, cancellationToken);

            return new HelpMostLoadedServiceResponseDto
            {
                EnabledServiceTypeId = null,
                EnabledServiceTypeName = null,
                Message = NoHelpCandidateMessage,
                ServiceProfile = currentProfile
            };
        }

        List<int> enabledServiceTypeIds = counter.CounterServiceTypes
            .Where(item => item.IsEnabled)
            .Select(item => item.ServiceTypeId)
            .Append(candidate.ServiceTypeId)
            .Distinct()
            .ToList();

        List<CounterServiceProfileItemResponseDto> updatedProfile =
            await _counterService.UpdateServiceProfileAsync(
                counterId,
                appUserId,
                new UpdateCounterServiceProfileRequestDto { EnabledServiceTypeIds = enabledServiceTypeIds },
                cancellationToken);

        return new HelpMostLoadedServiceResponseDto
        {
            EnabledServiceTypeId = candidate.ServiceTypeId,
            EnabledServiceTypeName = candidate.ServiceType?.Name,
            Message = $"Įjungta labiausiai apkrauta paslauga: {candidate.ServiceType?.Name}.",
            ServiceProfile = updatedProfile
        };
    }

    private async Task<List<ServiceLoadSummaryResponseDto>> GetSummariesAsync(
        List<int>? serviceTypeIds,
        CancellationToken cancellationToken)
    {
        IQueryable<ServiceType> serviceTypeQuery = _dbContext.ServiceTypes.AsNoTracking();
        if (serviceTypeIds != null)
        {
            if (serviceTypeIds.Count == 0)
            {
                return new List<ServiceLoadSummaryResponseDto>();
            }

            serviceTypeQuery = serviceTypeQuery.Where(serviceType => serviceTypeIds.Contains(serviceType.Id));
        }

        List<ServiceType> serviceTypes = await serviceTypeQuery
            .OrderBy(serviceType => serviceType.Name)
            .ToListAsync(cancellationToken);

        List<int> selectedServiceTypeIds = serviceTypes
            .Select(serviceType => serviceType.Id)
            .ToList();

        DateTime now = DateTime.UtcNow;

        List<Ticket> waitingTickets = await _dbContext.Tickets
            .Where(ticket => selectedServiceTypeIds.Contains(ticket.ServiceTypeId) && ticket.Status == TicketStatus.Waiting)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Dictionary<int, int> activeEnabledCounterCounts = await _dbContext.CounterServiceTypes
            .Where(item => selectedServiceTypeIds.Contains(item.ServiceTypeId)
                && item.IsEnabled
                && item.Counter != null
                && item.Counter.Status == CounterStatus.Active)
            .GroupBy(item => item.ServiceTypeId)
            .Select(group => new { ServiceTypeId = group.Key, Count = group.Select(item => item.CounterId).Distinct().Count() })
            .ToDictionaryAsync(item => item.ServiceTypeId, item => item.Count, cancellationToken);

        List<ServiceLoadSummaryResponseDto> summaries = new List<ServiceLoadSummaryResponseDto>();
        foreach (ServiceType serviceType in serviceTypes)
        {
            List<Ticket> serviceWaitingTickets = waitingTickets
                .Where(ticket => ticket.ServiceTypeId == serviceType.Id)
                .ToList();

            int waitingCount = serviceWaitingTickets.Count;
            int oldestWaitingMinutes = waitingCount == 0
                ? 0
                : serviceWaitingTickets.Max(ticket => GetWaitingMinutes(ticket.QueueEnteredAt, now));
            int activeEnabledCounterCount = activeEnabledCounterCounts.GetValueOrDefault(serviceType.Id);
            int estimatedWorkloadMinutes = waitingCount * serviceType.EstimatedDurationMinutes;
            int estimatedWaitMinutes = CalculateEstimatedWaitMinutes(waitingCount, serviceType.EstimatedDurationMinutes, activeEnabledCounterCount);
            int slaBreachCount = CalculateSlaBreachCount(serviceWaitingTickets, serviceType.MaxWaitMinutes, now);
            double servicePressure = CalculateServicePressure(
                estimatedWorkloadMinutes,
                activeEnabledCounterCount,
                oldestWaitingMinutes,
                slaBreachCount);
            ServiceLoadStatus loadStatus = CalculateLoadStatus(waitingCount, activeEnabledCounterCount, slaBreachCount, servicePressure);
            bool isCurrentlyAvailable = serviceType.IsActive && activeEnabledCounterCount > 0;

            summaries.Add(new ServiceLoadSummaryResponseDto
            {
                ServiceTypeId = serviceType.Id,
                ServiceTypeName = serviceType.Name,
                WaitingCount = waitingCount,
                OldestWaitingMinutes = oldestWaitingMinutes,
                EstimatedDurationMinutes = serviceType.EstimatedDurationMinutes,
                ActiveEnabledCounterCount = activeEnabledCounterCount,
                EstimatedWorkloadMinutes = estimatedWorkloadMinutes,
                EstimatedWaitMinutes = estimatedWaitMinutes,
                SlaBreachCount = slaBreachCount,
                ServicePressure = servicePressure,
                LoadStatus = loadStatus.ToString(),
                RecommendationMessage = GetRecommendationMessage(waitingCount, activeEnabledCounterCount, slaBreachCount, loadStatus),
                IsCurrentlyAvailable = isCurrentlyAvailable
            });
        }

        return summaries;
    }

    private async Task EnsureOperatorCounterAccessAsync(int counterId, int appUserId, CancellationToken cancellationToken)
    {
        AppUser appUser = await _dbContext.AppUsers
            .Include(user => user.Operator)
            .FirstOrDefaultAsync(user => user.Id == appUserId, cancellationToken)
            ?? throw new NotFoundException("Authenticated user was not found.");

        if (appUser.Role != UserRole.Operator)
        {
            throw new ForbiddenException("Only operators can view service load for a counter.");
        }

        if (appUser.Operator == null || appUser.Operator.CounterId != counterId)
        {
            throw new ForbiddenException("Operator can view only own counter service load.");
        }
    }

    private static int CalculateEstimatedWaitMinutes(int waitingCount, int estimatedDurationMinutes, int activeEnabledCounterCount)
    {
        if (activeEnabledCounterCount <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling((waitingCount + 1) * estimatedDurationMinutes / (double)activeEnabledCounterCount);
    }

    private static int CalculateSlaBreachCount(List<Ticket> waitingTickets, int? maxWaitMinutes, DateTime now)
    {
        if (maxWaitMinutes == null)
        {
            return 0;
        }

        return waitingTickets.Count(ticket => GetWaitingMinutes(ticket.QueueEnteredAt, now) >= maxWaitMinutes.Value);
    }

    private static double CalculateServicePressure(
        int estimatedWorkloadMinutes,
        int activeEnabledCounterCount,
        int oldestWaitingMinutes,
        int slaBreachCount)
    {
        return estimatedWorkloadMinutes / (double)Math.Max(1, activeEnabledCounterCount)
            + oldestWaitingMinutes
            + slaBreachCount * 30;
    }

    private static ServiceLoadStatus CalculateLoadStatus(
        int waitingCount,
        int activeEnabledCounterCount,
        int slaBreachCount,
        double servicePressure)
    {
        if (waitingCount == 0)
        {
            return ServiceLoadStatus.Low;
        }

        if (slaBreachCount > 0)
        {
            return ServiceLoadStatus.Critical;
        }

        if (activeEnabledCounterCount == 0)
        {
            return ServiceLoadStatus.Critical;
        }

        if (servicePressure >= 60)
        {
            return ServiceLoadStatus.High;
        }

        if (servicePressure >= 25)
        {
            return ServiceLoadStatus.Normal;
        }

        return ServiceLoadStatus.Low;
    }

    private static string? GetRecommendationMessage(
        int waitingCount,
        int activeEnabledCounterCount,
        int slaBreachCount,
        ServiceLoadStatus loadStatus)
    {
        if (waitingCount == 0)
        {
            return null;
        }

        if (activeEnabledCounterCount == 0)
        {
            return "Paslauga turi laukiančių pacientų, bet šiuo metu nėra ją aptarnaujančio aktyvaus langelio.";
        }

        if (slaBreachCount > 0)
        {
            return "Yra pacientų, viršijusių numatytą laukimo laiką.";
        }

        if (loadStatus == ServiceLoadStatus.High)
        {
            return "Rekomenduojama įjungti papildomą langelį šiai paslaugai.";
        }

        if (loadStatus == ServiceLoadStatus.Normal)
        {
            return null;
        }

        return null;
    }

    private static int GetWaitingMinutes(DateTime createdAt, DateTime now)
    {
        return Math.Max(0, (int)Math.Floor((now - createdAt).TotalMinutes));
    }
}
