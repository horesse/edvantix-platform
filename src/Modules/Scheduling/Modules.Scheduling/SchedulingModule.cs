using Asp.Versioning;
using FSH.Framework.Eventing;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

// Order 620 — after StudyGroups (610): a scheduled session belongs to a study group and (through
// its optional LessonId) a curriculum lesson, both of which must already be resolvable when this
// module's handlers run. See docs/01 Архитектура/Карта модулей.md.
[assembly: FshModule(typeof(FSH.Modules.Scheduling.SchedulingModule), 620)]

namespace FSH.Modules.Scheduling;

public sealed class SchedulingModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(SchedulingPermissions.All);

        builder.Services.AddHeroDbContext<SchedulingDbContext>();
        builder.Services.AddScoped<IDbInitializer, SchedulingDbInitializer>();

        // Outbox/Inbox for SchedulingDbContext — publishes SessionScheduled/Cancelled/Rescheduled/
        // Held, AttendanceMarked (added in step 10 of the implementation plan). AddEventingCore() is
        // NOT called here: IdentityModule already registers it (bus + OutboxDispatcherHostedService)
        // — see People/Curriculum/StudyGroups for the same note on why calling it twice would start
        // a second hosted dispatcher. IOutboxStore/IInboxStore are keyed by typeof(SchedulingDbContext)
        // — see .agents/rules/eventing.md.
        builder.Services.AddEventingForDbContext<SchedulingDbContext>();

        // Scheduling subscribes to StudyGroups/People integration events (StudyGroupActivated/
        // Finished, StudentEnrolled/Unenrolled, TeacherDeactivated) — added in step 10, see
        // IntegrationEventHandlers/.
        builder.Services.AddIntegrationEventHandlers(typeof(SchedulingModule).Assembly);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<SchedulingDbContext>(
                name: "db:scheduling",
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

        // Flat resource routing (no "/scheduling" segment prefix beyond the resource name itself)
        // — same convention as People/Curriculum/StudyGroups.
        var group = endpoints.MapGroup("api/v{version:apiVersion}")
            .WithTags("Scheduling")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // Endpoints are wired feature-by-feature starting step 3 of the implementation plan — see
        // docs/04 Задачи/Задачи · Новые модули.md → Scheduling.
        _ = group;
    }
}
