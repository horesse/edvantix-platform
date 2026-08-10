using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Curriculum.Contracts.Authorization;

public static class CurriculumPermissions
{
    public static class Subjects
    {
        public const string Resource = "Curriculum.Subjects";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class Courses
    {
        public const string Resource = "Curriculum.Courses";
        public const string View      = $"Permissions.{Resource}.View";
        public const string Create    = $"Permissions.{Resource}.Create";
        public const string Update    = $"Permissions.{Resource}.Update";
        public const string Delete    = $"Permissions.{Resource}.Delete";
        public const string Publish   = $"Permissions.{Resource}.Publish";
        public const string Restore   = $"Permissions.{Resource}.Restore";
        public const string ViewTrash = $"Permissions.{Resource}.ViewTrash";
    }

    public static class Lessons
    {
        public const string Resource = "Curriculum.Lessons";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class LessonMaterials
    {
        public const string Resource = "Curriculum.LessonMaterials";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Manage = $"Permissions.{Resource}.Manage";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Subjects",   ActionConstants.View,   Subjects.Resource, IsBasic: true),
        new("Create Subjects", ActionConstants.Create, Subjects.Resource),
        new("Update Subjects", ActionConstants.Update, Subjects.Resource),
        new("Delete Subjects", ActionConstants.Delete, Subjects.Resource),

        new("View Courses",      ActionConstants.View,   Courses.Resource, IsBasic: true),
        new("Create Courses",    ActionConstants.Create, Courses.Resource),
        new("Update Courses",    ActionConstants.Update, Courses.Resource),
        new("Delete Courses",    ActionConstants.Delete, Courses.Resource),
        new("Publish Courses",   "Publish",              Courses.Resource),
        new("Restore Courses",   "Restore",              Courses.Resource),
        new("View Course Trash", "ViewTrash",            Courses.Resource),

        new("View Lessons",   ActionConstants.View,   Lessons.Resource, IsBasic: true),
        new("Create Lessons", ActionConstants.Create, Lessons.Resource),
        new("Update Lessons", ActionConstants.Update, Lessons.Resource),
        new("Delete Lessons", ActionConstants.Delete, Lessons.Resource),

        new("View Lesson Materials",   ActionConstants.View, LessonMaterials.Resource, IsBasic: true),
        new("Manage Lesson Materials", "Manage",              LessonMaterials.Resource),
    ];
}
