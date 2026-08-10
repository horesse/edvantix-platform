using System.Text.Json.Serialization;

namespace FSH.Modules.People.Contracts.Dtos;

/// <summary>Whether a teacher currently takes on groups/sessions.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TeacherStatus>))]
public enum TeacherStatus
{
    Active = 0,
    Inactive = 1,
}
