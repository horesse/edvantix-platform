using System.Text.Json.Serialization;

namespace FSH.Modules.People.Contracts.Dtos;

/// <summary>
/// Lifecycle states a student transitions through.
///
/// Allowed transitions:
///   Lead     → Active    (enrolled)
///   Active   → Paused    (temporary hold, e.g. break, unpaid invoice)
///   Paused   → Active    (resumed)
///   Active   → Archived  (left the school)
///   Paused   → Archived  (left while paused)
///   Archived → Active    (returned — history of the original enrollment is preserved,
///                         see <c>Student.EnrolledAtUtc</c>)
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<StudentStatus>))]
public enum StudentStatus
{
    Lead = 0,
    Active = 1,
    Paused = 2,
    Archived = 3,
}
