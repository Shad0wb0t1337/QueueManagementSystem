using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Common;
using QueueSystem.API.Data;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Services;

public class ManagerDepartmentService : IManagerDepartmentService
{
    private readonly AppDbContext _dbContext;

    public ManagerDepartmentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ManagerUserResponseDto>> GetManagersAsync(CancellationToken cancellationToken)
    {
        List<AppUser> managers = await _dbContext.AppUsers
            .Where(user => user.Role == UserRole.Manager)
            .Include(user => user.ManagerDepartments)
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .ToListAsync(cancellationToken);

        return managers.Select(manager => new ManagerUserResponseDto
        {
            UserId = manager.Id,
            Username = manager.Username,
            DepartmentIds = manager.ManagerDepartments
                .Select(item => item.DepartmentId)
                .OrderBy(id => id)
                .ToList()
        }).ToList();
    }

    public async Task<List<ManagerDepartmentResponseDto>> GetManagerDepartmentsAsync(
        int appUserId,
        CancellationToken cancellationToken)
    {
        AppUser manager = await GetManagerUserAsync(appUserId, cancellationToken);
        return await MapDepartmentLinksAsync(manager.Id, includeInactive: true, cancellationToken);
    }

    public async Task<List<ManagerDepartmentResponseDto>> GetAssignedDepartmentsAsync(
        int appUserId,
        CancellationToken cancellationToken)
    {
        AppUser manager = await GetManagerUserAsync(appUserId, cancellationToken);
        return await MapDepartmentLinksAsync(manager.Id, includeInactive: false, cancellationToken);
    }

    public async Task<List<ManagerDepartmentResponseDto>> UpdateManagerDepartmentsAsync(
        int appUserId,
        UpdateManagerDepartmentsRequestDto request,
        CancellationToken cancellationToken)
    {
        AppUser manager = await GetManagerUserAsync(appUserId, cancellationToken);
        List<int> departmentIds = request.DepartmentIds.Distinct().OrderBy(id => id).ToList();

        List<Department> departments = await _dbContext.Departments
            .Where(department => departmentIds.Contains(department.Id))
            .ToListAsync(cancellationToken);

        if (departments.Count != departmentIds.Count)
        {
            throw new NotFoundException("One or more departments were not found.");
        }

        if (departments.Any(department => !department.IsActive))
        {
            throw new BadRequestException("Inactive departments cannot be assigned to managers.");
        }

        List<ManagerDepartment> existingLinks = await _dbContext.ManagerDepartments
            .Where(item => item.AppUserId == manager.Id)
            .ToListAsync(cancellationToken);

        _dbContext.ManagerDepartments.RemoveRange(existingLinks);
        foreach (int departmentId in departmentIds)
        {
            _dbContext.ManagerDepartments.Add(new ManagerDepartment
            {
                AppUserId = manager.Id,
                DepartmentId = departmentId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await MapDepartmentLinksAsync(manager.Id, includeInactive: true, cancellationToken);
    }

    private async Task<AppUser> GetManagerUserAsync(int appUserId, CancellationToken cancellationToken)
    {
        AppUser appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(user => user.Id == appUserId, cancellationToken)
            ?? throw new NotFoundException($"User {appUserId} was not found.");

        if (appUser.Role != UserRole.Manager)
        {
            throw new BadRequestException("Department assignments can be created only for Manager users.");
        }

        return appUser;
    }

    private async Task<List<ManagerDepartmentResponseDto>> MapDepartmentLinksAsync(
        int appUserId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ManagerDepartments
            .Where(item => item.AppUserId == appUserId
                && item.Department != null
                && (includeInactive || item.Department.IsActive))
            .OrderBy(item => item.Department!.Name)
            .Select(item => new ManagerDepartmentResponseDto
            {
                DepartmentId = item.DepartmentId,
                DepartmentName = item.Department!.Name
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
