using FluentValidation;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.AddLessonMaterial;

public sealed class AddLessonMaterialCommandValidator : AbstractValidator<AddLessonMaterialCommand>
{
    public AddLessonMaterialCommandValidator(IOptions<CurriculumOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var allowedVideoHosts = options.Value.VideoMaterialAllowedHosts ?? [];

        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).MaximumLength(2048);

        RuleFor(x => x)
            .Must(x => x.FileId.HasValue ^ !string.IsNullOrWhiteSpace(x.Url))
            .WithMessage("Exactly one of FileId or Url must be set.");

        // Link-backed kinds carry an external URL; file-backed kinds attach a stored file.
        RuleFor(x => x.FileId)
            .Null()
            .When(x => x.Kind is MaterialKind.Video or MaterialKind.Link)
            .WithMessage("Video and Link materials are external links — set Url, not FileId.");

        RuleFor(x => x.Url)
            .Null()
            .When(x => x.Kind is MaterialKind.File or MaterialKind.Presentation)
            .WithMessage("File and Presentation materials are uploaded files — set FileId, not Url.");

        // Any URL-backed material must be a well-formed absolute http(s) URL.
        RuleFor(x => x.Url!)
            .Must(BeAbsoluteHttpUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.Url))
            .WithMessage("Url must be an absolute http(s) URL.");

        // A class recording must sit on an allow-listed external host — no direct video upload.
        RuleFor(x => x.Url!)
            .Must(url => IsAllowedVideoHost(url, allowedVideoHosts))
            .When(x => x.Kind == MaterialKind.Video && !string.IsNullOrWhiteSpace(x.Url) && BeAbsoluteHttpUrl(x.Url!))
            .WithMessage($"Video materials must link to an allowed host ({string.Join(", ", allowedVideoHosts)}).");
    }

    private static bool BeAbsoluteHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool IsAllowedVideoHost(string url, IReadOnlyCollection<string> allowedHosts)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host;
        return allowedHosts.Any(h =>
            host.Equals(h, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));
    }
}
