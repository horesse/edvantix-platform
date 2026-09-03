using Asp.Versioning;
using FSH.Framework.Eventing;
using FSH.Framework.Persistence;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.StudyGroups.Contracts;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Data;
using FSH.Modules.StudyGroups.Features.v1.Enrollments.ChangeEnrollmentTariff;
using FSH.Modules.StudyGroups.Features.v1.Enrollments.EnrollStudents;
using FSH.Modules.StudyGroups.Features.v1.Enrollments.GetGroupEnrollments;
using FSH.Modules.StudyGroups.Features.v1.Enrollments.GetStudentEnrollments;
using FSH.Modules.StudyGroups.Features.v1.Enrollments.PauseEnrollment;
using FSH.Modules.StudyGroups.Features.v1.Enrollments.ResumeEnrollment;
using FSH.Modules.StudyGroups.Features.v1.Enrollments.TransferEnrollment;
using FSH.Modules.StudyGroups.Features.v1.Enrollments.UnenrollStudent;
using FSH.Modules.StudyGroups.Features.v1.StudyGroups.ActivateStudyGroup;
using FSH.Modules.StudyGroups.Features.v1.StudyGroups.CancelStudyGroup;
using FSH.Modules.StudyGroups.Features.v1.StudyGroups.CreateStudyGroup;
using FSH.Modules.StudyGroups.Features.v1.StudyGroups.DeleteStudyGroup;
using FSH.Modules.StudyGroups.Features.v1.StudyGroups.FinishStudyGroup;
using FSH.Modules.StudyGroups.Features.v1.StudyGroups.GetMyStudyGroups;
using FSH.Modules.StudyGroups.Features.v1.StudyGroups.GetStudyGroupById;
using FSH.Modules.StudyGroups.Features.v1.StudyGroups.SearchStudyGroups;
using FSH.Modules.StudyGroups.Features.v1.StudyGroups.UpdateStudyGroup;
using FSH.Modules.StudyGroups.Features.v1.Teachers.AddGroupTeacher;
using FSH.Modules.StudyGroups.Features.v1.Teachers.RemoveGroupTeacher;
using FSH.Modules.StudyGroups.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

// Order 610 — after People (550) and Curriculum (600): a study group references a published course
// and a teacher/roster of students, both of which must already be resolvable when this module's
// handlers run (see docs/01 Архитектура/Карта модулей.md).
[assembly: FshModule(typeof(FSH.Modules.StudyGroups.StudyGroupsModule), 610)]

namespace FSH.Modules.StudyGroups;

public sealed class StudyGroupsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(StudyGroupsPermissions.All);

        builder.Services.AddHeroDbContext<StudyGroupsDbContext>();
        builder.Services.AddScoped<IDbInitializer, StudyGroupsDbInitializer>();
        builder.Services.AddScoped<IStudyGroupQueryService, StudyGroupQueryService>();

        // Quota gauge: live per-tenant count of forming/active groups for the StudyGroups plan
        // limit (UsageSnapshots + soft creation block). Mirrors Identity's UserCount gauge.
        builder.Services.AddScoped<IQuotaGaugeProvider, StudyGroupCountQuotaGaugeProvider>();

        // Outbox/Inbox for StudyGroupsDbContext — publishes StudyGroupCreated/Activated/Finished,
        // StudentEnrolled/Unenrolled. AddEventingCore() is NOT called here: IdentityModule already
        // registers it (bus + OutboxDispatcherHostedService) — see People/Curriculum for the same
        // note on why calling it twice would start a second hosted dispatcher.
        builder.Services.AddEventingForDbContext<StudyGroupsDbContext>();

        // StudyGroups subscribes to People/Curriculum integration events (StudentArchived,
        // TeacherDeactivated, CourseArchived) — see IntegrationEventHandlers/.
        builder.Services.AddIntegrationEventHandlers(typeof(StudyGroupsModule).Assembly);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<StudyGroupsDbContext>(
                name: "db:study-groups",
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

        // Flat resource routing (no "/study-groups" segment prefix beyond the resource name itself)
        // — same convention as People/Curriculum. /study-groups/my is registered alongside
        // /study-groups/{id}; the :guid route constraint on {id} keeps "my" from being swallowed.
        var group = endpoints.MapGroup("api/v{version:apiVersion}")
            .WithTags("StudyGroups")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        group.MapCreateStudyGroupEndpoint();
        group.MapUpdateStudyGroupEndpoint();
        group.MapDeleteStudyGroupEndpoint();
        group.MapGetStudyGroupByIdEndpoint();
        group.MapSearchStudyGroupsEndpoint();
        group.MapGetMyStudyGroupsEndpoint();
        group.MapActivateStudyGroupEndpoint();
        group.MapFinishStudyGroupEndpoint();
        group.MapCancelStudyGroupEndpoint();

        group.MapGetGroupEnrollmentsEndpoint();
        group.MapEnrollStudentsEndpoint();
        group.MapUnenrollStudentEndpoint();
        group.MapTransferEnrollmentEndpoint();
        group.MapChangeEnrollmentTariffEndpoint();
        group.MapPauseEnrollmentEndpoint();
        group.MapResumeEnrollmentEndpoint();
        group.MapGetStudentEnrollmentsEndpoint();

        group.MapAddGroupTeacherEndpoint();
        group.MapRemoveGroupTeacherEndpoint();
    }
}
