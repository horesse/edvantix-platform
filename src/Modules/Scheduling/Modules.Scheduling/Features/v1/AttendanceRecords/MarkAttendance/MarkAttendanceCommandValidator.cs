using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Attendance;

namespace FSH.Modules.Scheduling.Features.v1.AttendanceRecords.MarkAttendance;

public sealed class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand>
{
    public MarkAttendanceCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Marks).NotEmpty();
        RuleForEach(x => x.Marks).ChildRules(mark =>
        {
            mark.RuleFor(m => m.StudentId).NotEmpty();
            mark.RuleFor(m => m.Status).IsInEnum();
            mark.RuleFor(m => m.Comment).MaximumLength(1024);
        });
    }
}
