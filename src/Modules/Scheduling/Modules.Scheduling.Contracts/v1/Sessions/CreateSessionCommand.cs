using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Sessions;

/// <summary><c>Force</c> bypasses a resource conflict (409) — e.g. a legitimate substitute-teacher
/// double-booking. See docs/02 Модули/Scheduling.md → "Конфликты".</summary>
public sealed record CreateSessionCommand(
    Guid StudyGroupId,
    Guid? LessonId,
    Guid TeacherId,
    Guid? RoomId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string? Topic,
    string? MeetingUrl,
    bool Force) : ICommand<Guid>;
