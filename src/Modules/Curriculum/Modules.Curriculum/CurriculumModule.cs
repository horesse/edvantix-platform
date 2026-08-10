using Asp.Versioning;
using FSH.Framework.Eventing;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Curriculum.Authorization;
using FSH.Modules.Curriculum.Contracts;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Features.v1.CourseModules.CreateCourseModule;
using FSH.Modules.Curriculum.Features.v1.CourseModules.DeleteCourseModule;
using FSH.Modules.Curriculum.Features.v1.CourseModules.ReorderCourseModules;
using FSH.Modules.Curriculum.Features.v1.CourseModules.UpdateCourseModule;
using FSH.Modules.Curriculum.Features.v1.Courses.ArchiveCourse;
using FSH.Modules.Curriculum.Features.v1.Courses.CreateCourse;
using FSH.Modules.Curriculum.Features.v1.Courses.DeleteCourse;
using FSH.Modules.Curriculum.Features.v1.Courses.DuplicateCourse;
using FSH.Modules.Curriculum.Features.v1.Courses.GetCourseById;
using FSH.Modules.Curriculum.Features.v1.Courses.ListTrashedCourses;
using FSH.Modules.Curriculum.Features.v1.Courses.PublishCourse;
using FSH.Modules.Curriculum.Features.v1.Courses.RestoreCourse;
using FSH.Modules.Curriculum.Features.v1.Courses.SearchCourses;
using FSH.Modules.Curriculum.Features.v1.Courses.UpdateCourse;
using FSH.Modules.Curriculum.Features.v1.LessonMaterials.AddLessonMaterial;
using FSH.Modules.Curriculum.Features.v1.LessonMaterials.GetLessonMaterials;
using FSH.Modules.Curriculum.Features.v1.LessonMaterials.RemoveLessonMaterial;
using FSH.Modules.Curriculum.Features.v1.LessonMaterials.ReorderLessonMaterials;
using FSH.Modules.Curriculum.Features.v1.Lessons.CreateLesson;
using FSH.Modules.Curriculum.Features.v1.Lessons.DeleteLesson;
using FSH.Modules.Curriculum.Features.v1.Lessons.GetLessonById;
using FSH.Modules.Curriculum.Features.v1.Lessons.ReorderLessons;
using FSH.Modules.Curriculum.Features.v1.Lessons.UpdateLesson;
using FSH.Modules.Curriculum.Features.v1.Subjects.CreateSubject;
using FSH.Modules.Curriculum.Features.v1.Subjects.DeleteSubject;
using FSH.Modules.Curriculum.Features.v1.Subjects.GetSubjectTree;
using FSH.Modules.Curriculum.Features.v1.Subjects.ReorderSubjects;
using FSH.Modules.Curriculum.Features.v1.Subjects.UpdateSubject;
using FSH.Modules.Curriculum.Services;
using FSH.Modules.Files.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

// Order 600 — the slot freed by Catalog's removal (ADR-002). Curriculum depends on nothing new
// at runtime beyond Identity/Multitenancy/Files, so it loads right after People (550) and before
// StudyGroups (610), which needs a published course to exist. See docs/01 Архитектура/Карта модулей.md.
[assembly: FshModule(typeof(FSH.Modules.Curriculum.CurriculumModule), 600)]

namespace FSH.Modules.Curriculum;

public sealed class CurriculumModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(CurriculumPermissions.All);

        builder.Services.AddHeroDbContext<CurriculumDbContext>();
        builder.Services.AddScoped<IDbInitializer, CurriculumDbInitializer>();
        builder.Services.AddScoped<ICourseQueryService, CourseQueryService>();

        // OwnerType=LessonMaterial policy for Files module attachments (lesson materials).
        builder.Services.AddScoped<IFileAccessPolicy, LessonMaterialAccessPolicy>();

        // Outbox/Inbox stores for CurriculumDbContext — Curriculum publishes CoursePublished/
        // CourseArchived/LessonMaterialAdded. AddEventingCore() is NOT called here: IdentityModule
        // already registers it (bus + OutboxDispatcherHostedService) — see People's identical note.
        builder.Services.AddEventingForDbContext<CurriculumDbContext>();

        // Curriculum has no integration event subscriptions (nothing upstream to react to yet),
        // so AddIntegrationEventHandlers(...) is intentionally not called.

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<CurriculumDbContext>(
                name: "db:curriculum",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        // No custom middleware needed
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        // Flat resource routing, no "/curriculum" segment — same convention as People.
        var group = endpoints.MapGroup("api/v{version:apiVersion}")
            .WithTags("Curriculum")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // /subjects/tree before /subjects/{subjectId:guid} so the literal route wins.
        group.MapGetSubjectTreeEndpoint();
        group.MapReorderSubjectsEndpoint();
        group.MapCreateSubjectEndpoint();
        group.MapUpdateSubjectEndpoint();
        group.MapDeleteSubjectEndpoint();

        // /courses/trash before /courses/{courseId:guid}.
        group.MapListTrashedCoursesEndpoint();
        group.MapSearchCoursesEndpoint();
        group.MapCreateCourseEndpoint();
        group.MapUpdateCourseEndpoint();
        group.MapDeleteCourseEndpoint();
        group.MapPublishCourseEndpoint();
        group.MapArchiveCourseEndpoint();
        group.MapDuplicateCourseEndpoint();
        group.MapRestoreCourseEndpoint();
        group.MapGetCourseByIdEndpoint();

        group.MapCreateCourseModuleEndpoint();
        group.MapUpdateCourseModuleEndpoint();
        group.MapDeleteCourseModuleEndpoint();
        group.MapReorderCourseModulesEndpoint();

        group.MapCreateLessonEndpoint();
        group.MapUpdateLessonEndpoint();
        group.MapDeleteLessonEndpoint();
        group.MapReorderLessonsEndpoint();
        group.MapGetLessonByIdEndpoint();

        group.MapAddLessonMaterialEndpoint();
        group.MapRemoveLessonMaterialEndpoint();
        group.MapReorderLessonMaterialsEndpoint();
        group.MapGetLessonMaterialsEndpoint();
    }
}
