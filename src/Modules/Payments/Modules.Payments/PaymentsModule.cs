using Asp.Versioning;
using FSH.Framework.Eventing;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Payments.Authorization;
using FSH.Modules.Payments.Contracts;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Features.v1.Payments.ConfirmPayment;
using FSH.Modules.Payments.Features.v1.Payments.GetInvoicePayments;
using FSH.Modules.Payments.Features.v1.Payments.ReversePayment;
using FSH.Modules.Payments.Features.v1.StudentInvoices.BulkGenerateInvoices;
using FSH.Modules.Payments.Features.v1.StudentInvoices.BulkIssueInvoices;
using FSH.Modules.Payments.Features.v1.StudentInvoices.CancelInvoice;
using FSH.Modules.Payments.Features.v1.StudentInvoices.CreateStudentInvoice;
using FSH.Modules.Payments.Features.v1.StudentInvoices.GetDebtorsReport;
using FSH.Modules.Payments.Features.v1.StudentInvoices.GetInvoicePdf;
using FSH.Modules.Payments.Features.v1.StudentInvoices.GetMyInvoices;
using FSH.Modules.Payments.Features.v1.StudentInvoices.GetMyMaterialsAccess;
using FSH.Modules.Payments.Features.v1.StudentInvoices.GetRevenueReport;
using FSH.Modules.Payments.Features.v1.StudentInvoices.GetStudentBalance;
using FSH.Modules.Payments.Features.v1.StudentInvoices.GetStudentInvoiceById;
using FSH.Modules.Payments.Features.v1.StudentInvoices.IssueInvoice;
using FSH.Modules.Payments.Features.v1.StudentInvoices.SearchStudentInvoices;
using FSH.Modules.Payments.Features.v1.StudentInvoices.UpdateStudentInvoice;
using FSH.Modules.Payments.Features.v1.Tariffs.CreateTariff;
using FSH.Modules.Payments.Features.v1.Tariffs.DeactivateTariff;
using FSH.Modules.Payments.Features.v1.Tariffs.GetTariffs;
using FSH.Modules.Payments.Features.v1.Tariffs.UpdateTariff;
using FSH.Modules.Payments.Jobs;
using FSH.Modules.Payments.Services;
using FSH.Modules.Files.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Hangfire;

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
        builder.Services.AddScoped<IInvoiceNumberGenerator, InvoiceNumberGenerator>();
        builder.Services.AddScoped<IInvoicePdfRenderer, InvoicePdfRenderer>();
        builder.Services.AddScoped<IDraftInvoiceRefreshService, DraftInvoiceRefreshService>();
        builder.Services.AddScoped<IFileAccessPolicy, PaymentProofAccessPolicy>();

        // EDX-015 — the materials-on-debt rule. Curriculum's LessonMaterialAccessPolicy and the
        // dashboard cabinet both consult this through IMaterialsAccessService.
        builder.Services.AddScoped<IMaterialsAccessService, MaterialsAccessService>();

        // Outbox/Inbox for PaymentsDbContext — publishes StudentInvoiceIssued/Cancelled,
        // StudentPaymentConfirmed, StudentInvoiceOverdue (the last from DetectOverdueInvoicesJob).
        // AddEventingCore() is NOT called here: IdentityModule already registers it (bus +
        // OutboxDispatcherHostedService) — see People/StudyGroups for the same note on why calling
        // it twice would start a second hosted dispatcher.
        builder.Services.AddEventingForDbContext<PaymentsDbContext>();

        // Payments subscribes to Scheduling/StudyGroups/People integration events (SessionHeld,
        // SessionCancelled, StudentEnrolled, StudentUnenrolled, StudentArchived) — see
        // IntegrationEventHandlers/.
        builder.Services.AddIntegrationEventHandlers(typeof(PaymentsModule).Assembly);

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
        group.MapGetMyInvoicesEndpoint();
        group.MapGetMyMaterialsAccessEndpoint();
        group.MapGetInvoicePdfEndpoint();

        group.MapConfirmPaymentEndpoint();
        group.MapGetInvoicePaymentsEndpoint();
        group.MapRevokePaymentEndpoint();

        group.MapGetStudentBalanceEndpoint();
        group.MapGetDebtorsReportEndpoint();
        group.MapGetRevenueReportEndpoint();

        // Recurring Hangfire jobs — registration here matches the pattern Files/Billing/Scheduling use.
        var jobManager = endpoints.ServiceProvider.GetService<IRecurringJobManager>();
        if (jobManager is not null)
        {
            jobManager.AddOrUpdate<DetectOverdueInvoicesJob>(
                "payments-detect-overdue-invoices",
                j => j.RunAsync(CancellationToken.None),
                "0 3 * * *", // daily 03:00 UTC
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            jobManager.AddOrUpdate<PaymentReminderJob>(
                "payments-payment-reminders",
                j => j.RunAsync(CancellationToken.None),
                "0 4 * * *", // daily 04:00 UTC
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            jobManager.AddOrUpdate<MonthlyInvoiceDraftJob>(
                "payments-monthly-invoice-drafts",
                j => j.RunAsync(CancellationToken.None),
                "0 5 1 * *", // 1st of the month, 05:00 UTC
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
    }
}
