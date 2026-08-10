using FSH.Modules.People.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.People.Contracts.v1;

/// <summary>Resolves the caller's own <see cref="PeopleScope"/> — backs <c>GET /people/me/scope</c>,
/// used by the frontend to know which of Student/Teacher/Guardian the current user is.</summary>
public sealed record GetMyPeopleScopeQuery : IQuery<PeopleScope>;
