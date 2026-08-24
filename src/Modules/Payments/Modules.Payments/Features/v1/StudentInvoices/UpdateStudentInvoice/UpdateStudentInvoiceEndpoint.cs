using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.UpdateStudentInvoice;

public static class UpdateStudentInvoiceEndpoint
{
    internal static RouteHandlerBuilder MapUpdateStudentInvoiceEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/student-invoices/{invoiceId:guid}",
                async (Guid invoiceId, UpdateStudentInvoiceBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(
                        new UpdateStudentInvoiceCommand(
                            invoiceId, body.PayerGuardianId, body.StudyGroupId, body.PeriodFrom, body.PeriodTo, body.DueDate, body.Comment, body.Lines),
                        ct);
                    return Results.NoContent();
                })
            .WithName("UpdateStudentInvoice")
            .WithSummary("Update a draft student invoice")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PaymentsPermissions.StudentInvoices.Create);

    public sealed record UpdateStudentInvoiceBody(
        Guid? PayerGuardianId,
        Guid? StudyGroupId,
        DateOnly PeriodFrom,
        DateOnly PeriodTo,
        DateOnly DueDate,
        string? Comment,
        IReadOnlyList<InvoiceLineInput> Lines);
}
