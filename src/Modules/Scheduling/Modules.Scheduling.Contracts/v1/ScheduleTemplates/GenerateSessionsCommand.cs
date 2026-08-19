using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;

/// <summary>Applies the template — same computation as <see cref="PreviewGenerationQuery"/>, but
/// persists the non-conflicting occurrences. No <c>Force</c> flag: unlike manual session creation,
/// the generator always skips conflicts and reports them (docs/02 Модули/Scheduling.md →
/// "Конфликты").</summary>
public sealed record GenerateSessionsCommand(Guid ScheduleTemplateId, int? HorizonWeeks) : ICommand<GenerationResultDto>;
