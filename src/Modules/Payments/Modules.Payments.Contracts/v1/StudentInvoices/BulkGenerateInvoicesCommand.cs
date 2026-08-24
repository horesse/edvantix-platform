using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.StudentInvoices;

/// <summary>Idempotent per (StudyGroupId, PeriodFrom, PeriodTo) — a repeat call for the same group
/// and period returns the existing invoices instead of creating duplicates (see
/// docs/02 Модули/Payments.md → «Массовое выставление — главный сценарий менеджера»).
/// <paramref name="IssueImmediately"/> defaults to <c>false</c>: drafts are checked by a human before
/// being issued.</summary>
public sealed record BulkGenerateInvoicesCommand(
    Guid StudyGroupId,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    DateOnly DueDate,
    bool IssueImmediately = false) : ICommand<IReadOnlyList<Guid>>;
