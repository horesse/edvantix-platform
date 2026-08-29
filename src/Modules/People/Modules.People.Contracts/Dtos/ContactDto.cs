namespace FSH.Modules.People.Contracts.Dtos;

/// <summary>Which person a <see cref="ContactDto"/> represents relative to a student.</summary>
public enum ContactRole
{
    Student = 0,
    Guardian = 1,
    PrimaryPayerGuardian = 2,
    Teacher = 3,
}

/// <summary>
/// A notification/chat target resolved from People. <see cref="UserId"/> is null when the person has
/// no account (in-app delivery is impossible; e-mail may still be, since Student/Guardian/Teacher
/// carry an <see cref="Email"/> directly). Consumed by Notifications (recipient fan-out) and Chat
/// (study-group channel membership).
/// </summary>
public sealed record ContactDto(
    string? UserId,
    string? Email,
    string DisplayName,
    ContactRole Role);

/// <summary>
/// The student plus everyone who should hear about things that happen to them: the student's own
/// account (when linked) and each active guardian, with the primary payer flagged
/// (<see cref="ContactRole.PrimaryPayerGuardian"/>).
/// </summary>
public sealed record StudentContactsDto(
    Guid StudentId,
    string StudentDisplayName,
    ContactDto Student,
    IReadOnlyList<ContactDto> Guardians);
