using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetStudentBalance;

public static class GetStudentBalanceEndpoint
{
    internal static RouteHandlerBuilder MapGetStudentBalanceEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/students/{studentId:guid}/balance",
                async (Guid studentId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetStudentBalanceQuery(studentId), ct)))
            .WithName("GetStudentBalance")
            .WithSummary("Get a student's balance (charged, paid, debt, advance)")
            .Produces<StudentBalanceDto>()
            .RequirePermission(PaymentsPermissions.StudentInvoices.View);
}
