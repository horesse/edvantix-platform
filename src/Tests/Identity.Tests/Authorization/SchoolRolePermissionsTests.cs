using FSH.Framework.Shared.Constants;
using FSH.Modules.Identity.Authorization;
using FSH.Modules.Identity.Contracts.Authorization;

namespace Identity.Tests.Authorization;

/// <summary>
/// <see cref="SchoolRolePermissions"/> is pure and stateless, so these tests build a small
/// synthetic catalog rather than touching the process-wide <see cref="PermissionConstants"/>
/// registry — that keeps them independent of which modules happen to be registered when the
/// suite runs.
/// </summary>
public sealed class SchoolRolePermissionsTests
{
    private static readonly FshPermission UsersView = new("View Users", "View", "Users", IsBasic: true);
    private static readonly FshPermission UsersCreate = new("Create Users", "Create", "Users");
    private static readonly FshPermission UsersManageRoles = new("Manage User Roles", "ManageRoles", "Users");
    private static readonly FshPermission UsersInvite = new("Invite User", "Invite", "Users");
    private static readonly FshPermission RolesView = new("View Roles", "View", "Roles", IsBasic: true);
    private static readonly FshPermission RolesCreate = new("Create Roles", "Create", "Roles");
    private static readonly FshPermission StudentsView = new("View Students", "View", "Students");
    private static readonly FshPermission StudentsViewOwn = new("View Own Students", "ViewOwn", "Students");
    private static readonly FshPermission SessionsViewOwn = new("View Own Sessions", "ViewOwn", "Sessions");
    private static readonly FshPermission AttendanceMark = new("Mark Attendance", "Mark", "Attendance");
    private static readonly FshPermission LessonMaterialsView = new("View Lesson Materials", "View", "LessonMaterials");
    private static readonly FshPermission RootOnly = new("Manage Platform", "Manage", "Platform", IsRoot: true);

    private static readonly IReadOnlyList<FshPermission> Catalog =
    [
        UsersView, UsersCreate, UsersManageRoles, UsersInvite,
        RolesView, RolesCreate,
        StudentsView, StudentsViewOwn,
        SessionsViewOwn,
        AttendanceMark, LessonMaterialsView,
        RootOnly,
    ];

    #region Happy Path

    [Fact]
    public void Resolve_Should_ReturnEveryNonRootPermission_When_RoleIsSchoolAdmin()
    {
        var result = SchoolRolePermissions.Resolve(SchoolRoleConstants.SchoolAdmin, Catalog);

        result.ShouldNotContain(RootOnly);
        result.Count.ShouldBe(Catalog.Count - 1);
    }

    [Fact]
    public void Resolve_Should_ExcludeNonViewIdentityActions_When_RoleIsManager()
    {
        var result = SchoolRolePermissions.Resolve(SchoolRoleConstants.Manager, Catalog);

        result.ShouldContain(UsersView);
        result.ShouldContain(RolesView);
        result.ShouldNotContain(UsersCreate);
        result.ShouldNotContain(UsersManageRoles);
        result.ShouldNotContain(RolesCreate);
    }

    [Fact]
    public void Resolve_Should_IncludeUsersInvite_When_RoleIsManager()
    {
        var result = SchoolRolePermissions.Resolve(SchoolRoleConstants.Manager, Catalog);

        result.ShouldContain(UsersInvite);
    }

    [Fact]
    public void Resolve_Should_IncludeNonIdentityFullAccess_When_RoleIsManager()
    {
        // Students isn't in the Identity-managed resource list, so Manager gets every
        // Students action registered — not just View.
        var result = SchoolRolePermissions.Resolve(SchoolRoleConstants.Manager, Catalog);

        result.ShouldContain(StudentsView);
        result.ShouldContain(StudentsViewOwn);
    }

    [Fact]
    public void Resolve_Should_ExcludeRootPermissions_When_RoleIsManager()
    {
        var result = SchoolRolePermissions.Resolve(SchoolRoleConstants.Manager, Catalog);

        result.ShouldNotContain(RootOnly);
    }

    [Fact]
    public void Resolve_Should_ReturnOnlyViewOwnActions_When_RoleIsStudent()
    {
        var result = SchoolRolePermissions.Resolve(SchoolRoleConstants.Student, Catalog);

        result.ShouldBe([StudentsViewOwn, SessionsViewOwn], ignoreOrder: true);
    }

    [Fact]
    public void Resolve_Should_ReturnOnlyViewOwnActions_When_RoleIsGuardian()
    {
        var result = SchoolRolePermissions.Resolve(SchoolRoleConstants.Guardian, Catalog);

        result.ShouldBe([StudentsViewOwn, SessionsViewOwn], ignoreOrder: true);
    }

    [Fact]
    public void Resolve_Should_ReturnViewOwnPlusExtraGrants_When_RoleIsTeacher()
    {
        var result = SchoolRolePermissions.Resolve(SchoolRoleConstants.Teacher, Catalog);

        result.ShouldBe(
            [StudentsViewOwn, SessionsViewOwn, AttendanceMark, LessonMaterialsView],
            ignoreOrder: true);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Resolve_Should_ReturnEmpty_When_CatalogHasNoMatchingActions()
    {
        var sparseCatalog = new[] { UsersView, UsersCreate };

        var result = SchoolRolePermissions.Resolve(SchoolRoleConstants.Teacher, sparseCatalog);

        // Expected, not a bug — Attendance/LessonMaterials/Sessions don't exist in this catalog yet.
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_Should_NotThrow_When_CatalogIsEmpty()
    {
        var result = SchoolRolePermissions.Resolve(SchoolRoleConstants.SchoolAdmin, []);

        result.ShouldBeEmpty();
    }

    #endregion

    #region Exceptions

    [Fact]
    public void Resolve_Should_Throw_When_RoleNameIsUnknown()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => SchoolRolePermissions.Resolve("NotARole", Catalog));
    }

    [Fact]
    public void Resolve_Should_Throw_When_RoleNameIsNull()
    {
        Should.Throw<ArgumentNullException>(() => SchoolRolePermissions.Resolve(null!, Catalog));
    }

    [Fact]
    public void Resolve_Should_Throw_When_CatalogIsNull()
    {
        Should.Throw<ArgumentNullException>(() => SchoolRolePermissions.Resolve(SchoolRoleConstants.SchoolAdmin, null!));
    }

    #endregion
}
