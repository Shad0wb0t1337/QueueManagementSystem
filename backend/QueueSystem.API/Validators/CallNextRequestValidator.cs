using FluentValidation;
using QueueSystem.API.DTOs.Requests;

namespace QueueSystem.API.Validators;

public class CallNextRequestValidator : AbstractValidator<CallNextRequestDto>
{
    public CallNextRequestValidator()
    {
        RuleFor(item => item.CounterId)
            .GreaterThan(0);
    }
}
