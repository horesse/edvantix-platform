using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Caching;
using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Multitenancy.Services;

public sealed class TenantSettingsService : ITenantSettingsService
{
    private static readonly HybridCacheEntryOptions SettingsEntryOptions = new()
    {
        Expiration = TimeSpan.FromHours(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(2),
    };

    private readonly HybridCache _cache;
    private readonly TenantDbContext _dbContext;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TenantSettingsService> _logger;

    public TenantSettingsService(
        HybridCache cache,
        TenantDbContext dbContext,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        ICurrentUser currentUser,
        ILogger<TenantSettingsService> logger)
    {
        _cache = cache;
        _dbContext = dbContext;
        _tenantAccessor = tenantAccessor;
        _currentUser = currentUser;
        _logger = logger;
    }

    public Task<TenantSettingsDto> GetCurrentAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new InvalidOperationException("No tenant context available");
        return GetAsync(tenantId, ct);
    }

    public Task<TenantSettingsDto> GetAsync(string tenantId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        // Per-tenant tag array — one small alloc per call is unavoidable because the tag is
        // parameterized by tenantId. Keeping the array allocation local (not LOH) and short-lived.
        var tags = new[] { CacheKeys.Tags.Tenant(tenantId) };

        // Stateless factory via a static method group — no closure allocation even on L1 hits.
        var state = new TenantFactoryState(_dbContext, tenantId);
        return _cache.GetOrCreateAsync(
            CacheKeys.TenantSettings(tenantId),
            state,
            LoadSettingsAsync,
            SettingsEntryOptions,
            tags,
            ct).AsTask();
    }

    private static async ValueTask<TenantSettingsDto> LoadSettingsAsync(TenantFactoryState state, CancellationToken ct)
    {
        var entity = await state.DbContext.TenantSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == state.TenantId, ct)
            .ConfigureAwait(false);

        return entity is null ? TenantSettingsDto.Default : MapEntityToDto(entity);
    }

    private readonly record struct TenantFactoryState(TenantDbContext DbContext, string TenantId);

    public async Task<TenantSettingsDto> CreateAsync(string tenantId, string? timeZoneId, string? currency, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var existing = await _dbContext.TenantSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return MapEntityToDto(existing);
        }

        var entity = TenantSettings.Create(tenantId, timeZoneId, currency, GetCurrentUserId());
        _dbContext.TenantSettings.Add(entity);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        await InvalidateCacheAsync(tenantId, ct).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Created settings for tenant {TenantId}", tenantId);
        }

        return MapEntityToDto(entity);
    }

    public async Task UpdateAsync(string tenantId, TenantSettingsDto settings, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(settings);

        var entity = await _dbContext.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = TenantSettings.Create(
                tenantId, settings.TimeZoneId, settings.Currency, GetCurrentUserId(),
                settings.RestrictMaterialsOnDebt, settings.DebtGraceDays, settings.InvoiceNumberTemplate);
            _dbContext.TenantSettings.Add(entity);
        }
        else
        {
            entity.Update(
                settings.TimeZoneId, settings.Currency, GetCurrentUserId(),
                settings.RestrictMaterialsOnDebt, settings.DebtGraceDays, settings.InvoiceNumberTemplate);
        }

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        await InvalidateCacheAsync(tenantId, ct).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Updated settings for tenant {TenantId}", tenantId);
        }
    }

    public async Task InvalidateCacheAsync(string tenantId, CancellationToken ct = default)
    {
        // Purge both the tenant-specific entry and anything tagged for this tenant.
        await _cache.RemoveAsync(CacheKeys.TenantSettings(tenantId), ct).ConfigureAwait(false);
        await _cache.RemoveByTagAsync(CacheKeys.Tags.Tenant(tenantId), ct).ConfigureAwait(false);
    }

    private static TenantSettingsDto MapEntityToDto(TenantSettings entity) => new()
    {
        TimeZoneId = entity.TimeZoneId,
        Currency = entity.Currency,
        RestrictMaterialsOnDebt = entity.RestrictMaterialsOnDebt,
        DebtGraceDays = entity.DebtGraceDays,
        InvoiceNumberTemplate = entity.InvoiceNumberTemplate,
    };

    private string? GetCurrentUserId()
    {
        var userId = _currentUser.GetUserId();
        return userId == Guid.Empty ? null : userId.ToString();
    }
}
