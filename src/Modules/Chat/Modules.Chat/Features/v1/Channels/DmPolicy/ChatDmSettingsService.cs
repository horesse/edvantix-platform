using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Channels.DmPolicy;

/// <summary>Per-school direct-message toggles (currently: student↔student DMs, off by default).</summary>
public interface IChatDmSettingsService
{
    Task<ChatDmSettingsDto> GetAsync(CancellationToken ct = default);

    Task SetAsync(bool allowStudentToStudentDm, CancellationToken ct = default);
}

public sealed class ChatDmSettingsService(ChatDbContext db) : IChatDmSettingsService
{
    public async Task<ChatDmSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var row = await db.DmSettings.AsNoTracking().FirstOrDefaultAsync(ct).ConfigureAwait(false);
        return new ChatDmSettingsDto(row?.AllowStudentToStudentDm ?? false);
    }

    public async Task SetAsync(bool allowStudentToStudentDm, CancellationToken ct = default)
    {
        var row = await db.DmSettings.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null)
        {
            db.DmSettings.Add(ChatDmSettings.Create(allowStudentToStudentDm));
        }
        else
        {
            row.Set(allowStudentToStudentDm);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
