using FluentValidation;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.Models;

namespace QueueSystem.API.Validators;

public class CreateCounterRequestValidator : AbstractValidator<CreateCounterRequestDto>
{
    public CreateCounterRequestValidator()
    {
        RuleFor(item => item.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(item => item.DisplayOrder)
            .GreaterThan(0)
            .WithMessage("Display order must be positive.");

        RuleFor(item => item.DepartmentId)
            .GreaterThan(0);

        RuleFor(item => item.ServiceTypeIds)
            .NotEmpty()
            .WithMessage("At least one service type must be selected.");

        RuleForEach(item => item.ServiceTypeIds)
            .GreaterThan(0);

        RuleFor(item => item.Status)
            .NotEmpty()
            .Must(BeValidCounterStatus)
            .WithMessage("Counter status must be one of: Active, Paused, Closed.");
    }

    private static bool BeValidCounterStatus(string status)
    {
        return Enum.TryParse<CounterStatus>(status, true, out _);
    }
}
