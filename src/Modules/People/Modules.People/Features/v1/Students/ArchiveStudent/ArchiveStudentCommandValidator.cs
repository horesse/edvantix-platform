using FluentValidation;
using FSH.Modules.People.Contracts.v1.Students;

namespace FSH.Modules.People.Features.v1.Students.ArchiveStudent;

public sealed class ArchiveStudentCommandValidator : AbstractValidator<ArchiveStudentCommand>
{
    public ArchiveStudentCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
    }
}
