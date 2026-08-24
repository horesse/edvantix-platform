using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Scheduling.Contracts.Authorization;

public static class SchedulingPermissions
{
    public static class Sessions
    {
        public const string Resource = "Scheduling.Sessions";
        public const string View       = $"Permissions.{Resource}.View";
        public const string ViewOwn    = $"Permissions.{Resource}.ViewOwn";
        public const string Create     = $"Permissions.{Resource}.Create";
        public const string Update     = $"Permissions.{Resource}.Update";
        public const string Cancel     = $"Permissions.{Resource}.Cancel";
        public const string Reschedule = $"Permissions.{Resource}.Reschedule";
        public const string Generate   = $"Permissions.{Resource}.Generate";
    }

    public static class Attendance
    {
        public const string Resource = "Scheduling.Attendance";
        public const string View     = $"Permissions.{Resource}.View";
        public const string ViewOwn  = $"Permissions.{Resource}.ViewOwn";
        public const string Mark     = $"Permissions.{Resource}.Mark";
        public const string MarkAny  = $"Permissions.{Resource}.MarkAny";
    }

    public static class Rooms
    {
        public const string Resource = "Scheduling.Rooms";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Manage = $"Permissions.{Resource}.Manage";
    }

    public static class ScheduleTemplates
    {
        public const string Resource = "Scheduling.ScheduleTemplates";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Manage = $"Permissions.{Resource}.Manage";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Sessions",           ActionConstants.View,   Sessions.Resource, IsBasic: true),
        new("View Own Sessions",       "ViewOwn",               Sessions.Resource, IsBasic: true),
        new("Create Sessions",         ActionConstants.Create, Sessions.Resource),
        new("Update Sessions",         ActionConstants.Update, Sessions.Resource),
        new("Cancel Sessions",         "Cancel",                Sessions.Resource),
        new("Reschedule Sessions",     "Reschedule",            Sessions.Resource),
        // Generate touches hundreds of rows in one call (schedule-template generator) — kept
        // separate from Create on purpose, see docs/02 Модули/Scheduling.md → "Права".
        new("Generate Sessions",       "Generate",              Sessions.Resource),

        new("View Attendance",         ActionConstants.View,   Attendance.Resource, IsBasic: true),
        new("View Own Attendance",     "ViewOwn",               Attendance.Resource, IsBasic: true),
        new("Mark Attendance",         "Mark",                  Attendance.Resource),
        // MarkAny = amend attendance retroactively after a billing period has closed — separate
        // from Mark for the same reason Generate is separate from Create.
        new("Mark Any Attendance",     "MarkAny",               Attendance.Resource),

        new("View Rooms",              ActionConstants.View,   Rooms.Resource, IsBasic: true),
        new("Manage Rooms",            "Manage",                Rooms.Resource),

        new("View Schedule Templates", ActionConstants.View,   ScheduleTemplates.Resource, IsBasic: true),
        new("Manage Schedule Templates", "Manage",              ScheduleTemplates.Resource),
    ];
}
