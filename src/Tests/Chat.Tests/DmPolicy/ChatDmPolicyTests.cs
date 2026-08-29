using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Features.v1.Channels.DmPolicy;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.People.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts;
using NSubstitute;

namespace Chat.Tests.DmPolicy;

public sealed class ChatDmPolicyTests
{
    private readonly IPeopleScopeResolver _scopes = Substitute.For<IPeopleScopeResolver>();
    private readonly IStudyGroupQueryService _groups = Substitute.For<IStudyGroupQueryService>();
    private readonly IUserService _users = Substitute.For<IUserService>();
    private readonly IChatDmSettingsService _settings = Substitute.For<IChatDmSettingsService>();

    private ChatDmPolicy Policy() => new(_scopes, _groups, _users, _settings);

    public ChatDmPolicyTests()
    {
        _users.GetUserRolesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserRoleDto>());
        _scopes.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(PeopleScope.Empty);
        _settings.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new ChatDmSettingsDto(AllowStudentToStudentDm: false));
        _groups.GetActiveGroupIdsForTeacherAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)Array.Empty<Guid>());
        _groups.GetActiveStudyGroupIdsForStudentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)Array.Empty<Guid>());
    }

    private void AsRole(string userId, string role) =>
        _users.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<UserRoleDto> { new() { RoleName = role, Enabled = true } });

    private void AsScope(string userId, PeopleScope scope) =>
        _scopes.ResolveAsync(userId, Arg.Any<CancellationToken>()).Returns(scope);

    private static PeopleScope Student(Guid id) => new(id, null, null, []);
    private static PeopleScope Teacher(Guid id) => new(null, id, null, []);
    private static PeopleScope Guardian(Guid id, params Guid[] wards) => new(null, null, id, wards);

    [Fact]
    public async Task Same_User_Denied()
    {
        (await Policy().CanStartDmAsync("u1", "u1")).ShouldBeFalse();
    }

    [Fact]
    public async Task Manager_Target_Always_Allowed()
    {
        AsRole("mgr", "Manager");
        AsScope("stu", Student(Guid.NewGuid()));

        (await Policy().CanStartDmAsync("stu", "mgr")).ShouldBeTrue();
        (await Policy().CanStartDmAsync("mgr", "stu")).ShouldBeTrue();
    }

    [Fact]
    public async Task Two_Teachers_Allowed()
    {
        AsScope("t1", Teacher(Guid.NewGuid()));
        AsScope("t2", Teacher(Guid.NewGuid()));

        (await Policy().CanStartDmAsync("t1", "t2")).ShouldBeTrue();
    }

    [Fact]
    public async Task Student_And_Their_Teacher_Allowed_Both_Directions()
    {
        var studentId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var sharedGroup = Guid.NewGuid();
        AsScope("stu", Student(studentId));
        AsScope("tea", Teacher(teacherId));
        _groups.GetActiveGroupIdsForTeacherAsync(teacherId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)new[] { sharedGroup });
        _groups.GetActiveStudyGroupIdsForStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)new[] { sharedGroup });

        (await Policy().CanStartDmAsync("stu", "tea")).ShouldBeTrue();
        (await Policy().CanStartDmAsync("tea", "stu")).ShouldBeTrue();
    }

    [Fact]
    public async Task Student_And_Unrelated_Teacher_Denied()
    {
        AsScope("stu", Student(Guid.NewGuid()));
        AsScope("tea", Teacher(Guid.NewGuid()));
        // no shared groups (defaults)

        (await Policy().CanStartDmAsync("stu", "tea")).ShouldBeFalse();
    }

    [Fact]
    public async Task Guardian_And_Wards_Teacher_Allowed()
    {
        var wardId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var g = Guid.NewGuid();
        AsScope("gua", Guardian(Guid.NewGuid(), wardId));
        AsScope("tea", Teacher(teacherId));
        _groups.GetActiveGroupIdsForTeacherAsync(teacherId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)new[] { g });
        _groups.GetActiveStudyGroupIdsForStudentAsync(wardId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)new[] { g });

        (await Policy().CanStartDmAsync("gua", "tea")).ShouldBeTrue();
    }

    [Fact]
    public async Task Student_To_Student_Follows_The_School_Setting()
    {
        AsScope("s1", Student(Guid.NewGuid()));
        AsScope("s2", Student(Guid.NewGuid()));

        (await Policy().CanStartDmAsync("s1", "s2")).ShouldBeFalse();

        _settings.GetAsync(Arg.Any<CancellationToken>()).Returns(new ChatDmSettingsDto(AllowStudentToStudentDm: true));
        (await Policy().CanStartDmAsync("s1", "s2")).ShouldBeTrue();
    }

    [Fact]
    public async Task Guardian_To_Guardian_Denied()
    {
        AsScope("g1", Guardian(Guid.NewGuid(), Guid.NewGuid()));
        AsScope("g2", Guardian(Guid.NewGuid(), Guid.NewGuid()));

        (await Policy().CanStartDmAsync("g1", "g2")).ShouldBeFalse();
    }
}
