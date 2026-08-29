using FSH.Framework.Shared.Constants;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.People.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts;

namespace FSH.Modules.Chat.Features.v1.Channels.DmPolicy;

/// <summary>
/// Who may start a direct message with whom (docs/02 Модули/Chat.md → «Ограничение личных
/// сообщений»). Gates <c>FindOrCreateDmCommand</c>; existing conversations are not retro-closed.
///
/// - Managers / school admins / platform admins: reachable by anyone, can reach anyone.
/// - Teacher ↔ teacher: allowed (colleagues).
/// - Student or guardian ↔ a teacher who teaches them / their ward: allowed (either direction).
/// - Student ↔ student: only when the school enabled it (<c>ChatDmSettings.AllowStudentToStudentDm</c>).
/// - Everything else (guardian ↔ guardian, student ↔ unrelated teacher, …): denied.
/// </summary>
public interface IChatDmPolicy
{
    Task<bool> CanStartDmAsync(string currentUserId, string targetUserId, CancellationToken ct = default);
}

public sealed class ChatDmPolicy(
    IPeopleScopeResolver scopeResolver,
    IStudyGroupQueryService studyGroups,
    IUserService users,
    IChatDmSettingsService settings)
    : IChatDmPolicy
{
    private static readonly string[] StaffRoles =
        [RoleConstants.Admin, SchoolRoleConstants.SchoolAdmin, SchoolRoleConstants.Manager];

    public async Task<bool> CanStartDmAsync(string currentUserId, string targetUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currentUserId) || string.IsNullOrWhiteSpace(targetUserId)
            || string.Equals(currentUserId, targetUserId, StringComparison.Ordinal))
        {
            return false;
        }

        if (await IsStaffAsync(currentUserId, ct).ConfigureAwait(false)
            || await IsStaffAsync(targetUserId, ct).ConfigureAwait(false))
        {
            return true;
        }

        var a = await scopeResolver.ResolveAsync(currentUserId, ct).ConfigureAwait(false);
        var b = await scopeResolver.ResolveAsync(targetUserId, ct).ConfigureAwait(false);

        var aTeacher = a.TeacherId;
        var bTeacher = b.TeacherId;

        if (aTeacher is not null && bTeacher is not null)
        {
            return true;
        }

        if (bTeacher is { } bt && await TeachesAnyAsync(bt, StudentIdsOf(a), ct).ConfigureAwait(false))
        {
            return true;
        }

        if (aTeacher is { } at && await TeachesAnyAsync(at, StudentIdsOf(b), ct).ConfigureAwait(false))
        {
            return true;
        }

        if (a.StudentId is not null && b.StudentId is not null && aTeacher is null && bTeacher is null)
        {
            return (await settings.GetAsync(ct).ConfigureAwait(false)).AllowStudentToStudentDm;
        }

        return false;
    }

    private static IReadOnlyList<Guid> StudentIdsOf(PeopleScope scope)
    {
        if (scope.StudentId is not { } sid)
        {
            return scope.WardStudentIds;
        }

        var list = new List<Guid>(scope.WardStudentIds.Count + 1) { sid };
        list.AddRange(scope.WardStudentIds);
        return list;
    }

    private async Task<bool> TeachesAnyAsync(Guid teacherId, IReadOnlyList<Guid> studentIds, CancellationToken ct)
    {
        if (studentIds.Count == 0)
        {
            return false;
        }

        var teacherGroups = await studyGroups.GetActiveGroupIdsForTeacherAsync(teacherId, ct).ConfigureAwait(false);
        if (teacherGroups.Count == 0)
        {
            return false;
        }

        var teacherGroupSet = teacherGroups.ToHashSet();
        foreach (var studentId in studentIds)
        {
            var studentGroups = await studyGroups
                .GetActiveStudyGroupIdsForStudentAsync(studentId, ct).ConfigureAwait(false);
            if (studentGroups.Any(teacherGroupSet.Contains))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsStaffAsync(string userId, CancellationToken ct)
    {
        var roles = await users.GetUserRolesAsync(userId, ct).ConfigureAwait(false);
        return roles.Any(r =>
            r.Enabled && r.RoleName is { } name && StaffRoles.Contains(name, StringComparer.OrdinalIgnoreCase));
    }
}
