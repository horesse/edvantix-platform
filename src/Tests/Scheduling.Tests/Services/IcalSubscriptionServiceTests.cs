using FSH.Framework.Core.Context;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Services;
using NSubstitute;

namespace Scheduling.Tests.Services;

public sealed class IcalSubscriptionServiceTests
{
    private static IcalSubscriptionService CreateService(SchedulingDbContext db, Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.GetUserId().Returns(userId);
        return new IcalSubscriptionService(db, currentUser, TimeProvider.System);
    }

    [Fact]
    public async Task GetForCurrentUser_Returns_Null_When_None()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var service = CreateService(db, Guid.NewGuid());

        (await service.GetForCurrentUserAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Rotate_Creates_Then_Replaces_The_Token()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var service = CreateService(db, userId);

        var first = await service.RotateForCurrentUserAsync();
        first.UserId.ShouldBe(userId);
        first.Token.ShouldNotBeNullOrWhiteSpace();
        var firstId = first.Id;
        var firstToken = first.Token;

        var second = await service.RotateForCurrentUserAsync();
        second.Id.ShouldBe(firstId); // same row
        second.Token.ShouldNotBe(firstToken); // fresh secret

        db.IcalSubscriptionTokens.Count().ShouldBe(1);
    }

    [Fact]
    public async Task Resolve_Returns_Owner_For_A_Live_Token_And_Null_After_Revoke()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var service = CreateService(db, userId);

        var row = await service.RotateForCurrentUserAsync();

        (await service.ResolveUserIdAsync(row.Token)).ShouldBe(userId);

        await service.RevokeForCurrentUserAsync();

        (await service.ResolveUserIdAsync(row.Token)).ShouldBeNull();
        db.IcalSubscriptionTokens.Count().ShouldBe(0);
    }

    [Fact]
    public async Task Resolve_Stamps_LastUsed_On_First_Fetch()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var service = CreateService(db, Guid.NewGuid());
        var row = await service.RotateForCurrentUserAsync();

        await service.ResolveUserIdAsync(row.Token);

        var reloaded = await service.GetForCurrentUserAsync();
        reloaded!.LastUsedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Resolve_Returns_Null_For_Unknown_Or_Empty_Token()
    {
        await using var db = TestSchedulingDbContextFactory.Create();
        var service = CreateService(db, Guid.NewGuid());

        (await service.ResolveUserIdAsync("nope")).ShouldBeNull();
        (await service.ResolveUserIdAsync("")).ShouldBeNull();
    }
}
