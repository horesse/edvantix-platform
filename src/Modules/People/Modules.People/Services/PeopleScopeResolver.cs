using FSH.Framework.Caching;
using FSH.Modules.People.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Modules.People.Services;

/// <summary>
/// Answers "who is this Identity user in the domain" for row-level "is this mine" checks in
/// StudyGroups/Scheduling/Payments. Cached via <see cref="HybridCache"/> (not raw Redis — see
/// caching.md) keyed by user id, tagged with the shared <c>CacheKeys.Tags.User(userId)</c> tag so
/// People's Link/Unlink/AddStudentGuardian/RemoveStudentGuardian handlers can invalidate it
/// without a new BuildingBlocks tag (see buildingblocks-protection.md — no touching CacheKeys.cs
/// for a module-local key).
/// </summary>
public sealed class PeopleScopeResolver(PeopleDbContext dbContext, HybridCache cache) : IPeopleScopeResolver
{
    private static readonly HybridCacheEntryOptions ScopeEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(30),
        LocalCacheExpiration = TimeSpan.FromMinutes(2),
    };

    /// <summary>Cache key for a user's <see cref="PeopleScope"/> — module-local (not in the shared
    /// <c>CacheKeys</c> class, which lives in the protected BuildingBlocks/Caching project).</summary>
    internal static string CacheKey(string userId) => $"people:scope:u:{userId}";

    public ValueTask<PeopleScope> ResolveAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var tags = new[] { CacheKeys.Tags.User(userId) };
        var state = new FactoryState(dbContext, userId);
        return cache.GetOrCreateAsync(
            CacheKey(userId),
            state,
            LoadAsync,
            ScopeEntryOptions,
            tags,
            cancellationToken);
    }

    private static async ValueTask<PeopleScope> LoadAsync(FactoryState state, CancellationToken ct)
    {
        var (dbContext, userId) = state;

        // A single person can hold more than one role with the same account (e.g. a teacher whose
        // child studies at the same school is both Teacher and Guardian) — all three are resolved,
        // not short-circuited on the first match.
        var studentId = await dbContext.Students
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var teacherId = await dbContext.Teachers
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var guardianId = await dbContext.Guardians
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        IReadOnlyList<Guid> wardIds = [];
        if (guardianId is { } gid)
        {
            wardIds = await dbContext.StudentGuardians
                .AsNoTracking()
                .Where(sg => sg.GuardianId == gid)
                .Select(sg => sg.StudentId)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        if (studentId is null && teacherId is null && guardianId is null)
        {
            return PeopleScope.Empty;
        }

        return new PeopleScope(studentId, teacherId, guardianId, wardIds);
    }

    private readonly record struct FactoryState(PeopleDbContext DbContext, string UserId);
}
