using Asp.Versioning;
using FSH.Framework.Eventing;
using FSH.Framework.Persistence;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Data;
using FSH.Modules.People.Features.v1.Guardians.CreateGuardian;
using FSH.Modules.People.Features.v1.Guardians.DeleteGuardian;
using FSH.Modules.People.Features.v1.Guardians.GetGuardianById;
using FSH.Modules.People.Features.v1.Guardians.GetGuardianStudents;
using FSH.Modules.People.Features.v1.Guardians.LinkGuardianUser;
using FSH.Modules.People.Features.v1.Guardians.SearchGuardians;
using FSH.Modules.People.Features.v1.Guardians.UnlinkGuardianUser;
using FSH.Modules.People.Features.v1.Guardians.UpdateGuardian;
using FSH.Modules.People.Features.v1.GetMyPeopleScope;
using FSH.Modules.People.Features.v1.Students.AddStudentGuardian;
using FSH.Modules.People.Features.v1.Students.AddStudentNote;
using FSH.Modules.People.Features.v1.Students.ArchiveStudent;
using FSH.Modules.People.Features.v1.Students.DeleteStudentNote;
using FSH.Modules.People.Features.v1.Students.GetStudentNotes;
using FSH.Modules.People.Features.v1.Students.CreateStudent;
using FSH.Modules.People.Features.v1.Students.DeleteStudent;
using FSH.Modules.People.Features.v1.Students.GetStudentById;
using FSH.Modules.People.Features.v1.Students.GetStudentGuardians;
using FSH.Modules.People.Features.v1.Students.ImportStudents;
using FSH.Modules.People.Features.v1.Students.LinkStudentUser;
using FSH.Modules.People.Features.v1.Students.RemoveStudentGuardian;
using FSH.Modules.People.Features.v1.Students.RestoreStudent;
using FSH.Modules.People.Features.v1.Students.SearchStudents;
using FSH.Modules.People.Features.v1.Students.SetPrimaryPayer;
using FSH.Modules.People.Features.v1.Students.UnlinkStudentUser;
using FSH.Modules.People.Features.v1.Students.UpdateStudent;
using FSH.Modules.People.Features.v1.Teachers.ActivateTeacher;
using FSH.Modules.People.Features.v1.Teachers.CreateTeacher;
using FSH.Modules.People.Features.v1.Teachers.DeactivateTeacher;
using FSH.Modules.People.Features.v1.Teachers.DeleteTeacher;
using FSH.Modules.People.Features.v1.Teachers.GetTeacherById;
using FSH.Modules.People.Features.v1.Teachers.LinkTeacherUser;
using FSH.Modules.People.Features.v1.Teachers.SearchTeachers;
using FSH.Modules.People.Features.v1.Teachers.UnlinkTeacherUser;
using FSH.Modules.People.Features.v1.Teachers.UpdateTeacher;
using FSH.Modules.People.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

// Order 550 — after Billing (500), before Catalog (600). People has no subscriptions of its own
// (see docs/02 Модули/People.md — "низовой модуль, ни от кого не зависит"), but StudyGroups/
// Scheduling/Payments load after it and consume PeopleScope + People's integration events.
[assembly: FshModule(typeof(FSH.Modules.People.PeopleModule), 550)]

namespace FSH.Modules.People;

public sealed class PeopleModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(PeoplePermissions.All);

        builder.Services.AddHeroDbContext<PeopleDbContext>();
        builder.Services.AddScoped<IDbInitializer, PeopleDbInitializer>();
        builder.Services.AddScoped<FSH.Modules.People.Contracts.IPeopleScopeResolver, PeopleScopeResolver>();
        builder.Services.AddScoped<FSH.Modules.People.Contracts.IPeopleLookupService, PeopleLookupService>();

        // Quota gauges: live per-tenant counts for the ActiveStudents / ActiveTeachers plan limits
        // (fed into UsageSnapshots and the soft creation block). Mirrors Identity's UserCount gauge.
        builder.Services.AddScoped<IQuotaGaugeProvider, ActiveStudentCountQuotaGaugeProvider>();
        builder.Services.AddScoped<IQuotaGaugeProvider, ActiveTeacherCountQuotaGaugeProvider>();

        // Outbox/Inbox stores for PeopleDbContext — People publishes StudentCreated/StudentStatusChanged/
        // StudentArchived/TeacherDeactivated/GuardianLinkedToStudent. AddEventingCore() is NOT called here:
        // IdentityModule already registers it (bus + OutboxDispatcherHostedService); calling it again would
        // register a second hosted dispatcher polling the same outbox table concurrently.
        builder.Services.AddEventingForDbContext<PeopleDbContext>();

        // People has no integration event subscriptions (leaf module — nothing to wire here),
        // so AddIntegrationEventHandlers(...) is intentionally not called.

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<PeopleDbContext>(
                name: "db:people",
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

        // Flat resource routing (no "/people" segment) — docs/02 Модули/People.md documents
        // /api/v1/students, /api/v1/teachers, /api/v1/guardians directly, same convention used
        // by the other four new modules (Curriculum, StudyGroups, Scheduling, Payments). Only
        // the scope-resolver endpoint (task 9) is under /api/v1/people/me/scope.
        var group = endpoints.MapGroup("api/v{version:apiVersion}")
            .WithTags("People")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        group.MapCreateStudentEndpoint();
        group.MapUpdateStudentEndpoint();
        group.MapDeleteStudentEndpoint();
        group.MapGetStudentByIdEndpoint();
        group.MapSearchStudentsEndpoint();
        group.MapArchiveStudentEndpoint();
        group.MapRestoreStudentEndpoint();
        group.MapAddStudentGuardianEndpoint();
        group.MapRemoveStudentGuardianEndpoint();
        group.MapSetPrimaryPayerEndpoint();
        group.MapGetStudentGuardiansEndpoint();
        group.MapLinkStudentUserEndpoint();
        group.MapUnlinkStudentUserEndpoint();
        group.MapAddStudentNoteEndpoint();
        group.MapDeleteStudentNoteEndpoint();
        group.MapGetStudentNotesEndpoint();
        group.MapImportStudentsEndpoint();

        group.MapCreateTeacherEndpoint();
        group.MapUpdateTeacherEndpoint();
        group.MapDeleteTeacherEndpoint();
        group.MapDeactivateTeacherEndpoint();
        group.MapActivateTeacherEndpoint();
        group.MapGetTeacherByIdEndpoint();
        group.MapSearchTeachersEndpoint();
        group.MapLinkTeacherUserEndpoint();
        group.MapUnlinkTeacherUserEndpoint();

        group.MapCreateGuardianEndpoint();
        group.MapUpdateGuardianEndpoint();
        group.MapDeleteGuardianEndpoint();
        group.MapGetGuardianByIdEndpoint();
        group.MapGetGuardianStudentsEndpoint();
        group.MapSearchGuardiansEndpoint();
        group.MapLinkGuardianUserEndpoint();
        group.MapUnlinkGuardianUserEndpoint();

        group.MapGetMyPeopleScopeEndpoint();
    }
}
