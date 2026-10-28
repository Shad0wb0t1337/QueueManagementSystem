using FluentValidation;
using QueueSystem.API.DTOs.Requests;

namespace QueueSystem.API.Validators;

public class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequestDto>
{
    public CreateDepartmentRequestValidator()
    {
        RuleFor(item => item.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(item => item.Description)
            .MaximumLength(500);
    }
}

public class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequestDto>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(item => item.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(item => item.Description)
            .MaximumLength(500);
    }
}

public class UpdateManagerDepartmentsRequestValidator : AbstractValidator<UpdateManagerDepartmentsRequestDto>
{
    public UpdateManagerDepartmentsRequestValidator()
    {
        RuleForEach(item => item.DepartmentIds)
            .GreaterThan(0);
    }
}
