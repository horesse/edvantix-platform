using FSH.Framework.Shared.Constants;

namespace FSH.Modules.People.Contracts.Authorization;

public static class PeoplePermissions
{
    public static class Students
    {
        public const string Resource = "People.Students";
        public const string View       = $"Permissions.{Resource}.View";
        public const string Create     = $"Permissions.{Resource}.Create";
        public const string Update     = $"Permissions.{Resource}.Update";
        public const string Delete     = $"Permissions.{Resource}.Delete";
        public const string Export     = $"Permissions.{Resource}.Export";
        public const string ViewNotes  = $"Permissions.{Resource}.ViewNotes";
    }

    public static class Teachers
    {
        public const string Resource = "People.Teachers";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class Guardians
    {
        public const string Resource = "People.Guardians";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Students",       ActionConstants.View,   Students.Resource, IsBasic: true),
        new("Create Students",     ActionConstants.Create, Students.Resource),
        new("Update Students",     ActionConstants.Update, Students.Resource),
        new("Delete Students",     ActionConstants.Delete, Students.Resource),
        new("Export Students",     "Export",                Students.Resource),
        new("View Student Notes",  "ViewNotes",             Students.Resource),

        new("View Teachers",   ActionConstants.View,   Teachers.Resource, IsBasic: true),
        new("Create Teachers", ActionConstants.Create, Teachers.Resource),
        new("Update Teachers", ActionConstants.Update, Teachers.Resource),
        new("Delete Teachers", ActionConstants.Delete, Teachers.Resource),

        new("View Guardians",   ActionConstants.View,   Guardians.Resource, IsBasic: true),
        new("Create Guardians", ActionConstants.Create, Guardians.Resource),
        new("Update Guardians", ActionConstants.Update, Guardians.Resource),
        new("Delete Guardians", ActionConstants.Delete, Guardians.Resource),
    ];
}
