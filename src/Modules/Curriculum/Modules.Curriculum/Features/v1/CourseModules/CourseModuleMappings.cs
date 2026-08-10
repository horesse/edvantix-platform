using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Domain;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules;

internal static class CourseModuleMappings
{
    public static CourseModuleDto ToDto(this CourseModule module) => new(
        module.Id, module.CourseId, module.Title, module.Description, module.SortOrder);
}
