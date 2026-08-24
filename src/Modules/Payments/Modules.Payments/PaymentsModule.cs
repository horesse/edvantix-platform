using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Payments.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Features.v1.Payments.ConfirmPayment;
using FSH.Modules.Payments.Features.v1.Payments.GetInvoicePayments;
using FSH.Modules.Payments.Features.v1.Payments.ReversePayment;
using FSH.Modules.Payments.Features.v1.StudentInvoices.BulkGenerateInvoices;
using FSH.Modules.Payments.Features.v1.StudentInvoices.BulkIssueInvoices;
using FSH.Modules.Payments.Features.v1.StudentInvoices.CancelInvoice;
using FSH.Modules.Payments.Features.v1.StudentInvoices.CreateStudentInvoice;
using FSH.Modules.Payments.Features.v1.StudentInvoices.GetStudentInvoiceById;
using FSH.Modules.Payments.Features.v1.StudentInvoices.IssueInvoice;
using FSH.Modules.Payments.Features.v1.StudentInvoices.SearchStudentInvoices;
using FSH.Modules.Payments.Features.v1.StudentInvoices.UpdateStudentInvoice;
using FSH.Modules.Payments.Features.v1.Tariffs.CreateTariff;
using FSH.Modules.Payments.Features.v1.Tariffs.DeactivateTariff;
using FSH.Modules.Payments.Features.v1.Tariffs.GetTariffs;
using FSH.Modules.Payments.Features.v1.Tariffs.UpdateTariff;
using FSH.Modules.Payments.Services;
using FSH.Modules.Files.Contracts;
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
        builder.Services.AddScoped<ITariffAccrualService, TariffAccrualService>();
        builder.Services.AddScoped<IFileAccessPolicy, PaymentProofAccessPolicy>();

        // Outbox/Inbox for PaymentsDbContext, eventing trio and jobs are wired in as their
        // respective vertical slices land — see docs/04 Задачи/Задачи · Новые модули.md → Payments
        // for the step-by-step log.

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

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        // Flat resource routing — same convention as People/Curriculum/StudyGroups/Scheduling.
        var group = endpoints.MapGroup("api/v{version:apiVersion}")
            .WithTags("Payments")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        group.MapCreateTariffEndpoint();
        group.MapUpdateTariffEndpoint();
        group.MapDeactivateTariffEndpoint();
        group.MapGetTariffsEndpoint();

        group.MapCreateStudentInvoiceEndpoint();
        group.MapUpdateStudentInvoiceEndpoint();
        group.MapGenerateInvoicesEndpoint();
        group.MapIssueInvoiceEndpoint();
        group.MapIssueInvoicesEndpoint();
        group.MapCancelInvoiceEndpoint();
        group.MapGetStudentInvoiceByIdEndpoint();
        group.MapSearchStudentInvoicesEndpoint();

        group.MapConfirmPaymentEndpoint();
        group.MapGetInvoicePaymentsEndpoint();
        group.MapRevokePaymentEndpoint();

        // Remaining endpoints and recurring jobs are wired in as their features land — see the step
        // log in docs/04 Задачи/Задачи · Новые модули.md → Payments.
    }
}
