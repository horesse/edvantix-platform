using FSH.Framework.Core.Context;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using FSH.Modules.People.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetMyInvoices;

public sealed class GetMyInvoicesQueryHandler(PaymentsDbContext dbContext, IPeopleScopeResolver scopeResolver, ICurrentUser currentUser, TimeProvider timeProvider)
    : IQueryHandler<GetMyInvoicesQuery, IReadOnlyList<StudentInvoiceDto>>
{
    public async ValueTask<IReadOnlyList<StudentInvoiceDto>> Handle(GetMyInvoicesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var scope = await scopeResolver.ResolveAsync(currentUser.GetUserId().ToString(), cancellationToken).ConfigureAwait(false);

        var studentIds = new List<Guid>(scope.WardStudentIds);
        if (scope.StudentId is { } studentId)
        {
            studentIds.Add(studentId);
        }
        if (studentIds.Count == 0)
        {
            return [];
        }

        var q = dbContext.StudentInvoices.AsNoTracking().Where(i => studentIds.Contains(i.StudentId));
        if (query.Status is { } status)
        {
            q = q.Where(i => i.Status == status);
        }

        var invoices = await q.OrderByDescending(i => i.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return invoices.Select(i => i.ToDto(today)).ToList();
    }
}
