using FSH.Framework.Shared.Persistence;
using FSH.Modules.Payments.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.StudentInvoices;

public sealed record SearchStudentInvoicesQuery(
    Guid? StudentId = null,
    Guid? StudyGroupId = null,
    InvoiceStatus? Status = null,
    DateOnly? PeriodFrom = null,
    DateOnly? PeriodTo = null,
    bool? HasDebt = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<StudentInvoiceDto>>;
