using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Common;
using QueueSystem.API.Data;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Services;

public class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _dbContext;

    public DepartmentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DepartmentResponseDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<Department> departments = await _dbContext.Departments
            .Include(department => department.Counters)
            .Include(department => department.ManagerDepartments)
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .ToListAsync(cancellationToken);

        return departments.Select(MapDepartment).ToList();
    }

    public async Task<DepartmentResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Department department = await _dbContext.Departments
            .Include(item => item.Counters)
            .Include(item => item.ManagerDepartments)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Department {id} was not found.");

        return MapDepartment(department);
    }

    public async Task<DepartmentResponseDto> CreateAsync(CreateDepartmentRequestDto request, CancellationToken cancellationToken)
    {
        await EnsureNameIsUniqueAsync(request.Name, excludedId: null, cancellationToken);

        Department department = new Department
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapDepartment(department);
    }

    public async Task<DepartmentResponseDto> UpdateAsync(
        int id,
        UpdateDepartmentRequestDto request,
        CancellationToken cancellationToken)
    {
        Department department = await _dbContext.Departments
            .Include(item => item.Counters)
            .Include(item => item.ManagerDepartments)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Department {id} was not found.");

        await EnsureNameIsUniqueAsync(request.Name, id, cancellationToken);

        department.Name = request.Name.Trim();
        department.Description = request.Description.Trim();
        department.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapDepartment(department);
    }

    public async Task<DepartmentResponseDto> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        Department department = await _dbContext.Departments
            .Include(item => item.Counters)
            .Include(item => item.ManagerDepartments)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Department {id} was not found.");

        department.IsActive = false;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapDepartment(department);
    }

    private async Task EnsureNameIsUniqueAsync(string name, int? excludedId, CancellationToken cancellationToken)
    {
        string normalizedName = name.Trim();
        bool nameExists = await _dbContext.Departments
            .AnyAsync(department => department.Name == normalizedName
                && (excludedId == null || department.Id != excludedId.Value),
                cancellationToken);

        if (nameExists)
        {
            throw new BadRequestException("Department name must be unique.");
        }
    }

    private static DepartmentResponseDto MapDepartment(Department department)
    {
        return new DepartmentResponseDto
        {
            DepartmentId = department.Id,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            CreatedAt = department.CreatedAt,
            CounterCount = department.Counters.Count,
            ManagerCount = department.ManagerDepartments.Count
        };
    }
}
