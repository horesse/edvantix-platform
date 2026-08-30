using FSH.Modules.Auditing.Contracts.Catalog;
using Mediator;

namespace FSH.Modules.Auditing.Contracts.v1.GetAuditLabels;

/// <summary>
/// Returns the friendly-label dictionaries for audit entity type names and property names, so the
/// UI can render "Ученик" / "Статус" instead of "Student" / "Status". Static reference data — the
/// same for every tenant.
/// </summary>
public sealed record GetAuditLabelsQuery() : IQuery<AuditLabels>;

/// <param name="Entities">Simple CLR type name → label.</param>
/// <param name="Fields">Property name → label.</param>
public sealed record AuditLabels(
    IReadOnlyDictionary<string, string> Entities,
    IReadOnlyDictionary<string, string> Fields);
