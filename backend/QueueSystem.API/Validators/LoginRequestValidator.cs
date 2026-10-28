using FluentValidation;
using QueueSystem.API.DTOs.Requests;

namespace QueueSystem.API.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(item => item.Username)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(item => item.Password)
            .NotEmpty()
            .MaximumLength(100);
    }
}
