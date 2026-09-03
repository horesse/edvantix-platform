using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

/// <summary>Re-prices an existing enrollment in place — sets <paramref name="TariffId"/> and
/// <paramref name="DiscountPercent"/> without re-enrolling the student (contrast
/// <see cref="TransferEnrollmentCommand"/>, which closes the row and opens a new one). Already-issued
/// invoices are left untouched; the new terms apply from the next accrual run, because
/// <c>BulkGenerateInvoicesCommand</c> resolves each enrollment's tariff live off the roster
/// (see docs/02 Модули/StudyGroups.md → «Смена тарифа»).</summary>
public sealed record ChangeEnrollmentTariffCommand(
    Guid EnrollmentId,
    Guid? TariffId,
    decimal DiscountPercent) : ICommand<Unit>;
