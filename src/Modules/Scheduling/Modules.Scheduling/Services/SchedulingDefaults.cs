namespace FSH.Modules.Scheduling.Services;

internal static class SchedulingDefaults
{
    /// <summary>Weeks ahead the generator keeps sessions materialized. See
    /// docs/04 Задачи/Открытые вопросы.md → Scheduling → "Горизонт генерации".</summary>
    public const int DefaultHorizonWeeks = 8;
}
