using FluentValidation;
using QueueSystem.API.DTOs.Requests;

namespace QueueSystem.API.Validators;

public class CreateOperatorRequestValidator : AbstractValidator<CreateOperatorRequestDto>
{
    public CreateOperatorRequestValidator()
    {
        RuleFor(item => item.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(item => item.CounterId)
            .GreaterThan(0);
    }
}
