using FluentValidation;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.Models;

namespace QueueSystem.API.Validators;

public class UpdateOwnCounterStatusRequestValidator : AbstractValidator<UpdateOwnCounterStatusRequestDto>
{
    public UpdateOwnCounterStatusRequestValidator()
    {
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
