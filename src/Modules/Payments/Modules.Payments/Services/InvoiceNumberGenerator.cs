using System.Data;
using System.Globalization;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FSH.Modules.Payments.Services;

/// <summary>
/// Reserves invoice numbers with a single atomic upsert against <c>payments."InvoiceNumberSequences"</c>:
/// <code>
/// INSERT ... VALUES (..., @count, ...)
/// ON CONFLICT ("TenantId","Scope") DO UPDATE SET "NextValue" = existing + EXCLUDED."NextValue"
/// RETURNING "NextValue";
/// </code>
/// The <c>DO UPDATE</c> takes a row lock, so two concurrent batches for the same tenant/scope are
/// serialised and their reserved ranges never overlap. The returned high-water mark minus
/// <c>count</c> is the first number of the reserved block. Numbers are formatted by
/// <see cref="InvoiceNumberFormat"/> from the tenant's template.
/// </summary>
internal sealed class InvoiceNumberGenerator(
    PaymentsDbContext dbContext,
    ITenantSettingsService tenantSettings,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    TimeProvider timeProvider) : IInvoiceNumberGenerator
{
    public async ValueTask<string> NextAsync(CancellationToken cancellationToken = default)
    {
        var batch = await NextBatchAsync(1, cancellationToken).ConfigureAwait(false);
        return batch[0];
    }

    public async ValueTask<IReadOnlyList<string>> NextBatchAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return [];
        }

        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new InvalidOperationException("No tenant context available for invoice numbering.");

        var settings = await tenantSettings.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var template = InvoiceNumberFormat.IsValid(settings.InvoiceNumberTemplate)
            ? settings.InvoiceNumberTemplate
            : InvoiceNumberFormat.DefaultTemplate;

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var scope = InvoiceNumberFormat.IsYearScoped(template)
            ? today.Year.ToString("D4", CultureInfo.InvariantCulture)
            : "*";

        var highWater = await ReserveBlockAsync(tenantId, scope, count, cancellationToken).ConfigureAwait(false);
        var start = highWater - count + 1;

        var numbers = new string[count];
        for (var i = 0; i < count; i++)
        {
            numbers[i] = InvoiceNumberFormat.Render(template, start + i, today);
        }

        return numbers;
    }

    private async ValueTask<long> ReserveBlockAsync(string tenantId, string scope, int count, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var ambient = dbContext.Database.CurrentTransaction;

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO payments."InvoiceNumberSequences" ("Id", "TenantId", "Scope", "NextValue", "CreatedAtUtc")
            VALUES (@id, @tenant, @scope, @count, now())
            ON CONFLICT ("TenantId", "Scope")
            DO UPDATE SET "NextValue" = payments."InvoiceNumberSequences"."NextValue" + EXCLUDED."NextValue",
                          "UpdatedAtUtc" = now()
            RETURNING "NextValue";
            """;
        AddParameter(command, "id", Guid.CreateVersion7());
        AddParameter(command, "tenant", tenantId);
        AddParameter(command, "scope", scope);
        AddParameter(command, "count", (long)count);

        if (ambient is not null)
        {
            command.Transaction = ambient.GetDbTransaction();
        }

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
