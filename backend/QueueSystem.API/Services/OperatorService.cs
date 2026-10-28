using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Common;
using QueueSystem.API.Data;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Repositories.Interfaces;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Services;

public class OperatorService : IOperatorService
{
    private readonly IRepository<Operator> _operatorRepository;
    private readonly IRepository<Counter> _counterRepository;
    private readonly AppDbContext _dbContext;

    public OperatorService(IRepository<Operator> operatorRepository, IRepository<Counter> counterRepository, AppDbContext dbContext)
    {
        _operatorRepository = operatorRepository;
        _counterRepository = counterRepository;
        _dbContext = dbContext;
    }

    public async Task<List<OperatorResponseDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<Operator> operators = await _dbContext.Operators
            .Include(@operator => @operator.Counter)
            .AsNoTracking()
            .OrderBy(@operator => @operator.Id)
            .ToListAsync(cancellationToken);

        return operators
            .Select(@operator => new OperatorResponseDto
            {
                Id = @operator.Id,
                Name = @operator.Name,
                CounterId = @operator.CounterId,
                CounterName = @operator.Counter?.Name ?? string.Empty,
                IsActive = @operator.IsActive
            })
            .ToList();
    }

    public async Task<OperatorResponseDto> CreateAsync(CreateOperatorRequestDto request, CancellationToken cancellationToken)
    {
        Counter counter = await _counterRepository.GetById(request.CounterId, cancellationToken)
            ?? throw new NotFoundException($"Counter {request.CounterId} was not found.");

        Operator @operator = new Operator
        {
            Name = request.Name,
            CounterId = request.CounterId,
            IsActive = request.IsActive
        };

        await _operatorRepository.Add(@operator, cancellationToken);
        await _operatorRepository.SaveChanges(cancellationToken);

        return new OperatorResponseDto
        {
            Id = @operator.Id,
            Name = @operator.Name,
            CounterId = @operator.CounterId,
            CounterName = counter.Name,
            IsActive = @operator.IsActive
        };
    }

    public async Task<OperatorResponseDto> UpdateAsync(int id, UpdateOperatorRequestDto request, CancellationToken cancellationToken)
    {
        Operator @operator = await _operatorRepository.GetById(id, cancellationToken)
            ?? throw new NotFoundException($"Operator {id} was not found.");

        Counter counter = await _counterRepository.GetById(request.CounterId, cancellationToken)
            ?? throw new NotFoundException($"Counter {request.CounterId} was not found.");

        @operator.Name = request.Name;
        @operator.CounterId = request.CounterId;
        @operator.IsActive = request.IsActive;

        _operatorRepository.Update(@operator);
        await _operatorRepository.SaveChanges(cancellationToken);

        return new OperatorResponseDto
        {
            Id = @operator.Id,
            Name = @operator.Name,
            CounterId = @operator.CounterId,
            CounterName = counter.Name,
            IsActive = @operator.IsActive
        };
    }
}
