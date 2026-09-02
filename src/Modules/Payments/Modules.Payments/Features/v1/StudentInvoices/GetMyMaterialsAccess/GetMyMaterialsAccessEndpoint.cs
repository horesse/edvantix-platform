using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetMyMaterialsAccess;

public static class GetMyMaterialsAccessEndpoint
{
    internal static RouteHandlerBuilder MapGetMyMaterialsAccessEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/student-invoices/my/materials-access",
                async (IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetMyMaterialsAccessQuery(), ct)))
            .WithName("GetMyMaterialsAccess")
            .WithSummary("Whether the caller is blocked from lesson materials due to overdue payment (EDX-015)")
            .Produces<MaterialsAccessStatus>()
            .RequirePermission(PaymentsPermissions.StudentInvoices.ViewOwn);
}
