using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Domain;

namespace Scheduling.Tests.Domain;

public sealed class AttendanceTests
{
    [Fact]
    public void CreateDefault_Should_DefaultToPresent_When_Created()
    {
        var attendance = Attendance.CreateDefault(Guid.NewGuid(), Guid.NewGuid());

        attendance.Status.ShouldBe(AttendanceStatus.Present);
    }

    [Fact]
    public void CreateDefault_Should_Throw_When_SessionIdIsEmpty()
    {
        Should.Throw<ArgumentException>(() => Attendance.CreateDefault(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Mark_Should_UpdateStatusCommentAndMarkedBy()
    {
        var attendance = Attendance.CreateDefault(Guid.NewGuid(), Guid.NewGuid());

        attendance.Mark(AttendanceStatus.Absent, " sick ", "user-1");

        attendance.Status.ShouldBe(AttendanceStatus.Absent);
        attendance.Comment.ShouldBe("sick");
        attendance.MarkedByUserId.ShouldBe("user-1");
    }
}
