using FSH.Framework.Core.Domain;
using FSH.Modules.People.Contracts.Dtos;

namespace FSH.Modules.People.Domain;

/// <summary>
/// A teacher. Login is optional (<see cref="UserId"/> nullable) — same reasoning as
/// <see cref="Student"/>, see ADR-003 / docs/02 Модули/People.md.
/// </summary>
public sealed class Teacher : AggregateRoot<Guid>, ISoftDeletable
{
    public string LastName { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string? MiddleName { get; private set; }
    public string Phone { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? UserId { get; private set; }
    public TeacherStatus Status { get; private set; }
    public string? Bio { get; private set; }

    /// <summary>Comma-separated — mirrors <c>WebhookSubscription.EventsCsv</c>: no established
    /// native-array column convention in this codebase, so a CSV column keeps the migration
    /// and config simple. Use <see cref="GetSpecializations"/> to read it back as an array.</summary>
    public string SpecializationsCsv { get; private set; } = string.Empty;

    public decimal? HourlyRate { get; private set; }
    public Guid? AvatarFileId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public string DisplayName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{LastName} {FirstName}"
        : $"{LastName} {FirstName} {MiddleName}";

    private Teacher() { }

    public static Teacher Create(
        string lastName,
        string firstName,
        string? middleName,
        string phone,
        string email,
        string? bio,
        string[]? specializations,
        decimal? hourlyRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        if (hourlyRate is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hourlyRate), "Hourly rate cannot be negative.");
        }

        return new Teacher
        {
            Id = Guid.CreateVersion7(),
            LastName = lastName.Trim(),
            FirstName = firstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
            Phone = phone.Trim(),
            Email = email.Trim(),
            Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim(),
            SpecializationsCsv = string.Join(',', specializations ?? []),
            HourlyRate = hourlyRate,
            Status = TeacherStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public string[] GetSpecializations() =>
        SpecializationsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public void Update(
        string lastName,
        string firstName,
        string? middleName,
        string phone,
        string email,
        string? bio,
        string[]? specializations,
        decimal? hourlyRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        if (hourlyRate is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hourlyRate), "Hourly rate cannot be negative.");
        }

        LastName = lastName.Trim();
        FirstName = firstName.Trim();
        MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        Phone = phone.Trim();
        Email = email.Trim();
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        SpecializationsCsv = string.Join(',', specializations ?? []);
        HourlyRate = hourlyRate;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

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

    public void Deactivate()
    {
        Status = TeacherStatus.Inactive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        Status = TeacherStatus.Active;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
