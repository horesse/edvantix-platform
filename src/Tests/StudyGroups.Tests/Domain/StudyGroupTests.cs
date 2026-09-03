using FSH.Framework.Core.Exceptions;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Domain;

namespace StudyGroups.Tests.Domain;

public sealed class StudyGroupTests
{
    private static StudyGroup CreateValidGroup(int capacity = 2) => StudyGroup.Create(
        code: " A1-01 ",
        name: " English A1, group 1 ",
        courseId: Guid.NewGuid(),
        primaryTeacherId: Guid.NewGuid(),
        format: GroupFormat.Online,
        capacity: capacity,
        startDate: new DateOnly(2026, 9, 1),
        endDate: null,
        meetingUrl: null,
        roomId: null,
        notes: null);

    #region Create

    [Fact]
    public void Create_Should_SetFormingStatus_When_Created()
    {
        StudyGroup group = CreateValidGroup();

        group.Status.ShouldBe(StudyGroupStatus.Forming);
    }

    [Fact]
    public void Create_Should_TrimCodeAndName_When_Created()
    {
        StudyGroup group = CreateValidGroup();

        group.Code.ShouldBe("A1-01");
        group.Name.ShouldBe("English A1, group 1");
    }

    [Fact]
    public void Create_Should_Throw_When_CapacityIsLessThanOne()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => StudyGroup.Create(
            "A1-01", "Group", Guid.NewGuid(), Guid.NewGuid(), GroupFormat.Online,
            capacity: 0, startDate: new DateOnly(2026, 9, 1), endDate: null,
            meetingUrl: null, roomId: null, notes: null));
    }

    [Fact]
    public void Create_Should_Throw_When_CourseIdIsEmpty()
    {
        Should.Throw<ArgumentException>(() => StudyGroup.Create(
            "A1-01", "Group", Guid.Empty, Guid.NewGuid(), GroupFormat.Online,
            capacity: 1, startDate: new DateOnly(2026, 9, 1), endDate: null,
            meetingUrl: null, roomId: null, notes: null));
    }

    #endregion

    #region Enroll / Unenroll

    [Fact]
    public void Enroll_Should_AddActiveEnrollment_When_UnderCapacity()
    {
        StudyGroup group = CreateValidGroup();

        var enrollment = group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), tariffId: null, discountPercent: 0);

        enrollment.Status.ShouldBe(EnrollmentStatus.Active);
        group.ActiveEnrollmentCount.ShouldBe(1);
    }

    [Fact]
    public void Enroll_Should_Throw_When_AtCapacity()
    {
        StudyGroup group = CreateValidGroup(capacity: 1);
        group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);

        Should.Throw<CustomException>(() =>
            group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0));
    }

    [Fact]
    public void Enroll_Should_Throw_When_StudentAlreadyActivelyEnrolled()
    {
        StudyGroup group = CreateValidGroup();
        var studentId = Guid.NewGuid();
        group.Enroll(studentId, new DateOnly(2026, 9, 1), null, 0);

        Should.Throw<CustomException>(() =>
            group.Enroll(studentId, new DateOnly(2026, 9, 1), null, 0));
    }

    [Fact]
    public void Enroll_Should_Allow_ReEnrollment_When_PreviousEnrollmentLeft()
    {
        StudyGroup group = CreateValidGroup();
        var studentId = Guid.NewGuid();
        var first = group.Enroll(studentId, new DateOnly(2026, 9, 1), null, 0);
        group.Unenroll(first.Id, new DateOnly(2026, 9, 10), "moved away");

        var second = group.Enroll(studentId, new DateOnly(2026, 10, 1), null, 0);

        second.Id.ShouldNotBe(first.Id);
        second.Status.ShouldBe(EnrollmentStatus.Active);
        group.ActiveEnrollmentCount.ShouldBe(1);
    }

    [Fact]
    public void Unenroll_Should_SetLeftStatus_And_LeftOn()
    {
        StudyGroup group = CreateValidGroup();
        var enrollment = group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);

        group.Unenroll(enrollment.Id, new DateOnly(2026, 9, 15), "left the school");

        group.Enrollments.Single().Status.ShouldBe(EnrollmentStatus.Left);
        group.Enrollments.Single().LeftOn.ShouldBe(new DateOnly(2026, 9, 15));
        group.ActiveEnrollmentCount.ShouldBe(0);
    }

    [Fact]
    public void ChangeEnrollmentTariff_Should_UpdateTariffAndDiscount_When_Active()
    {
        StudyGroup group = CreateValidGroup();
        var enrollment = group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), tariffId: Guid.NewGuid(), discountPercent: 5);
        var newTariffId = Guid.NewGuid();

        group.ChangeEnrollmentTariff(enrollment.Id, newTariffId, discountPercent: 15);

        enrollment.TariffId.ShouldBe(newTariffId);
        enrollment.DiscountPercent.ShouldBe(15);
    }

    [Fact]
    public void ChangeEnrollmentTariff_Should_AllowClearingTariff_When_Paused()
    {
        StudyGroup group = CreateValidGroup();
        var enrollment = group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), tariffId: Guid.NewGuid(), discountPercent: 0);
        group.PauseEnrollment(enrollment.Id);

        group.ChangeEnrollmentTariff(enrollment.Id, tariffId: null, discountPercent: 0);

        enrollment.TariffId.ShouldBeNull();
    }

    [Fact]
    public void ChangeEnrollmentTariff_Should_Throw_When_EnrollmentLeft()
    {
        StudyGroup group = CreateValidGroup();
        var enrollment = group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);
        group.Unenroll(enrollment.Id, new DateOnly(2026, 9, 10), "left");

        Should.Throw<CustomException>(() =>
            group.ChangeEnrollmentTariff(enrollment.Id, Guid.NewGuid(), 0));
    }

    [Fact]
    public void ChangeEnrollmentTariff_Should_Throw_When_DiscountOutOfRange()
    {
        StudyGroup group = CreateValidGroup();
        var enrollment = group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            group.ChangeEnrollmentTariff(enrollment.Id, Guid.NewGuid(), discountPercent: 150));
    }

    [Fact]
    public void ChangeEnrollmentTariff_Should_Throw_When_GroupFinished()
    {
        StudyGroup group = CreateValidGroup();
        var enrollment = group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);
        group.Activate();
        group.Finish(new DateOnly(2026, 12, 1));

        Should.Throw<CustomException>(() =>
            group.ChangeEnrollmentTariff(enrollment.Id, Guid.NewGuid(), 0));
    }

    [Fact]
    public void PauseEnrollment_Then_ResumeEnrollment_Should_RoundTrip_To_Active()
    {
        StudyGroup group = CreateValidGroup();
        var enrollment = group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);

        group.PauseEnrollment(enrollment.Id);
        group.Enrollments.Single().Status.ShouldBe(EnrollmentStatus.Paused);
        group.ActiveEnrollmentCount.ShouldBe(1, "a paused enrollment still occupies a roster slot");

        group.ResumeEnrollment(enrollment.Id);
        group.Enrollments.Single().Status.ShouldBe(EnrollmentStatus.Active);
    }

    #endregion

    #region Lifecycle

    [Fact]
    public void Activate_Should_Throw_When_NoEnrollments()
    {
        StudyGroup group = CreateValidGroup();

        Should.Throw<CustomException>(() => group.Activate());
    }

    [Fact]
    public void Activate_Should_SetActiveStatus_When_HasAtLeastOneEnrollment()
    {
        StudyGroup group = CreateValidGroup();
        group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);

        group.Activate();

        group.Status.ShouldBe(StudyGroupStatus.Active);
    }

    [Fact]
    public void Finish_Should_Throw_When_NotActive()
    {
        StudyGroup group = CreateValidGroup();

        Should.Throw<CustomException>(() => group.Finish(new DateOnly(2026, 12, 1)));
    }

    [Fact]
    public void Finish_Should_CompleteActiveAndPausedEnrollments_And_FreezeRoster()
    {
        StudyGroup group = CreateValidGroup(capacity: 3);
        var active = group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);
        var toPause = group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);
        group.PauseEnrollment(toPause.Id);
        group.Activate();

        group.Finish(new DateOnly(2026, 12, 20));

        group.Status.ShouldBe(StudyGroupStatus.Finished);
        group.Enrollments.Single(e => e.Id == active.Id).Status.ShouldBe(EnrollmentStatus.Completed);
        group.Enrollments.Single(e => e.Id == toPause.Id).Status.ShouldBe(EnrollmentStatus.Completed);
        Should.Throw<CustomException>(() => group.Enroll(Guid.NewGuid(), new DateOnly(2026, 12, 21), null, 0));
    }

    [Fact]
    public void Finish_Should_SetEndDate_When_NotAlreadySet()
    {
        StudyGroup group = CreateValidGroup();
        group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);
        group.Activate();

        group.Finish(new DateOnly(2026, 12, 20));

        group.EndDate.ShouldBe(new DateOnly(2026, 12, 20));
    }

    [Theory]
    [InlineData(StudyGroupStatus.Forming)]
    [InlineData(StudyGroupStatus.Active)]
    public void Cancel_Should_SetCancelledStatus_From_FormingOrActive(StudyGroupStatus fromStatus)
    {
        StudyGroup group = CreateValidGroup();
        if (fromStatus == StudyGroupStatus.Active)
        {
            group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);
            group.Activate();
        }

        group.Cancel("not enough students");

        group.Status.ShouldBe(StudyGroupStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Should_Throw_When_AlreadyFinished()
    {
        StudyGroup group = CreateValidGroup();
        group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);
        group.Activate();
        group.Finish(new DateOnly(2026, 12, 1));

        Should.Throw<CustomException>(() => group.Cancel("too late"));
    }

    #endregion

    #region Update / capacity invariant

    [Fact]
    public void Update_Should_Throw_When_CapacityBelowActiveEnrollmentCount()
    {
        StudyGroup group = CreateValidGroup(capacity: 2);
        group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);
        group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);

        Should.Throw<CustomException>(() => group.Update(
            "New name", Guid.NewGuid(), GroupFormat.Online, capacity: 1,
            startDate: new DateOnly(2026, 9, 1), endDate: null,
            meetingUrl: null, roomId: null, notes: null));
    }

    [Fact]
    public void Update_Should_Throw_When_GroupIsFinished()
    {
        StudyGroup group = CreateValidGroup();
        group.Enroll(Guid.NewGuid(), new DateOnly(2026, 9, 1), null, 0);
        group.Activate();
        group.Finish(new DateOnly(2026, 12, 1));

        Should.Throw<CustomException>(() => group.Update(
            "New name", Guid.NewGuid(), GroupFormat.Online, capacity: 5,
            startDate: new DateOnly(2026, 9, 1), endDate: null,
            meetingUrl: null, roomId: null, notes: null));
    }

    #endregion

    #region Teachers

    [Fact]
    public void AddTeacher_Should_Throw_When_TeacherAlreadyOnRoster()
    {
        StudyGroup group = CreateValidGroup();
        var teacherId = Guid.NewGuid();
        group.AddTeacher(teacherId, TeacherRole.Assistant);

        Should.Throw<CustomException>(() => group.AddTeacher(teacherId, TeacherRole.Substitute));
    }

    [Fact]
    public void RemoveTeacher_Should_Throw_When_TeacherNotOnRoster()
    {
        StudyGroup group = CreateValidGroup();

        Should.Throw<NotFoundException>(() => group.RemoveTeacher(Guid.NewGuid()));
    }

    [Fact]
    public void RemoveTeacher_Should_RemoveFromRoster_When_Present()
    {
        StudyGroup group = CreateValidGroup();
        var teacherId = Guid.NewGuid();
        group.AddTeacher(teacherId, TeacherRole.Assistant);

        group.RemoveTeacher(teacherId);

        group.Teachers.ShouldBeEmpty();
    }

    #endregion
}
