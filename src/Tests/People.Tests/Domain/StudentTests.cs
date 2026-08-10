using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Domain;

namespace People.Tests.Domain;

public sealed class StudentTests
{
    private static Student CreateValidStudent(string? middleName = "Ivanovna", string managerUserId = "manager-1")
        => Student.Create(
            lastName: " Ivanova ",
            firstName: " Anna ",
            middleName: middleName,
            birthDate: new DateOnly(2012, 5, 1),
            phone: "+15550000",
            email: "anna@example.com",
            managerUserId: managerUserId,
            source: " Website ");

    #region Create

    [Fact]
    public void Create_Should_SetLeadStatus_When_Created()
    {
        Student student = CreateValidStudent();

        student.Status.ShouldBe(StudentStatus.Lead);
    }

    [Fact]
    public void Create_Should_TrimFields_When_Created()
    {
        Student student = CreateValidStudent();

        student.LastName.ShouldBe("Ivanova");
        student.FirstName.ShouldBe("Anna");
        student.Source.ShouldBe("Website");
    }

    [Fact]
    public void Create_Should_SetEnrolledAtUtc_ThatDoesNotChangeOnLaterUpdates()
    {
        Student student = CreateValidStudent();
        DateTimeOffset enrolledAt = student.EnrolledAtUtc;

        student.ChangeStatus(StudentStatus.Active);
        student.ChangeStatus(StudentStatus.Paused);
        student.ChangeStatus(StudentStatus.Active);

        student.EnrolledAtUtc.ShouldBe(enrolledAt);
    }

    [Fact]
    public void DisplayName_Should_IncludeMiddleName_When_Present()
    {
        Student student = CreateValidStudent(middleName: "Ivanovna");

        student.DisplayName.ShouldBe("Ivanova Anna Ivanovna");
    }

    [Fact]
    public void DisplayName_Should_OmitMiddleName_When_Null()
    {
        Student student = CreateValidStudent(middleName: null);

        student.DisplayName.ShouldBe("Ivanova Anna");
    }

    #endregion

    #region ChangeStatus

    [Fact]
    public void ChangeStatus_Should_MoveLeadToActive()
    {
        Student student = CreateValidStudent();

        student.ChangeStatus(StudentStatus.Active);

        student.Status.ShouldBe(StudentStatus.Active);
    }

    [Fact]
    public void ChangeStatus_Should_Throw_When_TransitionIsNotAllowed()
    {
        // Lead → Paused skips the required Active step.
        Student student = CreateValidStudent();

        Should.Throw<InvalidOperationException>(() => student.ChangeStatus(StudentStatus.Paused));
    }

    [Fact]
    public void ChangeStatus_Should_BeNoOp_When_TargetEqualsCurrentStatus()
    {
        Student student = CreateValidStudent();
        DateTimeOffset? updatedBefore = student.UpdatedAtUtc;

        student.ChangeStatus(StudentStatus.Lead);

        student.Status.ShouldBe(StudentStatus.Lead);
        student.UpdatedAtUtc.ShouldBe(updatedBefore);
    }

    [Fact]
    public void Archive_Should_SetArchivedStatus_When_Active()
    {
        Student student = CreateValidStudent();
        student.ChangeStatus(StudentStatus.Active);

        student.Archive();

        student.Status.ShouldBe(StudentStatus.Archived);
    }

    [Fact]
    public void Reactivate_Should_RestoreActiveStatus_When_Archived()
    {
        Student student = CreateValidStudent();
        student.ChangeStatus(StudentStatus.Active);
        student.Archive();

        student.Reactivate();

        student.Status.ShouldBe(StudentStatus.Active);
    }

    #endregion

    #region Guardian links

    [Fact]
    public void AddGuardianLink_Should_AddLink_When_GuardianNotAlreadyLinked()
    {
        Student student = CreateValidStudent();
        Guid guardianId = Guid.NewGuid();

        StudentGuardian link = student.AddGuardianLink(guardianId, "Mother", isPrimaryPayer: true);

        student.GuardianLinks.ShouldContain(link);
        link.IsPrimaryPayer.ShouldBeTrue();
    }

    [Fact]
    public void AddGuardianLink_Should_Throw_When_GuardianAlreadyLinked()
    {
        Student student = CreateValidStudent();
        Guid guardianId = Guid.NewGuid();
        student.AddGuardianLink(guardianId, "Mother", isPrimaryPayer: false);

        Should.Throw<InvalidOperationException>(() => student.AddGuardianLink(guardianId, "Mother", isPrimaryPayer: false));
    }

    [Fact]
    public void AddGuardianLink_Should_DemotePreviousPrimaryPayer_When_NewLinkIsPrimary()
    {
        // The invariant this whole feature exists for: exactly one primary payer at a time.
        Student student = CreateValidStudent();
        StudentGuardian first = student.AddGuardianLink(Guid.NewGuid(), "Mother", isPrimaryPayer: true);

        StudentGuardian second = student.AddGuardianLink(Guid.NewGuid(), "Father", isPrimaryPayer: true);

        first.IsPrimaryPayer.ShouldBeFalse();
        second.IsPrimaryPayer.ShouldBeTrue();
        student.GuardianLinks.Count(g => g.IsPrimaryPayer).ShouldBe(1);
    }

    [Fact]
    public void SetPrimaryPayer_Should_DemoteOldAndPromoteNew()
    {
        Student student = CreateValidStudent();
        Guid motherId = Guid.NewGuid();
        Guid fatherId = Guid.NewGuid();
        student.AddGuardianLink(motherId, "Mother", isPrimaryPayer: true);
        student.AddGuardianLink(fatherId, "Father", isPrimaryPayer: false);

        student.SetPrimaryPayer(fatherId);

        student.GuardianLinks.Single(g => g.GuardianId == motherId).IsPrimaryPayer.ShouldBeFalse();
        student.GuardianLinks.Single(g => g.GuardianId == fatherId).IsPrimaryPayer.ShouldBeTrue();
    }

    [Fact]
    public void SetPrimaryPayer_Should_Throw_When_GuardianNotLinked()
    {
        Student student = CreateValidStudent();

        Should.Throw<InvalidOperationException>(() => student.SetPrimaryPayer(Guid.NewGuid()));
    }

    [Fact]
    public void RemoveGuardianLink_Should_RemoveFromCollection_When_Linked()
    {
        Student student = CreateValidStudent();
        Guid guardianId = Guid.NewGuid();
        student.AddGuardianLink(guardianId, "Mother", isPrimaryPayer: false);

        student.RemoveGuardianLink(guardianId);

        student.GuardianLinks.ShouldNotContain(g => g.GuardianId == guardianId);
    }

    [Fact]
    public void RemoveGuardianLink_Should_Throw_When_NotLinked()
    {
        Student student = CreateValidStudent();

        Should.Throw<InvalidOperationException>(() => student.RemoveGuardianLink(Guid.NewGuid()));
    }

    #endregion

    #region Notes

    [Fact]
    public void AddNote_Should_AddToCollection_When_Called()
    {
        Student student = CreateValidStudent();

        StudentNote note = student.AddNote("Called about tuition.", "manager-1");

        student.Notes.ShouldContain(note);
        note.AuthorUserId.ShouldBe("manager-1");
    }

    #endregion

    #region Account linking

    [Fact]
    public void LinkUser_Should_SetUserId_When_Called()
    {
        Student student = CreateValidStudent();

        student.LinkUser("user-42");

        student.UserId.ShouldBe("user-42");
    }

    [Fact]
    public void UnlinkUser_Should_ClearUserId_When_Called()
    {
        Student student = CreateValidStudent();
        student.LinkUser("user-42");

        student.UnlinkUser();

        student.UserId.ShouldBeNull();
    }

    #endregion

    #region Soft delete

    [Fact]
    public void Restore_Should_BeNoOp_When_NotDeleted()
    {
        Student student = CreateValidStudent();

        student.Restore();

        student.IsDeleted.ShouldBeFalse();
    }

    #endregion
}
