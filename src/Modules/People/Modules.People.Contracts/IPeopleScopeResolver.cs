using FSH.Modules.People.Contracts.Dtos;

namespace FSH.Modules.People.Contracts;

/// <summary>
/// Resolves an Identity user id to their domain identity (<see cref="PeopleScope"/>). The result
/// is cached (see <c>PeopleScopeResolver</c> in the runtime project); callers don't need to think
/// about invalidation — People's own command handlers invalidate on link/unlink and guardian
/// changes.
/// </summary>
public interface IPeopleScopeResolver
{
    ValueTask<PeopleScope> ResolveAsync(string userId, CancellationToken cancellationToken = default);
}
