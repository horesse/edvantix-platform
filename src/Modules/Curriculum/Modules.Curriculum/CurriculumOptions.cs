namespace FSH.Modules.Curriculum;

/// <summary>
/// Curriculum module configuration. Bound from the <c>Curriculum</c> section of appsettings.json.
/// </summary>
public sealed class CurriculumOptions
{
    /// <summary>
    /// Registrable domains accepted for a <c>MaterialKind.Video</c> lesson material's URL. Class
    /// recordings are linked from an external host that already handles transcoding and adaptive
    /// streaming — never uploaded directly (gigabytes on MinIO with no streaming is expensive and
    /// clumsy; see docs/04 Задачи/Открытые вопросы.md → «Хранение видео»). The match is on the URL
    /// host, case-insensitive, and also succeeds for any sub-domain of a listed entry
    /// (<c>www.youtube.com</c> matches <c>youtube.com</c>).
    /// </summary>
    public List<string> VideoMaterialAllowedHosts { get; set; } =
    [
        "youtube.com",
        "youtu.be",
        "vimeo.com",
        "rutube.ru",
        "vk.com",
        "vkvideo.ru",
        "dzen.ru",
    ];
}
