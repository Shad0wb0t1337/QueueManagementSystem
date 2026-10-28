using FluentValidation;
using QueueSystem.API.DTOs.Requests;

namespace QueueSystem.API.Validators;

public class IssueTicketRequestValidator : AbstractValidator<IssueTicketRequestDto>
{
    public IssueTicketRequestValidator()
    {
        RuleFor(item => item.ServiceTypeId)
            .GreaterThan(0);
    }
}
