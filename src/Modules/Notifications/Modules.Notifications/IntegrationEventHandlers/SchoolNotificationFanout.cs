using FSH.Modules.Notifications.Channels;
using FSH.Modules.People.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>
/// Shared recipient resolution + dispatch for the school-domain notification handlers
/// (Scheduling / Payments / StudyGroups). Turns a group id or a student id into a de-duplicated set
/// of <see cref="ContactDto"/> and dispatches one rendered notification per recipient.
/// </summary>
public sealed class SchoolNotificationFanout(
    IStudyGroupQueryService studyGroups,
    IPeopleLookupService people,
    INotificationDispatcher dispatcher)
{
    /// <summary>The group's active students (+ their guardians) and, optionally, its primary teacher.</summary>
    public async Task<GroupAudience?> ResolveGroupAsync(Guid studyGroupId, bool includeTeacher, CancellationToken ct)
    {
        var brief = await studyGroups.GetBriefAsync(studyGroupId, ct).ConfigureAwait(false);
        if (brief is null)
        {
            return null;
        }

        var studentIds = await studyGroups
            .GetActiveStudentIdsAsync(studyGroupId, DateOnly.FromDateTime(DateTime.UtcNow), ct)
            .ConfigureAwait(false);
        var students = await people.GetStudentContactsAsync(studentIds, ct).ConfigureAwait(false);
        var teacher = includeTeacher
            ? await people.GetTeacherContactAsync(brief.PrimaryTeacherId, ct).ConfigureAwait(false)
            : null;

        return new GroupAudience(brief.Name, students, teacher);
    }

    public Task<IReadOnlyList<StudentContactsDto>> ResolveStudentsAsync(Guid studentId, CancellationToken ct) =>
        people.GetStudentContactsAsync([studentId], ct).AsTask();

    /// <summary>Dispatches <paramref name="templateKey"/> to every distinct recipient in <paramref name="contacts"/>.</summary>
    public async Task DispatchAsync(
        IEnumerable<ContactDto> contacts,
        string templateKey,
        string source,
        NotificationChannelKind channels,
        IReadOnlyDictionary<string, string?> tokens,
        string? expectedTenantId,
        object? metadata,
        CancellationToken ct)
    {
        foreach (var contact in Distinct(contacts))
        {
            var recipientId = contact.UserId ?? $"email:{contact.Email}";
            await dispatcher.DispatchAsync(
                new NotificationRequest(recipientId, templateKey, tokens)
                {
                    Source = source,
                    RecipientEmail = contact.Email,
                    // No account → in-app impossible; fall back to e-mail only.
                    Channels = contact.UserId is null ? channels & NotificationChannelKind.Email : channels,
                    ExpectedTenantId = expectedTenantId,
                    Metadata = metadata,
                },
                ct).ConfigureAwait(false);
        }
    }

    /// <summary>Who pays for a student: the primary-payer guardian(s), or the student themselves if none is set.</summary>
    public static IEnumerable<ContactDto> Payers(StudentContactsDto student)
    {
        ArgumentNullException.ThrowIfNull(student);
        var payers = student.Guardians.Where(g => g.Role == ContactRole.PrimaryPayerGuardian).ToList();
        return payers.Count > 0 ? payers : [student.Student];
    }

    /// <summary>Distinct by account id, else by e-mail; drops contacts with neither.</summary>
    public static IReadOnlyList<ContactDto> Distinct(IEnumerable<ContactDto> contacts)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ContactDto>();
        foreach (var c in contacts)
        {
            var key = c.UserId ?? c.Email;
            if (!string.IsNullOrWhiteSpace(key) && seen.Add(key))
            {
                result.Add(c);
            }
        }

        return result;
    }
}

/// <summary>A study group's notification audience.</summary>
public sealed record GroupAudience(
    string GroupName,
    IReadOnlyList<StudentContactsDto> Students,
    ContactDto? Teacher)
{
    /// <summary>Students' own accounts + their guardians.</summary>
    public IEnumerable<ContactDto> StudentsAndGuardians =>
        Students.SelectMany(s => new[] { s.Student }.Concat(s.Guardians));

    /// <summary>Students' own accounts + guardians + the teacher (when present).</summary>
    public IEnumerable<ContactDto> Everyone =>
        Teacher is null ? StudentsAndGuardians : StudentsAndGuardians.Append(Teacher);
}
