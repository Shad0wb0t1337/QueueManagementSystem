using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/admin/departments")]
public class AdminDepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public AdminDepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DepartmentResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        List<DepartmentResponseDto> response = await _departmentService.GetAllAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentResponseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        DepartmentResponseDto response = await _departmentService.GetByIdAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentResponseDto>> Create(
        [FromBody] CreateDepartmentRequestDto request,
        CancellationToken cancellationToken)
    {
        DepartmentResponseDto response = await _departmentService.CreateAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmentResponseDto>> Update(
        int id,
        [FromBody] UpdateDepartmentRequestDto request,
        CancellationToken cancellationToken)
    {
        DepartmentResponseDto response = await _departmentService.UpdateAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{id:int}/deactivate")]
    public async Task<ActionResult<DepartmentResponseDto>> Deactivate(int id, CancellationToken cancellationToken)
    {
        DepartmentResponseDto response = await _departmentService.DeactivateAsync(id, cancellationToken);
        return Ok(response);
    }
}
