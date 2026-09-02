using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.StudentInvoices;

/// <summary>EDX-015 — whether the caller (student, or guardian of an overdue ward) is currently
/// blocked from lesson materials. Drives the "доступ ограничен" banner in the dashboard cabinet.</summary>
public sealed record GetMyMaterialsAccessQuery() : IQuery<MaterialsAccessStatus>;
