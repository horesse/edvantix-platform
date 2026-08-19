using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Domain;

namespace Scheduling.Tests.Domain;

public sealed class SessionTests
{
    private static Session CreateValidSession() => Session.Create(
        studyGroupId: Guid.NewGuid(),
        lessonId: null,
        teacherId: Guid.NewGuid(),
        roomId: null,
        startUtc: new DateTimeOffset(2026, 9, 1, 18, 0, 0, TimeSpan.Zero),
        endUtc: new DateTimeOffset(2026, 9, 1, 19, 0, 0, TimeSpan.Zero),
        topic: " Past Simple ",
        meetingUrl: null);

    #region Create

    [Fact]
    public void Create_Should_SetPlannedStatus_When_Created()
    {
        var session = CreateValidSession();

        session.Status.ShouldBe(SessionStatus.Planned);
    }

    [Fact]
    public void Create_Should_TrimTopic_When_Created()
    {
        var session = CreateValidSession();

        session.Topic.ShouldBe("Past Simple");
    }

    [Fact]
    public void Create_Should_Throw_When_EndBeforeStart()
    {
        Should.Throw<ArgumentException>(() => Session.Create(
            Guid.NewGuid(), null, Guid.NewGuid(), null,
            startUtc: new DateTimeOffset(2026, 9, 1, 18, 0, 0, TimeSpan.Zero),
            endUtc: new DateTimeOffset(2026, 9, 1, 17, 0, 0, TimeSpan.Zero),
            topic: null, meetingUrl: null));
    }

    [Fact]
    public void Create_Should_Throw_When_TeacherIdIsEmpty()
    {
        Should.Throw<ArgumentException>(() => Session.Create(
            Guid.NewGuid(), null, Guid.Empty, null,
            new DateTimeOffset(2026, 9, 1, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 19, 0, 0, TimeSpan.Zero),
            topic: null, meetingUrl: null));
    }

    #endregion

    #region Hold

    [Fact]
    public void Hold_Should_TransitionToHeld_When_Planned()
    {
        var session = CreateValidSession();

        session.Hold();

        session.Status.ShouldBe(SessionStatus.Held);
    }

    [Fact]
    public void Hold_Should_BeIdempotent_When_AlreadyHeld()
    {
        var session = CreateValidSession();
        session.Hold();

        Should.NotThrow(() => session.Hold());
        session.Status.ShouldBe(SessionStatus.Held);
    }

    [Fact]
    public void Hold_Should_Throw_When_AlreadyCancelled()
    {
        var session = CreateValidSession();
        session.Cancel("no reason");

        Should.Throw<CustomException>(() => session.Hold());
    }

    #endregion

    #region Cancel

    [Fact]
    public void Cancel_Should_TransitionToCancelled_And_StoreReason()
    {
        var session = CreateValidSession();

        session.Cancel(" teacher sick ");

        session.Status.ShouldBe(SessionStatus.Cancelled);
        session.CancelReason.ShouldBe("teacher sick");
    }

    [Fact]
    public void Cancel_Should_BeIdempotent_When_AlreadyCancelled()
    {
        var session = CreateValidSession();
        session.Cancel("first reason");

        Should.NotThrow(() => session.Cancel("second reason"));
    }

    [Fact]
    public void Cancel_Should_Throw_When_AlreadyHeld()
    {
        var session = CreateValidSession();
        session.Hold();

        Should.Throw<CustomException>(() => session.Cancel(null));
    }

    #endregion

    #region MarkRescheduled

    [Fact]
    public void MarkRescheduled_Should_TransitionToRescheduled_When_Planned()
    {
        var session = CreateValidSession();

        session.MarkRescheduled();

        session.Status.ShouldBe(SessionStatus.Rescheduled);
    }

    [Fact]
    public void MarkRescheduled_Should_Throw_When_AlreadyHeld()
    {
        var session = CreateValidSession();
        session.Hold();

        Should.Throw<CustomException>(() => session.MarkRescheduled());
    }

    #endregion

    #region Update

    [Fact]
    public void Update_Should_Throw_When_SessionIsCancelled()
    {
        var session = CreateValidSession();
        session.Cancel(null);

        Should.Throw<CustomException>(() => session.Update(
            null, Guid.NewGuid(), null,
            new DateTimeOffset(2026, 9, 2, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 2, 19, 0, 0, TimeSpan.Zero),
            null, null, null));
    }

    #endregion
}
