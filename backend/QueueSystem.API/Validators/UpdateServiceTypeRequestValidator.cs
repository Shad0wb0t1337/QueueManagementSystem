using FluentValidation;
using QueueSystem.API.DTOs.Requests;

namespace QueueSystem.API.Validators;

public class UpdateServiceTypeRequestValidator : AbstractValidator<UpdateServiceTypeRequestDto>
{
    public UpdateServiceTypeRequestValidator()
    {
        RuleFor(item => item.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(item => item.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(item => item.EstimatedDurationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(240);

        RuleFor(item => item.PriorityLevel)
            .InclusiveBetween(1, 5);

        RuleFor(item => item.MaxWaitMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(1440)
            .When(item => item.MaxWaitMinutes.HasValue);
    }
}
