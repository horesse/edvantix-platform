using System.Collections.ObjectModel;
using System.Text;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Notifications.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Notifications.Features.v1.Digest;

/// <summary>
/// Every few minutes, flushes each recipient's held digestable e-mails (see
/// <see cref="INotificationDigestBuffer"/>) into one summary message — once the batch's oldest entry
/// has waited out the aggregation window, so late-arriving siblings (a whole class cancelled over a
/// minute) land in the same e-mail. Per-tenant fresh scope, same shape as Payments'
/// <c>DetectOverdueInvoicesJob</c>.
/// </summary>
public sealed class NotificationDigestJob(
    IMultiTenantStore<AppTenantInfo> tenantStore,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<NotificationDigestJob> logger)
{
    /// <summary>Hold the first entry this long for siblings before sending the summary.</summary>
    public static readonly TimeSpan AggregationWindow = TimeSpan.FromMinutes(7);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var tenants = await tenantStore.GetAllAsync().ConfigureAwait(false);
        var sent = 0;

        foreach (var tenant in tenants)
        {
            // No root skip (unlike scan-everything jobs): this one only acts on rows that were
            // already buffered, so an empty tenant is a cheap no-op.
            if (!tenant.IsActive)
            {
                continue;
            }

            try
            {
                sent += await FlushTenantAsync(tenant, cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // one tenant's failure must not block the rest
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(ex, "[Notifications] digest flush failed for tenant {TenantId}", tenant.Id);
            }
        }

        if (sent > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[Notifications] digest flush sent {Count} summary e-mail(s)", sent);
        }
    }

    private async Task<int> FlushTenantAsync(AppTenantInfo tenant, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        ((IMultiTenantContextSetter)scope.ServiceProvider.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>())
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var mail = scope.ServiceProvider.GetRequiredService<IMailService>();

        var pending = await db.PendingNotificationDigests
            .Where(d => d.SentAtUtc == null)
            .OrderBy(d => d.CreatedAtUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (pending.Count == 0)
        {
            return 0;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var ready = pending
            .GroupBy(d => d.RecipientEmail, StringComparer.OrdinalIgnoreCase)
            .Where(g => now - g.Min(d => d.CreatedAtUtc) >= AggregationWindow)
            .ToList();

        var sent = 0;
        foreach (var group in ready)
        {
            var entries = group.OrderBy(d => d.CreatedAtUtc).ToList();
            var (subject, body) = Compose(entries.Count, entries.Select(e => (e.Title, e.Body)));

            try
            {
                await mail.SendAsync(
                    new MailRequest(new Collection<string> { group.Key }, subject, body), ct).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // best-effort; leave the rows unsent to retry next tick
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogWarning(ex, "[Notifications] failed to send digest to {Email}", group.Key);
                continue;
            }

            foreach (var entry in entries)
            {
                entry.MarkSent(now);
            }

            sent++;
        }

        if (sent > 0)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return sent;
    }

    private static (string Subject, string Body) Compose(int count, IEnumerable<(string Title, string? Body)> items)
    {
        var subject = $"{count} update{(count == 1 ? string.Empty : "s")} from your school";

        var list = new StringBuilder();
        foreach (var (title, body) in items)
        {
            list.Append("<li style=\"margin-bottom:8px\"><strong>")
                .Append(Escape(title))
                .Append("</strong>");
            if (!string.IsNullOrWhiteSpace(body))
            {
                list.Append("<br>").Append(Escape(body));
            }

            list.Append("</li>");
        }

        var html =
            "<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1a1a1a;line-height:1.5\">" +
            $"<h2 style=\"font-size:18px;margin:0 0 12px\">{Escape(subject)}</h2>" +
            $"<ul style=\"padding-left:18px\">{list}</ul>" +
            "<p style=\"margin-top:24px;color:#6b7280;font-size:12px\">This is a summary of recent updates from your school.</p>" +
            "</div>";

        return (subject, html);
    }

    private static string Escape(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
             .Replace("<", "&lt;", StringComparison.Ordinal)
             .Replace(">", "&gt;", StringComparison.Ordinal);
}
