using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Common;
using QueueSystem.API.Data;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Repositories.Interfaces;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Services;

public class ServiceTypeService : IServiceTypeService
{
    private readonly IRepository<ServiceType> _serviceTypeRepository;
    private readonly AppDbContext _dbContext;

    public ServiceTypeService(IRepository<ServiceType> serviceTypeRepository, AppDbContext dbContext)
    {
        _serviceTypeRepository = serviceTypeRepository;
        _dbContext = dbContext;
    }

    public async Task<List<ServiceTypeResponseDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        IQueryable<ServiceType> serviceTypeQuery = _dbContext.ServiceTypes.AsNoTracking();
        if (!includeInactive)
        {
            serviceTypeQuery = serviceTypeQuery.Where(serviceType => serviceType.IsActive);
        }

        List<ServiceType> serviceTypes = await serviceTypeQuery
            .OrderBy(serviceType => serviceType.Id)
            .ToListAsync(cancellationToken);

        Dictionary<int, int> waitingCounts = await _dbContext.Tickets
            .Where(ticket => ticket.Status == TicketStatus.Waiting)
            .GroupBy(ticket => ticket.ServiceTypeId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        Dictionary<int, int> activeCounterCounts = await _dbContext.CounterServiceTypes
            .Where(item => item.IsEnabled && item.Counter != null && item.Counter.Status == CounterStatus.Active)
            .GroupBy(item => item.ServiceTypeId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        return serviceTypes
            .Select(serviceType => new ServiceTypeResponseDto
            {
                Id = serviceType.Id,
                Name = serviceType.Name,
                Description = serviceType.Description,
                EstimatedDurationMinutes = serviceType.EstimatedDurationMinutes,
                IsActive = serviceType.IsActive,
                Prefix = serviceType.Prefix,
                PriorityLevel = serviceType.PriorityLevel,
                IsFastTrack = serviceType.IsFastTrack,
                MaxWaitMinutes = serviceType.MaxWaitMinutes,
                CurrentQueueLength = waitingCounts.GetValueOrDefault(serviceType.Id),
                EstimatedWaitMinutes = CalculateEstimatedWaitMinutes(
                    waitingCounts.GetValueOrDefault(serviceType.Id),
                    serviceType.EstimatedDurationMinutes,
                    activeCounterCounts.GetValueOrDefault(serviceType.Id)),
                HasActiveCounter = activeCounterCounts.GetValueOrDefault(serviceType.Id) > 0
            })
            .ToList();
    }

    public async Task<ServiceTypeResponseDto> CreateAsync(CreateServiceTypeRequestDto request, CancellationToken cancellationToken)
    {
        ServiceType serviceType = new ServiceType
        {
            Name = request.Name,
            Description = request.Description,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            IsActive = request.IsActive,
            PriorityLevel = request.PriorityLevel,
            IsFastTrack = request.IsFastTrack,
            MaxWaitMinutes = request.MaxWaitMinutes,
            Prefix = await GetNextPrefixAsync(cancellationToken)
        };

        await _serviceTypeRepository.Add(serviceType, cancellationToken);
        await _serviceTypeRepository.SaveChanges(cancellationToken);

        return new ServiceTypeResponseDto
        {
            Id = serviceType.Id,
            Name = serviceType.Name,
            Description = serviceType.Description,
            EstimatedDurationMinutes = serviceType.EstimatedDurationMinutes,
            IsActive = serviceType.IsActive,
            Prefix = serviceType.Prefix,
            PriorityLevel = serviceType.PriorityLevel,
            IsFastTrack = serviceType.IsFastTrack,
            MaxWaitMinutes = serviceType.MaxWaitMinutes,
            CurrentQueueLength = 0,
            EstimatedWaitMinutes = 0,
            HasActiveCounter = false
        };
    }

    public async Task<ServiceTypeResponseDto> UpdateAsync(int id, UpdateServiceTypeRequestDto request, CancellationToken cancellationToken)
    {
        ServiceType serviceType = await _serviceTypeRepository.GetById(id, cancellationToken)
            ?? throw new NotFoundException($"Service type {id} was not found.");

        serviceType.Name = request.Name;
        serviceType.Description = request.Description;
        serviceType.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        serviceType.IsActive = request.IsActive;
        serviceType.PriorityLevel = request.PriorityLevel;
        serviceType.IsFastTrack = request.IsFastTrack;
        serviceType.MaxWaitMinutes = request.MaxWaitMinutes;

        _serviceTypeRepository.Update(serviceType);
        await _serviceTypeRepository.SaveChanges(cancellationToken);

        int waitingCount = await _dbContext.Tickets
            .Where(ticket => ticket.ServiceTypeId == id && ticket.Status == TicketStatus.Waiting)
            .CountAsync(cancellationToken);

        int activeCounterCount = await _dbContext.CounterServiceTypes
            .Where(item => item.ServiceTypeId == id && item.IsEnabled && item.Counter != null && item.Counter.Status == CounterStatus.Active)
            .CountAsync(cancellationToken);

        return new ServiceTypeResponseDto
        {
            Id = serviceType.Id,
            Name = serviceType.Name,
            Description = serviceType.Description,
            EstimatedDurationMinutes = serviceType.EstimatedDurationMinutes,
            IsActive = serviceType.IsActive,
            Prefix = serviceType.Prefix,
            PriorityLevel = serviceType.PriorityLevel,
            IsFastTrack = serviceType.IsFastTrack,
            MaxWaitMinutes = serviceType.MaxWaitMinutes,
            CurrentQueueLength = waitingCount,
            EstimatedWaitMinutes = CalculateEstimatedWaitMinutes(waitingCount, serviceType.EstimatedDurationMinutes, activeCounterCount),
            HasActiveCounter = activeCounterCount > 0
        };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        ServiceType serviceType = await _serviceTypeRepository.GetById(id, cancellationToken)
            ?? throw new NotFoundException($"Service type {id} was not found.");

        bool hasRelatedData = await _dbContext.CounterServiceTypes.AnyAsync(item => item.ServiceTypeId == id, cancellationToken)
            || await _dbContext.Tickets.AnyAsync(ticket => ticket.ServiceTypeId == id, cancellationToken);

        if (hasRelatedData)
        {
            throw new BadRequestException("Service type cannot be deleted because it has related counters or tickets.");
        }

        _serviceTypeRepository.Delete(serviceType);
        await _serviceTypeRepository.SaveChanges(cancellationToken);
    }

    private async Task<string> GetNextPrefixAsync(CancellationToken cancellationToken)
    {
        List<string> existingPrefixes = await _dbContext.ServiceTypes
            .Select(serviceType => serviceType.Prefix)
            .ToListAsync(cancellationToken);

        HashSet<string> used = existingPrefixes
            .Select(prefix => prefix.Trim().ToUpperInvariant())
            .ToHashSet();

        for (char letter = 'A'; letter <= 'Z'; letter++)
        {
            string prefix = letter.ToString();
            if (!used.Contains(prefix))
            {
                return prefix;
            }
        }

        throw new BadRequestException("No more prefixes are available.");
    }

    private static int CalculateEstimatedWaitMinutes(int waitingCount, int estimatedDurationMinutes, int activeCounterCount)
    {
        if (activeCounterCount <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling((waitingCount + 1) * estimatedDurationMinutes / (double)activeCounterCount);
    }
}
