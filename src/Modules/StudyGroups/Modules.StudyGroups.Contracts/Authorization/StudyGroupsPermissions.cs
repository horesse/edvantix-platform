using FSH.Framework.Shared.Constants;

namespace FSH.Modules.StudyGroups.Contracts.Authorization;

public static class StudyGroupsPermissions
{
    public static class StudyGroups
    {
        public const string Resource = "StudyGroups.StudyGroups";
        public const string View    = $"Permissions.{Resource}.View";
        public const string ViewOwn = $"Permissions.{Resource}.ViewOwn";
        public const string Create  = $"Permissions.{Resource}.Create";
        public const string Update  = $"Permissions.{Resource}.Update";
        public const string Delete  = $"Permissions.{Resource}.Delete";
        public const string Archive = $"Permissions.{Resource}.Archive";
    }

    public static class Enrollments
    {
        public const string Resource = "StudyGroups.Enrollments";
        public const string View     = $"Permissions.{Resource}.View";
        public const string Create   = $"Permissions.{Resource}.Create";
        public const string Update   = $"Permissions.{Resource}.Update";
        public const string Delete   = $"Permissions.{Resource}.Delete";
        public const string Transfer = $"Permissions.{Resource}.Transfer";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Study Groups",     ActionConstants.View,   StudyGroups.Resource, IsBasic: true),
        new("View Own Study Groups", "ViewOwn",               StudyGroups.Resource, IsBasic: true),
        new("Create Study Groups",   ActionConstants.Create, StudyGroups.Resource),
        new("Update Study Groups",   ActionConstants.Update, StudyGroups.Resource),
        new("Delete Study Groups",   ActionConstants.Delete, StudyGroups.Resource),
        new("Archive Study Groups",  "Archive",               StudyGroups.Resource),

        new("View Enrollments",     ActionConstants.View,   Enrollments.Resource, IsBasic: true),
        new("Create Enrollments",   ActionConstants.Create, Enrollments.Resource),
        new("Update Enrollments",   ActionConstants.Update, Enrollments.Resource),
        new("Delete Enrollments",   ActionConstants.Delete, Enrollments.Resource),
        new("Transfer Enrollments", "Transfer",               Enrollments.Resource),
    ];
}
