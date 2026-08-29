using FSH.Framework.Core.Domain;

namespace FSH.Modules.Chat.Domain;

/// <summary>
/// Per-school toggles for the direct-message policy. One row per tenant. Only setting so far:
/// whether two students may DM each other (off by default — see docs/02 Модули/Chat.md →
/// «Ограничение личных сообщений»).
/// </summary>
public sealed class ChatDmSettings : AggregateRoot<Guid>
{
    public bool AllowStudentToStudentDm { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private ChatDmSettings() { }

    public static ChatDmSettings Create(bool allowStudentToStudentDm) => new()
    {
        Id = Guid.CreateVersion7(),
        AllowStudentToStudentDm = allowStudentToStudentDm,
        UpdatedAtUtc = DateTime.UtcNow,
    };

    public void Set(bool allowStudentToStudentDm)
    {
        AllowStudentToStudentDm = allowStudentToStudentDm;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
