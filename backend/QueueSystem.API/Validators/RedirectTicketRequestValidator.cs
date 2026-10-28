using FluentValidation;
using QueueSystem.API.DTOs.Requests;

namespace QueueSystem.API.Validators;

public class RedirectTicketRequestValidator : AbstractValidator<RedirectTicketRequestDto>
{
    public RedirectTicketRequestValidator()
    {
        RuleFor(request => request.TargetServiceTypeId)
            .GreaterThan(0);

        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}
