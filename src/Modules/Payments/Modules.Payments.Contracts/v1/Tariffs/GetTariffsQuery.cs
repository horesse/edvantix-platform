using FSH.Modules.Payments.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.Tariffs;

/// <summary>Small reference list (a handful to a few dozen tariffs per school) — not paginated, same
/// convention as Scheduling's <c>GetRoomsQuery</c>.</summary>
public sealed record GetTariffsQuery(bool? IsActive = null) : IQuery<IReadOnlyList<TariffDto>>;
