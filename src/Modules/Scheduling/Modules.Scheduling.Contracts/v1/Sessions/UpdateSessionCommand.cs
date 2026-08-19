using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Sessions;

public sealed record UpdateSessionCommand(
    Guid SessionId,
    Guid? LessonId,
    Guid TeacherId,
    Guid? RoomId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string? Topic,
    string? MeetingUrl,
    string? TeacherComment,
    bool Force) : ICommand<Unit>;
