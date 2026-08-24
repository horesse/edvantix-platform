namespace FSH.Modules.Payments.Services;

/// <summary>
/// Keeps single-line, tariff-linked <c>Draft</c> invoices (the shape <c>BulkGenerateInvoicesCommand</c>
/// produces) in sync with Scheduling/StudyGroups activity that happens <em>after</em> generation but
/// <em>before</em> the invoice is issued — a Draft's line amount is computed once and does not
/// auto-recompute on its own. Called from the <c>SessionHeld</c>/<c>SessionCancelled</c>/
/// <c>StudentUnenrolled</c> integration-event handlers (see docs/02 Модули/Payments.md → «Подписки»).
/// A manually-edited Draft (not exactly one tariff-linked line) is left untouched — the manager
/// already took ownership of its content.
/// </summary>
public interface IDraftInvoiceRefreshService
{
    Task RefreshForGroupAsync(Guid studyGroupId, CancellationToken cancellationToken = default);
}
