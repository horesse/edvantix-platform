using FSH.Framework.Core.Domain;
using FSH.Modules.People.Contracts.Dtos;

namespace FSH.Modules.People.Domain;

/// <summary>
/// A student (learner). Login is optional — <see cref="UserId"/> is nullable by design
/// (see ADR-003 / docs/02 Модули/People.md): a child can exist in the system, get scheduled
/// and invoiced, without ever having an account.
/// <para>
/// Owns <see cref="GuardianLinks"/> (the "exactly one primary payer" invariant lives here,
/// same shape as <c>Product.Images</c>/<c>IsThumbnail</c> in Catalog) and <see cref="Notes"/>.
/// </para>
/// </summary>
public sealed class Student : AggregateRoot<Guid>, ISoftDeletable
{
    public string LastName { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string? MiddleName { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public string Phone { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? UserId { get; private set; }
    public StudentStatus Status { get; private set; }
    public string? Source { get; private set; }
    public Guid? AvatarFileId { get; private set; }
    public string ManagerUserId { get; private set; } = default!;

    /// <summary>
    /// When the student first enrolled. Set once at creation and never overwritten by later
    /// status transitions — pausing/archiving/reactivating does not reset enrollment history.
    /// </summary>
    public DateTimeOffset EnrolledAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    // EF populates these via the navigation properties; aggregate methods mutate through the
    // private lists so the "exactly one primary payer" invariant holds (see AddGuardianLink).
    private readonly List<StudentGuardian> _guardianLinks = [];
    public IReadOnlyList<StudentGuardian> GuardianLinks => _guardianLinks;

    private readonly List<StudentNote> _notes = [];
    public IReadOnlyList<StudentNote> Notes => _notes;

    /// <summary>Computed, not stored (see docs/04 Задачи/Открытые вопросы.md — People/ФИО).</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{LastName} {FirstName}"
        : $"{LastName} {FirstName} {MiddleName}";

    private Student() { }

    public static Student Create(
        string lastName,
        string firstName,
        string? middleName,
        DateOnly birthDate,
        string phone,
        string email,
        string managerUserId,
        string? source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(managerUserId);

        var now = DateTimeOffset.UtcNow;
        return new Student
        {
            Id = Guid.CreateVersion7(),
            LastName = lastName.Trim(),
            FirstName = firstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
            BirthDate = birthDate,
            Phone = phone.Trim(),
            Email = email.Trim(),
            ManagerUserId = managerUserId,
            Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim(),
            Status = StudentStatus.Lead,
            EnrolledAtUtc = now,
            CreatedAtUtc = now,
        };
    }

    public void Update(
        string lastName,
        string firstName,
        string? middleName,
        DateOnly birthDate,
        string phone,
        string email,
        string managerUserId,
        string? source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(managerUserId);

        LastName = lastName.Trim();
        FirstName = firstName.Trim();
        MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        BirthDate = birthDate;
        Phone = phone.Trim();
        Email = email.Trim();
        ManagerUserId = managerUserId;
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Undoes a soft delete (trash restore) — distinct from <see cref="Reactivate"/>,
    /// which moves an Archived student back to Active. This just clears <see cref="IsDeleted"/>.</summary>
    public void Restore()
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetAvatar(Guid? avatarFileId)
    {
        AvatarFileId = avatarFileId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void LinkUser(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        UserId = userId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void UnlinkUser()
    {
        UserId = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    // ─── Lifecycle: Lead → Active → Paused → Archived, restore back to Active ──────────────

    /// <summary>
    /// Moves the student to <paramref name="newStatus"/> if the transition is on the allowed
    /// map. Throws <see cref="InvalidOperationException"/> otherwise — callers (command
    /// handlers) translate that into a 409/400.
    /// </summary>
    public void ChangeStatus(StudentStatus newStatus)
    {
        if (newStatus == Status)
        {
            return;
        }

        bool allowed = (Status, newStatus) switch
        {
            (StudentStatus.Lead, StudentStatus.Active) => true,
            (StudentStatus.Active, StudentStatus.Paused) => true,
            (StudentStatus.Active, StudentStatus.Archived) => true,
            (StudentStatus.Paused, StudentStatus.Active) => true,
            (StudentStatus.Paused, StudentStatus.Archived) => true,
            (StudentStatus.Archived, StudentStatus.Active) => true,
            _ => false,
        };

        if (!allowed)
        {
            throw new InvalidOperationException(
                $"Cannot transition student status from {Status} to {newStatus}.");
        }

        Status = newStatus;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Archive() => ChangeStatus(StudentStatus.Archived);

    public void Reactivate() => ChangeStatus(StudentStatus.Active);

    // ─── Guardian links ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Links a guardian to this student. When <paramref name="isPrimaryPayer"/> is true, the
    /// previous primary payer (if any) is demoted first — exactly one primary payer at a time,
    /// enforced here rather than a DB constraint (same reasoning as Catalog's single-thumbnail:
    /// a partial unique index can't be made deferrable enough for demote-then-promote in one tx).
    /// </summary>
    public StudentGuardian AddGuardianLink(Guid guardianId, string relation, bool isPrimaryPayer)
    {
        if (guardianId == Guid.Empty)
        {
            throw new ArgumentException("GuardianId is required.", nameof(guardianId));
        }
        if (_guardianLinks.Any(g => !g.IsDeleted && g.GuardianId == guardianId))
        {
            throw new InvalidOperationException($"Guardian {guardianId} is already linked to this student.");
        }

        if (isPrimaryPayer)
        {
            DemotePrimaryPayer();
        }

        var link = StudentGuardian.Create(Id, guardianId, relation, isPrimaryPayer);
        _guardianLinks.Add(link);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return link;
    }

    public void RemoveGuardianLink(Guid guardianId)
    {
        var link = FindGuardianLink(guardianId);
        _guardianLinks.Remove(link);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetPrimaryPayer(Guid guardianId)
    {
        var link = FindGuardianLink(guardianId);
        if (link.IsPrimaryPayer)
        {
            return;
        }

        DemotePrimaryPayer();
        link.MarkPrimaryPayer(true);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private StudentGuardian FindGuardianLink(Guid guardianId) =>
        _guardianLinks.FirstOrDefault(g => !g.IsDeleted && g.GuardianId == guardianId)
            ?? throw new InvalidOperationException($"Guardian {guardianId} is not linked to this student.");

    private void DemotePrimaryPayer()
    {
        var current = _guardianLinks.FirstOrDefault(g => !g.IsDeleted && g.IsPrimaryPayer);
        current?.MarkPrimaryPayer(false);
    }

    // ─── Notes (Students.ViewNotes only — see docs/02 Модули/People.md) ────────────────────

    public StudentNote AddNote(string text, string authorUserId)
    {
        var note = StudentNote.Create(Id, text, authorUserId);
        _notes.Add(note);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return note;
    }
}
