using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

/// <summary>AuthorUserId is NOT a client-supplied field — the handler resolves it from
/// <c>ICurrentUser</c> (see Chat's CreateChannelCommandHandler for the same pattern).</summary>
public sealed record AddStudentNoteCommand(Guid StudentId, string Text) : ICommand<Guid>;
