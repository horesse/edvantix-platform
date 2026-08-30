using FluentValidation;
using FSH.Framework.Web.Validation;
using FSH.Modules.Auditing.Contracts.v1.GetAudits;

namespace FSH.Modules.Auditing.Features.v1.GetAudits;

public sealed class GetAuditsQueryValidator : AbstractValidator<GetAuditsQuery>
{
    public GetAuditsQueryValidator()
    {
        Include(new PagedQueryValidator<GetAuditsQuery>());

        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <= q.ToUtc)
            .WithMessage("FromUtc must be less than or equal to ToUtc.");

        // Reject oversized windows up-front (user sees a 400, not a silent clamp). The handler
        // still clamps as defence in depth (e.g. when only one endpoint is supplied).
        RuleFor(q => q)
            .Must(q =>
                !q.FromUtc.HasValue
                || !q.ToUtc.HasValue
                || (q.ToUtc.Value - q.FromUtc.Value) <= GetAuditsQueryHandler.MaxWindow)
            .WithMessage($"Audit query window cannot exceed {GetAuditsQueryHandler.MaxWindow.TotalDays:0} days.");

        // EntityKey alone is ambiguous ("Id:..." collides across entity types); it only
        // narrows a history query when paired with the entity type name.
        RuleFor(q => q.EntityName)
            .NotEmpty()
            .When(q => !string.IsNullOrWhiteSpace(q.EntityKey))
            .WithMessage("EntityName is required when EntityKey is supplied.");
    }
}
