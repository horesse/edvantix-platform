using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.SearchStudentInvoices;

public static class SearchStudentInvoicesEndpoint
{
    internal static RouteHandlerBuilder MapSearchStudentInvoicesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/student-invoices",
                async (
                    Guid? studentId,
                    Guid? studyGroupId,
                    InvoiceStatus? status,
                    DateOnly? periodFrom,
                    DateOnly? periodTo,
                    bool? hasDebt,
                    string? search,
                    int pageNumber,
                    int pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var query = new SearchStudentInvoicesQuery(
                        studentId, studyGroupId, status, periodFrom, periodTo, hasDebt, search,
                        pageNumber == 0 ? 1 : pageNumber,
                        pageSize == 0 ? 50 : pageSize,
                        sortBy, sortDir);
                    return Results.Ok(await mediator.Send(query, ct));
                })
            .WithName("SearchStudentInvoices")
            .WithSummary("Search student invoices")
            .RequirePermission(PaymentsPermissions.StudentInvoices.View);
}
