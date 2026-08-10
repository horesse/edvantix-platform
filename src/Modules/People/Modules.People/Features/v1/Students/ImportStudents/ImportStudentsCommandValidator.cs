using FluentValidation;
using FSH.Modules.People.Contracts.v1.Students;

namespace FSH.Modules.People.Features.v1.Students.ImportStudents;

public sealed class ImportStudentsCommandValidator : AbstractValidator<ImportStudentsCommand>
{
    public ImportStudentsCommandValidator()
    {
        RuleFor(x => x.CsvContent).NotEmpty();
    }
}
