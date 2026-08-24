using FSH.Framework.Core.Exceptions;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.UpdateStudentInvoice;

public sealed class UpdateStudentInvoiceCommandHandler(PaymentsDbContext dbContext)
    : ICommandHandler<UpdateStudentInvoiceCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateStudentInvoiceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await dbContext.StudentInvoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Invoice {command.InvoiceId} not found.");

        invoice.UpdateHeader(command.PayerGuardianId, command.StudyGroupId, command.PeriodFrom, command.PeriodTo, command.DueDate, command.Comment);
        invoice.ReplaceLines(command.Lines.Select(l => (l.Description, l.TariffId, l.Quantity, l.UnitPrice)).ToList());

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
