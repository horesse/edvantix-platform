using FSH.Framework.Shared.Constants;
using FSH.Modules.Identity.Contracts.Authorization;

namespace FSH.Modules.Identity.Authorization;

/// <summary>
/// Resolves the permission bundle for each <see cref="SchoolRoleConstants"/> role from the live
/// <see cref="PermissionConstants"/> catalog. Pure and stateless — callers (<c>IdentityDbInitializer</c>
/// for the initial seed, <see cref="RolePermissionSyncer"/> for the periodic top-up) pass the
/// catalog snapshot in, so this class has no dependency on when a module has registered its
/// permissions.
/// </summary>
/// <remarks>
/// Bundles are built by filtering the catalog rather than enumerating permission names, so a
/// role's access grows automatically as the People/Curriculum/StudyGroups/Scheduling/Payments
/// modules register their own permissions in later releases — the same "grows with the catalog"
/// idea already used for <c>PermissionConstants.Admin</c>/<c>Basic</c>. Until those modules
/// exist, the Teacher/Student/Guardian bundles are legitimately empty (or near-empty) — that is
/// expected, not a bug.
/// </remarks>
internal static class SchoolRolePermissions
{
    private const string ViewOwnAction = "ViewOwn";

    /// <summary>
    /// Identity resources a school <c>Manager</c> only gets <see cref="ActionConstants.View"/> on —
    /// role/permission administration stays with <c>SchoolAdmin</c>.
    /// </summary>
    private static readonly HashSet<string> IdentityManagedResources = new(StringComparer.Ordinal)
    {
        IdentityPermissions.Users.Resource,
        IdentityPermissions.UserRoles.Resource,
        IdentityPermissions.Roles.Resource,
        IdentityPermissions.RoleClaims.Resource,
        IdentityPermissions.Sessions.Resource,
        IdentityPermissions.Groups.Resource,
        IdentityPermissions.Impersonation.Resource,
    };

    /// <summary>
    /// Extra grants for <c>Teacher</c> beyond the <c>*.ViewOwn</c> convention, per the table in
    /// docs/01 Архитектура/Модель прав доступа.md. Resources not yet registered (Scheduling,
    /// Curriculum) simply produce no match — see the type-level remarks.
    /// </summary>
    private static readonly (string Resource, string Action)[] TeacherExtraGrants =
    [
        ("Attendance", "Mark"),
        ("LessonMaterials", ActionConstants.View),
        ("Sessions", ActionConstants.View),
    ];

    /// <summary>
    /// Resolves the permission bundle for one school role. Throws for any name outside
    /// <see cref="SchoolRoleConstants.All"/> — callers only ever iterate that list.
    /// </summary>
    public static IReadOnlyList<FshPermission> Resolve(string roleName, IReadOnlyList<FshPermission> catalog)
    {
        ArgumentNullException.ThrowIfNull(roleName);
        ArgumentNullException.ThrowIfNull(catalog);

        return roleName switch
        {
            SchoolRoleConstants.SchoolAdmin => ResolveSchoolAdmin(catalog),
            SchoolRoleConstants.Manager => ResolveManager(catalog),
            SchoolRoleConstants.Teacher => ResolveViewOwn(catalog, TeacherExtraGrants),
            SchoolRoleConstants.Student => ResolveViewOwn(catalog, []),
            SchoolRoleConstants.Guardian => ResolveViewOwn(catalog, []),
            _ => throw new ArgumentOutOfRangeException(nameof(roleName), roleName, "Unknown school role."),
        };
    }

    // SchoolAdmin = "all non-root permissions of the tenant" — the exact bundle PermissionConstants.Admin
    // already computes for the framework Admin role. No exceptions, by design (docs table: "все
    // не-root права тенанта").
    private static List<FshPermission> ResolveSchoolAdmin(IReadOnlyList<FshPermission> catalog)
        => [.. catalog.Where(p => !p.IsRoot)];

    // Manager = same "all non-root" bundle, minus Identity's non-View actions (role/user
    // administration stays with SchoolAdmin), plus the one Identity exception it does need:
    // inviting a new user (Users.Invite) without full Users.Create/ManageRoles.
    private static List<FshPermission> ResolveManager(IReadOnlyList<FshPermission> catalog)
    {
        var result = catalog
            .Where(p => !p.IsRoot)
            .Where(p => !IdentityManagedResources.Contains(p.Resource) || p.Action == ActionConstants.View)
            .ToList();

        var invite = catalog.FirstOrDefault(p =>
            p.Resource == IdentityPermissions.Users.Resource && p.Action == "Invite");
        if (invite is not null && !result.Contains(invite))
        {
            result.Add(invite);
        }

        return result;
    }

    // Teacher/Student/Guardian = "*.ViewOwn" across the whole catalog, plus a short explicit
    // exception list per role (Teacher only, for now — Guardian's extra grant in the docs table,
    // StudentInvoices.ViewOwn, is already covered by the glob).
    private static List<FshPermission> ResolveViewOwn(
        IReadOnlyList<FshPermission> catalog, IReadOnlyCollection<(string Resource, string Action)> extraGrants)
    {
        var result = catalog.Where(p => p.Action == ViewOwnAction).ToList();

        foreach (var (resource, action) in extraGrants)
        {
            var match = catalog.FirstOrDefault(p => p.Resource == resource && p.Action == action);
            if (match is not null && !result.Contains(match))
            {
                result.Add(match);
            }
        }

        return result;
    }
}
