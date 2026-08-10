using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Subjects;

/// <summary>Sets SortOrder = 0, 1, 2... for the siblings under <paramref name="ParentId"/>
/// (null = top level) in the order supplied. Ids not listed keep their relative order, appended
/// after — same convention as Catalog's <c>ReorderProductImagesCommand</c>.</summary>
public sealed record ReorderSubjectsCommand(
    Guid? ParentId,
    IReadOnlyList<Guid> OrderedSubjectIds) : ICommand<Unit>;
