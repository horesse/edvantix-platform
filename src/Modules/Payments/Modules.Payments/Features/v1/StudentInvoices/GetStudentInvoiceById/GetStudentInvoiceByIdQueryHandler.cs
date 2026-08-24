using FSH.Framework.Core.Exceptions;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetStudentInvoiceById;

public sealed class GetStudentInvoiceByIdQueryHandler(PaymentsDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<GetStudentInvoiceByIdQuery, StudentInvoiceDetailDto>
{
    public async ValueTask<StudentInvoiceDetailDto> Handle(GetStudentInvoiceByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoice = await dbContext.StudentInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == query.InvoiceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Invoice {query.InvoiceId} not found.");

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return invoice.ToDetailDto(today);
    }
}
