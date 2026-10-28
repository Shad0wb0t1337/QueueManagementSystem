using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Manager))]
[Route("api/manager/departments")]
public class ManagerDepartmentsController : ControllerBase
{
    private readonly IManagerDepartmentService _managerDepartmentService;

    public ManagerDepartmentsController(IManagerDepartmentService managerDepartmentService)
    {
        _managerDepartmentService = managerDepartmentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ManagerDepartmentResponseDto>>> GetAssignedDepartments(CancellationToken cancellationToken)
    {
        int appUserId = GetAppUserId();
        List<ManagerDepartmentResponseDto> response =
            await _managerDepartmentService.GetAssignedDepartmentsAsync(appUserId, cancellationToken);
        return Ok(response);
    }

    private int GetAppUserId()
    {
        string? userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdRaw, out int appUserId))
        {
            throw new UnauthorizedAccessException("Invalid token user id.");
        }

        return appUserId;
    }
}
