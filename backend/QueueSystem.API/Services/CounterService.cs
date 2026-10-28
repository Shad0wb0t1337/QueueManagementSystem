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

public class CounterService : ICounterService
{
    private readonly IRepository<Counter> _counterRepository;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<QueueHub> _hubContext;

    public CounterService(
        IRepository<Counter> counterRepository,
        AppDbContext dbContext,
        IHubContext<QueueHub> hubContext)
    {
        _counterRepository = counterRepository;
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task<List<CounterResponseDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<Counter> counters = await _dbContext.Counters
            .Include(counter => counter.Department)
            .Include(counter => counter.CounterServiceTypes)
                .ThenInclude(item => item.ServiceType)
            .AsNoTracking()
            .OrderBy(counter => counter.DisplayOrder)
            .ThenBy(counter => counter.Id)
            .ToListAsync(cancellationToken);

        return counters
            .Select(MapCounter)
            .ToList();
    }

    public async Task<CounterResponseDto> CreateAsync(CreateCounterRequestDto request, CancellationToken cancellationToken)
    {
        List<ServiceType> serviceTypes = await GetRequestedServiceTypesAsync(request.ServiceTypeIds, cancellationToken);
        Department department = await GetActiveDepartmentAsync(request.DepartmentId, cancellationToken);
        CounterStatus status = ParseCounterStatus(request.Status);
        await EnsureDisplayOrderIsUniqueAsync(request.DisplayOrder, excludedCounterId: null, cancellationToken);

        Counter counter = new Counter
        {
            Name = request.Name,
            DisplayOrder = request.DisplayOrder,
            DepartmentId = department.Id,
            Department = department,
            Status = status,
            CounterServiceTypes = serviceTypes
                .Select(serviceType => new CounterServiceType { ServiceTypeId = serviceType.Id, IsEnabled = true })
                .ToList()
        };

        await _counterRepository.Add(counter, cancellationToken);
        await _counterRepository.SaveChanges(cancellationToken);
        await BroadcastCounterAvailabilityChangedAsync(serviceTypes.Select(serviceType => serviceType.Id), cancellationToken);

        return MapCounter(counter);
    }

    public async Task<CounterResponseDto> UpdateAsync(int id, UpdateCounterRequestDto request, CancellationToken cancellationToken)
    {
        Counter counter = await _dbContext.Counters
            .Include(item => item.Department)
            .Include(item => item.CounterServiceTypes)
                .ThenInclude(item => item.ServiceType)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Counter {id} was not found.");

        List<int> previousServiceTypeIds = counter.CounterServiceTypes
            .Select(item => item.ServiceTypeId)
            .ToList();
        Dictionary<int, bool> previousEnabledByServiceTypeId = counter.CounterServiceTypes
            .ToDictionary(item => item.ServiceTypeId, item => item.IsEnabled);

        List<ServiceType> serviceTypes = await GetRequestedServiceTypesAsync(request.ServiceTypeIds, cancellationToken);
        Department department = await GetActiveDepartmentAsync(request.DepartmentId, cancellationToken);
        CounterStatus status = ParseCounterStatus(request.Status);
        await EnsureDisplayOrderIsUniqueAsync(request.DisplayOrder, id, cancellationToken);

        counter.Name = request.Name;
        counter.DisplayOrder = request.DisplayOrder;
        counter.DepartmentId = department.Id;
        counter.Department = department;
        counter.Status = status;
        counter.CounterServiceTypes.Clear();
        foreach (ServiceType serviceType in serviceTypes)
        {
            counter.CounterServiceTypes.Add(new CounterServiceType
            {
                CounterId = counter.Id,
                ServiceTypeId = serviceType.Id,
                ServiceType = serviceType,
                IsEnabled = previousEnabledByServiceTypeId.GetValueOrDefault(serviceType.Id, true)
            });
        }

        _counterRepository.Update(counter);
        await _counterRepository.SaveChanges(cancellationToken);

        HashSet<int> affectedServiceTypeIds = previousServiceTypeIds.Concat(serviceTypes.Select(serviceType => serviceType.Id)).ToHashSet();
        await BroadcastCounterAvailabilityChangedAsync(affectedServiceTypeIds, cancellationToken);
        await BroadcastDisplayUpdatedAsync(cancellationToken);
        await BroadcastCounterDisplayUpdatedAsync(counter.Id, cancellationToken);

        return MapCounter(counter);
    }

    public async Task<List<CounterServiceProfileItemResponseDto>> GetServiceProfileAsync(
        int counterId,
        int appUserId,
        CancellationToken cancellationToken)
    {
        await EnsureOperatorCounterAccessAsync(counterId, appUserId, cancellationToken);
        Counter counter = await GetCounterWithServiceTypesAsync(counterId, cancellationToken);

        return await MapServiceProfileAsync(counter, cancellationToken);
    }

    public async Task<List<CounterServiceProfileItemResponseDto>> UpdateServiceProfileAsync(
        int counterId,
        int appUserId,
        UpdateCounterServiceProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureOperatorCounterAccessAsync(counterId, appUserId, cancellationToken);
        Counter counter = await GetCounterWithServiceTypesAsync(counterId, cancellationToken);

        HashSet<int> supportedServiceTypeIds = counter.CounterServiceTypes
            .Select(item => item.ServiceTypeId)
            .ToHashSet();

        HashSet<int> enabledServiceTypeIds = request.EnabledServiceTypeIds
            .Distinct()
            .ToHashSet();

        if (enabledServiceTypeIds.Any(serviceTypeId => !supportedServiceTypeIds.Contains(serviceTypeId)))
        {
            throw new BadRequestException("Cannot enable a service type that is not supported by this counter.");
        }

        foreach (CounterServiceType counterServiceType in counter.CounterServiceTypes)
        {
            counterServiceType.IsEnabled = enabledServiceTypeIds.Contains(counterServiceType.ServiceTypeId);
        }

        _counterRepository.Update(counter);
        await _counterRepository.SaveChanges(cancellationToken);
        await BroadcastCounterAvailabilityChangedAsync(supportedServiceTypeIds, cancellationToken);

        return await MapServiceProfileAsync(counter, cancellationToken);
    }

    public async Task<CounterResponseDto> UpdateOwnStatusAsync(int appUserId, UpdateOwnCounterStatusRequestDto request, CancellationToken cancellationToken)
    {
        AppUser appUser = await _dbContext.AppUsers
            .Include(user => user.Operator)
            .FirstOrDefaultAsync(user => user.Id == appUserId, cancellationToken)
            ?? throw new NotFoundException("Authenticated user was not found.");

        if (appUser.Role != UserRole.Operator)
        {
            throw new ForbiddenException("Only operators can update own counter status.");
        }

        Operator operatorProfile = appUser.Operator
            ?? throw new ForbiddenException("Operator account is not linked to an operator profile.");

        Counter counter = await _dbContext.Counters
            .Include(item => item.CounterServiceTypes)
                .ThenInclude(item => item.ServiceType)
            .FirstOrDefaultAsync(item => item.Id == operatorProfile.CounterId, cancellationToken)
            ?? throw new NotFoundException($"Counter {operatorProfile.CounterId} was not found.");

        counter.Status = ParseCounterStatus(request.Status);

        _counterRepository.Update(counter);
        await _counterRepository.SaveChanges(cancellationToken);
        await BroadcastCounterAvailabilityChangedAsync(counter.CounterServiceTypes.Select(item => item.ServiceTypeId), cancellationToken);
        await BroadcastDisplayUpdatedAsync(cancellationToken);
        await BroadcastCounterDisplayUpdatedAsync(counter.Id, cancellationToken);

        return MapCounter(counter);
    }

    private async Task<List<ServiceType>> GetRequestedServiceTypesAsync(List<int> serviceTypeIds, CancellationToken cancellationToken)
    {
        List<int> distinctServiceTypeIds = serviceTypeIds.Distinct().ToList();

        List<ServiceType> serviceTypes = await _dbContext.ServiceTypes
            .Where(serviceType => distinctServiceTypeIds.Contains(serviceType.Id))
            .ToListAsync(cancellationToken);

        if (serviceTypes.Count != distinctServiceTypeIds.Count)
        {
            throw new NotFoundException("One or more service types were not found.");
        }

        return serviceTypes;
    }

    private async Task<Department> GetActiveDepartmentAsync(int departmentId, CancellationToken cancellationToken)
    {
        Department department = await _dbContext.Departments
            .FirstOrDefaultAsync(item => item.Id == departmentId, cancellationToken)
            ?? throw new NotFoundException($"Department {departmentId} was not found.");

        if (!department.IsActive)
        {
            throw new BadRequestException("Inactive departments cannot be assigned to counters.");
        }

        return department;
    }

    private async Task EnsureDisplayOrderIsUniqueAsync(
        int displayOrder,
        int? excludedCounterId,
        CancellationToken cancellationToken)
    {
        if (displayOrder <= 0)
        {
            throw new BadRequestException("Counter display order must be positive.");
        }

        bool isDuplicate = await _dbContext.Counters
            .AnyAsync(counter =>
                counter.DisplayOrder == displayOrder &&
                (!excludedCounterId.HasValue || counter.Id != excludedCounterId.Value),
                cancellationToken);

        if (isDuplicate)
        {
            throw new BadRequestException("Counter display order must be unique.");
        }
    }

    private static CounterResponseDto MapCounter(Counter counter)
    {
        List<SupportedServiceTypeResponseDto> supportedServiceTypes = counter.CounterServiceTypes
            .Where(item => item.ServiceType != null)
            .OrderBy(item => item.ServiceType!.Name)
            .Select(item => new SupportedServiceTypeResponseDto
            {
                Id = item.ServiceTypeId,
                Name = item.ServiceType!.Name,
                IsEnabled = item.IsEnabled
            })
            .ToList();

        return new CounterResponseDto
        {
            Id = counter.Id,
            Name = counter.Name,
            DisplayOrder = counter.DisplayOrder,
            DepartmentId = counter.DepartmentId,
            DepartmentName = counter.Department?.Name ?? string.Empty,
            Status = counter.Status.ToString(),
            SupportedServiceTypeIds = counter.CounterServiceTypes
                .Select(item => item.ServiceTypeId)
                .OrderBy(id => id)
                .ToList(),
            SupportedServiceTypes = supportedServiceTypes
        };
    }

    private async Task<Counter> GetCounterWithServiceTypesAsync(int counterId, CancellationToken cancellationToken)
    {
        return await _dbContext.Counters
            .Include(counter => counter.CounterServiceTypes)
                .ThenInclude(item => item.ServiceType)
            .FirstOrDefaultAsync(counter => counter.Id == counterId, cancellationToken)
            ?? throw new NotFoundException($"Counter {counterId} was not found.");
    }

    private async Task EnsureOperatorCounterAccessAsync(int counterId, int appUserId, CancellationToken cancellationToken)
    {
        AppUser appUser = await _dbContext.AppUsers
            .Include(user => user.Operator)
            .FirstOrDefaultAsync(user => user.Id == appUserId, cancellationToken)
            ?? throw new NotFoundException("Authenticated user was not found.");

        if (appUser.Role != UserRole.Operator)
        {
            throw new ForbiddenException("Only operators can update service profile.");
        }

        if (appUser.Operator == null || appUser.Operator.CounterId != counterId)
        {
            throw new ForbiddenException("Operator can update only own counter service profile.");
        }
    }

    private async Task<List<CounterServiceProfileItemResponseDto>> MapServiceProfileAsync(
        Counter counter,
        CancellationToken cancellationToken)
    {
        List<int> serviceTypeIds = counter.CounterServiceTypes
            .Select(item => item.ServiceTypeId)
            .ToList();

        Dictionary<int, int> waitingCounts = await _dbContext.Tickets
            .Where(ticket => serviceTypeIds.Contains(ticket.ServiceTypeId) && ticket.Status == TicketStatus.Waiting)
            .GroupBy(ticket => ticket.ServiceTypeId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        return counter.CounterServiceTypes
            .Where(item => item.ServiceType != null)
            .OrderBy(item => item.ServiceType!.Name)
            .Select(item => new CounterServiceProfileItemResponseDto
            {
                ServiceTypeId = item.ServiceTypeId,
                ServiceTypeName = item.ServiceType!.Name,
                IsEnabled = item.IsEnabled,
                WaitingCount = waitingCounts.GetValueOrDefault(item.ServiceTypeId),
                PriorityLevel = item.ServiceType.PriorityLevel,
                IsFastTrack = item.ServiceType.IsFastTrack,
                MaxWaitMinutes = item.ServiceType.MaxWaitMinutes
            })
            .ToList();
    }

    private static CounterStatus ParseCounterStatus(string statusRaw)
    {
        string normalizedStatus = statusRaw?.Trim() ?? string.Empty;

        bool parsed = Enum.TryParse(normalizedStatus, ignoreCase: true, out CounterStatus status);
        if (!parsed)
        {
            throw new BadRequestException("Counter status is invalid. Allowed values: Active, Paused, Closed.");
        }

        return status;
    }

    private async Task BroadcastCounterAvailabilityChangedAsync(IEnumerable<int> serviceTypeIds, CancellationToken cancellationToken)
    {
        foreach (int serviceTypeId in serviceTypeIds.Distinct())
        {
            object payload = new { serviceTypeId };

            await _hubContext.Clients.Group(SignalRGroups.Service(serviceTypeId))
                .SendAsync(SignalREvents.QueueUpdated, payload, cancellationToken);

            await _hubContext.Clients.Group(SignalRGroups.Display)
                .SendAsync(SignalREvents.QueueUpdated, payload, cancellationToken);
        }
    }

    private async Task BroadcastCounterDisplayUpdatedAsync(int counterId, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(SignalRGroups.Counter(counterId))
            .SendAsync(SignalREvents.CounterDisplayUpdated, new { counterId }, cancellationToken);
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
}
