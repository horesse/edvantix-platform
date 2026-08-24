using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

// Order 630 — right after Scheduling (620): invoices are accrued from planned/held sessions
// (ISessionPlanQueryService/IAttendanceQueryService) and from group enrollments (StudyGroups, 610),
// so both must already be resolvable when this module's handlers run
// (see docs/01 Архитектура/Карта модулей.md).
[assembly: FshModule(typeof(FSH.Modules.Payments.PaymentsModule), 630)]

namespace FSH.Modules.Payments;

public sealed class PaymentsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(PaymentsPermissions.All);

        builder.Services.AddHeroDbContext<PaymentsDbContext>();
        builder.Services.AddScoped<IDbInitializer, PaymentsDbInitializer>();

        // Outbox/Inbox for PaymentsDbContext, eventing trio, jobs, file access policy and the
        // Mediator handler registrations are wired in as their respective vertical slices land —
        // see docs/04 Задачи/Задачи · Новые модули.md → Payments for the step-by-step log.

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<PaymentsDbContext>(
                name: "db:payments",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        // No custom middleware needed
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Endpoint group (flat routing, same convention as People/Curriculum/StudyGroups/Scheduling)
        // and recurring jobs are wired in once the first feature lands — see the step log in
        // docs/04 Задачи/Задачи · Новые модули.md → Payments. An empty MapGroup here would trip
        // "assigned but never used" under TreatWarningsAsErrors.
    }
}
