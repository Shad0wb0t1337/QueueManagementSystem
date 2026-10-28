using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/managers")]
public class AdminManagersController : ControllerBase
{
    private readonly IManagerDepartmentService _managerDepartmentService;

    public AdminManagersController(IManagerDepartmentService managerDepartmentService)
    {
        _managerDepartmentService = managerDepartmentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ManagerUserResponseDto>>> GetManagers(CancellationToken cancellationToken)
    {
        List<ManagerUserResponseDto> response = await _managerDepartmentService.GetManagersAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{userId:int}/departments")]
    public async Task<ActionResult<List<ManagerDepartmentResponseDto>>> GetDepartments(
        int userId,
        CancellationToken cancellationToken)
    {
        List<ManagerDepartmentResponseDto> response =
            await _managerDepartmentService.GetManagerDepartmentsAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{userId:int}/departments")]
    public async Task<ActionResult<List<ManagerDepartmentResponseDto>>> UpdateDepartments(
        int userId,
        [FromBody] UpdateManagerDepartmentsRequestDto request,
        CancellationToken cancellationToken)
    {
        List<ManagerDepartmentResponseDto> response =
            await _managerDepartmentService.UpdateManagerDepartmentsAsync(userId, request, cancellationToken);
        return Ok(response);
    }
}
