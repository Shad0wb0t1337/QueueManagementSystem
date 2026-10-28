using FluentValidation;
using QueueSystem.API.DTOs.Requests;

namespace QueueSystem.API.Validators;

public class UpdateCounterServiceProfileRequestValidator : AbstractValidator<UpdateCounterServiceProfileRequestDto>
{
    public UpdateCounterServiceProfileRequestValidator()
    {
        RuleForEach(item => item.EnabledServiceTypeIds)
            .GreaterThan(0);
    }
}
