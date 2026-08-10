using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Courses;

/// <summary>Deep-clones a course: the course itself plus every module → lesson → material,
/// as a new <c>Draft</c> course. See docs/04 Задачи/Задачи · Новые модули.md → Curriculum →
/// "Проектные решения" for the exact copy semantics.</summary>
public sealed record DuplicateCourseCommand(Guid CourseId) : ICommand<Guid>;
