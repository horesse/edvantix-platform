using FSH.Modules.Auditing.Contracts.Catalog;
using FSH.Modules.Auditing.Contracts.v1.GetAuditLabels;
using Mediator;

namespace FSH.Modules.Auditing.Features.v1.GetAuditLabels;

public sealed class GetAuditLabelsQueryHandler
    : IQueryHandler<GetAuditLabelsQuery, AuditLabels>
{
    private static readonly AuditLabels Value =
        new(AuditLabelCatalog.Entities, AuditLabelCatalog.Fields);

    public ValueTask<AuditLabels> Handle(GetAuditLabelsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return ValueTask.FromResult(Value);
    }
}
