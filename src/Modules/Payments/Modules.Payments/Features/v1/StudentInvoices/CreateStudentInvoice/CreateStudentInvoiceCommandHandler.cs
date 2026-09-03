using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Domain;
using FSH.Modules.Payments.Services;
using Mediator;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.CreateStudentInvoice;

public sealed class CreateStudentInvoiceCommandHandler(
    PaymentsDbContext dbContext,
    IInvoiceNumberGenerator numberGenerator)
    : ICommandHandler<CreateStudentInvoiceCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateStudentInvoiceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var number = await numberGenerator.NextAsync(cancellationToken).ConfigureAwait(false);

        var invoice = StudentInvoice.Create(
            number,
            command.StudentId,
            command.PayerGuardianId,
            command.StudyGroupId,
            command.PeriodFrom,
            command.PeriodTo,
            command.DueDate,
            command.Currency,
            command.Comment);

        if (command.Lines.Count > 0)
        {
            invoice.ReplaceLines(command.Lines.Select(l => (l.Description, l.TariffId, l.Quantity, l.UnitPrice)).ToList());
        }

        dbContext.StudentInvoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return invoice.Id;
    }
}
