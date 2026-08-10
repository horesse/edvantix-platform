using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Domain;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials;

internal static class LessonMaterialMappings
{
    public static LessonMaterialDto ToDto(this LessonMaterial material) => new(
        material.Id, material.LessonId, material.Kind, material.Title, material.FileId,
        material.Url, material.VisibleToStudents, material.SortOrder);
}
