namespace FSH.Modules.Scheduling.Services;

internal static class SchedulingDefaults
{
    /// <summary>Weeks ahead the generator keeps sessions materialized. See
    /// docs/04 Задачи/Открытые вопросы.md → Scheduling → "Горизонт генерации".</summary>
    public const int DefaultHorizonWeeks = 8;

    /// <summary>Days ahead <c>GetTeacherWorkloadQuery</c> looks when <c>To</c> is omitted — a
    /// near-term "how busy is this teacher" view, deliberately much shorter than
    /// <see cref="DefaultHorizonWeeks"/>.</summary>
    public const int DefaultWorkloadWindowDays = 7;
}
