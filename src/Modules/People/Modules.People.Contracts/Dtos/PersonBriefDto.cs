namespace FSH.Modules.People.Contracts.Dtos;

/// <summary>Just enough to render a name in a list — used by <c>IPeopleLookupService</c> so
/// StudyGroups/Scheduling/Payments don't N+1 to Students/Teachers for every roster row.</summary>
public sealed record PersonBriefDto(Guid Id, string DisplayName, Guid? AvatarFileId);
