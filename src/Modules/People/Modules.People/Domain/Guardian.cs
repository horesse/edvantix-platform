using FSH.Framework.Core.Domain;

namespace FSH.Modules.People.Domain;

/// <summary>
/// A guardian (parent/relative/sponsor) responsible for one or more students — see
/// <see cref="StudentGuardian"/> for the link and the primary-payer flag. Login is optional
/// (<see cref="UserId"/> nullable), same reasoning as <see cref="Student"/>/<see cref="Teacher"/>.
/// </summary>
public sealed class Guardian : AggregateRoot<Guid>, ISoftDeletable
{
    public string LastName { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? UserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public string DisplayName => $"{LastName} {FirstName}";

    private Guardian() { }

    public static Guardian Create(string lastName, string firstName, string phone, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return new Guardian
        {
            Id = Guid.CreateVersion7(),
            LastName = lastName.Trim(),
            FirstName = firstName.Trim(),
            Phone = phone.Trim(),
            Email = email.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string lastName, string firstName, string phone, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        LastName = lastName.Trim();
        FirstName = firstName.Trim();
        Phone = phone.Trim();
        Email = email.Trim();
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
}
