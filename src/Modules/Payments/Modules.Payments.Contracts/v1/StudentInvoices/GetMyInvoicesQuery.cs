using FSH.Modules.Payments.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.StudentInvoices;

/// <summary>Own invoices (caller is the student) plus every ward's invoices (caller is a guardian) —
/// see <c>PeopleScope.WardStudentIds</c>.</summary>
public sealed record GetMyInvoicesQuery(InvoiceStatus? Status = null) : IQuery<IReadOnlyList<StudentInvoiceDto>>;
