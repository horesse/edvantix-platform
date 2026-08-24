using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetStudentInvoiceById;

public static class GetStudentInvoiceByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetStudentInvoiceByIdEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/student-invoices/{invoiceId:guid}",
                async (Guid invoiceId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetStudentInvoiceByIdQuery(invoiceId), ct)))
            .WithName("GetStudentInvoiceById")
            .WithSummary("Get a student invoice with its lines")
            .Produces<StudentInvoiceDetailDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PaymentsPermissions.StudentInvoices.View);
}
