using System.Collections.ObjectModel;

namespace FSH.Modules.Identity.Contracts.Authorization;

/// <summary>
/// Product-facing roles seeded for every school (non-root) tenant, on top of the
/// framework's own <c>Admin</c>/<c>Basic</c> (<c>FSH.Framework.Shared.Constants.RoleConstants</c>).
/// Deliberately **not** added to <c>RoleConstants.DefaultRoles</c> — that list lives in the
/// protected <c>BuildingBlocks</c> and marks roles the framework treats as system/undeletable
/// (see <c>RoleService.EnsureNotSystemRole</c>). These five are ordinary <c>FshRole</c> rows a
/// school can rename, re-permission, or delete like any role it creates itself — that
/// editability is the point (see docs/00 Обзор/Роли и сценарии.md).
/// </summary>
public static class SchoolRoleConstants
{
    public const string SchoolAdmin = nameof(SchoolAdmin);
    public const string Manager = nameof(Manager);
    public const string Teacher = nameof(Teacher);
    public const string Student = nameof(Student);
    public const string Guardian = nameof(Guardian);

    /// <summary>
    /// All school roles, in seeding order. Seeded only for non-root tenants — see
    /// <c>IdentityDbInitializer.SeedRolesAsync</c> and <c>RolePermissionSyncer.SyncAsync</c>.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new ReadOnlyCollection<string>(
    [
        SchoolAdmin,
        Manager,
        Teacher,
        Student,
        Guardian
    ]);
}
