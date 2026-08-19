using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Attendance;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.AttendanceRecords.MarkAttendance;

public sealed class MarkAttendanceCommandHandler(SchedulingDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<MarkAttendanceCommand, Unit>
{
    public async ValueTask<Unit> Handle(MarkAttendanceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool sessionExists = await dbContext.Sessions
            .AnyAsync(s => s.Id == command.SessionId, cancellationToken)
            .ConfigureAwait(false);
        if (!sessionExists)
        {
            throw new NotFoundException($"Session {command.SessionId} not found.");
        }

        var existing = await dbContext.Attendances
            .Where(a => a.SessionId == command.SessionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingByStudent = existing.ToDictionary(a => a.StudentId);

        var markedByUserId = currentUser.GetUserId().ToString();

        foreach (var mark in command.Marks)
        {
            if (existingByStudent.TryGetValue(mark.StudentId, out var attendance))
            {
                attendance.Mark(mark.Status, mark.Comment, markedByUserId);
            }
            else
            {
                var created = Attendance.CreateDefault(command.SessionId, mark.StudentId);
                created.Mark(mark.Status, mark.Comment, markedByUserId);
                dbContext.Attendances.Add(created);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
