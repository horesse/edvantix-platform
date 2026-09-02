using FSH.Framework.Core.Context;
using FSH.Modules.Payments.Contracts;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetMyMaterialsAccess;

public sealed class GetMyMaterialsAccessQueryHandler(
    IMaterialsAccessService materialsAccess,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyMaterialsAccessQuery, MaterialsAccessStatus>
{
    public async ValueTask<MaterialsAccessStatus> Handle(GetMyMaterialsAccessQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await materialsAccess
            .GetForUserAsync(currentUser.GetUserId(), cancellationToken)
            .ConfigureAwait(false);
    }
}
