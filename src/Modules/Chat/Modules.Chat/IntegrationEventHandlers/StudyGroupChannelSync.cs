using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using FSH.Modules.People.Contracts.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.IntegrationEventHandlers;

/// <summary>Shared bits for the study-group channel sync handlers (create / enrol / unenrol / finish).</summary>
internal static class StudyGroupChannelSync
{
    /// <summary>Actor stamp for member/lock mutations that originate from an event, not a user.</summary>
    public const string SystemActor = "system:studygroup-sync";

    public static Task<ChatChannel?> FindChannelAsync(ChatDbContext db, Guid studyGroupId, CancellationToken ct) =>
        db.Channels.FirstOrDefaultAsync(c => c.SourceStudyGroupId == studyGroupId, ct);

    /// <summary>
    /// The account that represents a student in chat: their own login, else the primary-payer
    /// guardian's, else any guardian with a login. Null when nobody in the family has an account
    /// (the student simply isn't added — docs/02 Модули/Chat.md → «Ученик без учётной записи»).
    /// </summary>
    public static string? ResolveChatUserId(StudentContactsDto contacts)
    {
        ArgumentNullException.ThrowIfNull(contacts);

        if (!string.IsNullOrWhiteSpace(contacts.Student.UserId))
        {
            return contacts.Student.UserId;
        }

        var payer = contacts.Guardians
            .FirstOrDefault(g => g.Role == ContactRole.PrimaryPayerGuardian && !string.IsNullOrWhiteSpace(g.UserId));
        if (payer is not null)
        {
            return payer.UserId;
        }

        return contacts.Guardians.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g.UserId))?.UserId;
    }
}
