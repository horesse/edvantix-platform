using Asp.Versioning;
using FluentValidation;
using FSH.Framework.Eventing;
using FSH.Framework.Persistence;
using FSH.Modules.Notifications.Features.v1.Digest;
using Hangfire;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Notifications.Contracts.Authorization;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Features.v1.GetUnreadCount;
using FSH.Modules.Notifications.Features.v1.ListNotifications;
using FSH.Modules.Notifications.Features.v1.MarkAllNotificationsRead;
using FSH.Modules.Notifications.Features.v1.MarkNotificationRead;
using FSH.Modules.Notifications.Features.v1.Preferences;
using FSH.Modules.Notifications.Features.v1.QuietHours;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.Notifications;

/// <summary>
/// Notifications module: per-user inbox driven by integration events from other modules. Module
/// Order 750 places it BEFORE Chat (800) so its integration-event handlers are registered
/// before Chat starts publishing — handler registration is order-sensitive.
/// </summary>
public sealed class NotificationsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(NotificationPermissions.All);

        builder.Services.AddHeroDbContext<NotificationsDbContext>();
        builder.Services.AddScoped<IDbInitializer, NotificationsDbInitializer>();
        builder.Services.AddValidatorsFromAssembly(typeof(NotificationsModule).Assembly);

        // Notification copy: one tokenised template per type (the framework ships no template
        // engine — see BuildingBlocks/Mailing). Stateless, so registered as singletons.
        builder.Services.AddSingleton<Templating.INotificationTemplateCatalog, Templating.NotificationTemplateCatalog>();
        builder.Services.AddSingleton<Templating.INotificationTemplateRenderer, Templating.NotificationTemplateRenderer>();

        // Delivery channels + the dispatcher that renders once and fans out. Adding Telegram/SMS
        // later = one more INotificationChannel registration, nothing else.
        builder.Services.AddScoped<Channels.INotificationChannel, Channels.InAppNotificationChannel>();
        builder.Services.AddScoped<Channels.INotificationChannel, Channels.EmailNotificationChannel>();
        builder.Services.AddScoped<Channels.INotificationDispatcher, Channels.NotificationDispatcher>();

        // Per-user opt-in/opt-out; consulted by the dispatcher and exposed via /preferences.
        builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();

        // School-wide quiet hours — the dispatcher holds e-mail during the window.
        builder.Services.AddScoped<INotificationQuietHoursService, NotificationQuietHoursService>();

        // Digest buffer — the dispatcher writes digestable e-mails here; NotificationDigestJob flushes.
        builder.Services.AddScoped<INotificationDigestBuffer, NotificationDigestBuffer>();

        // Recipient resolution + school-local time formatting shared by the school-domain handlers.
        builder.Services.AddScoped<IntegrationEventHandlers.SchoolNotificationFanout>();
        builder.Services.AddScoped<IntegrationEventHandlers.NotificationTimeFormatter>();

        // Subscribe to cross-module integration events handled by this assembly.
        builder.Services.AddIntegrationEventHandlers(typeof(NotificationsModule).Assembly);

        builder.Services.AddHealthChecks().AddDbContextCheck<NotificationsDbContext>(
            name: "db:notifications",
            failureStatus: HealthStatus.Unhealthy);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("api/v{version:apiVersion}/notifications")
            .WithTags("Notifications")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // Literal routes first; /{id:guid}/read is the only param-route and lives last.
        group.MapListNotificationsEndpoint();              // GET /
        group.MapGetUnreadCountEndpoint();                 // GET /unread-count
        group.MapListNotificationPreferencesEndpoint();    // GET /preferences
        group.MapUpdateNotificationPreferencesEndpoint();  // PUT /preferences
        group.MapGetNotificationQuietHoursEndpoint();      // GET /quiet-hours
        group.MapSetNotificationQuietHoursEndpoint();      // PUT /quiet-hours
        group.MapMarkAllNotificationsReadEndpoint();       // POST /read-all
        group.MapMarkNotificationReadEndpoint();           // POST /{id:guid}/read

        // Recurring digest flush — same registration pattern as Payments/Scheduling jobs.
        var jobManager = endpoints.ServiceProvider.GetService<IRecurringJobManager>();
        jobManager?.AddOrUpdate<NotificationDigestJob>(
            "notifications-digest-flush",
            j => j.RunAsync(CancellationToken.None),
            "*/5 * * * *", // every 5 minutes
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}
