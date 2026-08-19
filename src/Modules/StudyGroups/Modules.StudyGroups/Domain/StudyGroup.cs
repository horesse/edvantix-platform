using System.Net;
using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.StudyGroups.Contracts.Dtos;

namespace FSH.Modules.StudyGroups.Domain;

/// <summary>
/// A study group — binds a <see cref="CourseId"/> (Curriculum), a primary teacher and a roster of
/// students for a period. Named <c>StudyGroup</c>, not <c>Group</c>, because <c>Identity.Group</c>
/// already owns that name for access groups (see ADR-005).
/// <para>
/// Owns <see cref="Enrollments"/> and <see cref="Teachers"/> as owned collections — unlike
/// Curriculum's flat independent-aggregate model (see Curriculum.md), enrollment writes are
/// naturally bounded by <see cref="Capacity"/> (tens, not hundreds of rows), so loading the whole
/// roster with the group and mutating through the aggregate (same shape as
/// <c>Student.GuardianLinks</c>) keeps the "one active enrollment per student" and "capacity"
/// invariants in one transaction without a second query round-trip.
/// </para>
/// </summary>
public sealed class StudyGroup : AggregateRoot<Guid>, ISoftDeletable
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Guid CourseId { get; private set; }

    /// <summary>Denormalized "who teaches this group" for search/filtering. Independent of the
    /// <see cref="Teachers"/> roster (no auto-sync) — a school may also add the primary teacher as
    /// a <see cref="Domain.GroupTeacher"/> row with <see cref="TeacherRole.Primary"/> for
    /// scheduling/substitution purposes, but nothing enforces the two stay in lockstep; there is no
    /// command in the contracts to keep them synced (see docs/02 Модули/StudyGroups.md → Контракты).</summary>
    public Guid PrimaryTeacherId { get; private set; }

    public GroupFormat Format { get; private set; }
    public int Capacity { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public StudyGroupStatus Status { get; private set; }

    // Written by the future Chat module's StudyGroupCreated subscriber via reflection — not yet
    // wired here (see docs/04 Задачи/Задачи · Доработки каркаса.md → Chat).
#pragma warning disable S1144 // EF Core writes this setter via reflection
    public Guid? ChatChannelId { get; private set; }
#pragma warning restore S1144
    public string? MeetingUrl { get; private set; }
    public Guid? RoomId { get; private set; }
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private readonly List<GroupEnrollment> _enrollments = [];
    public IReadOnlyList<GroupEnrollment> Enrollments => _enrollments;

    private readonly List<GroupTeacher> _teachers = [];
    public IReadOnlyList<GroupTeacher> Teachers => _teachers;

    /// <summary>Enrollments that currently occupy a roster slot — <see cref="EnrollmentStatus.Active"/>
    /// and <see cref="EnrollmentStatus.Paused"/> both count (a pause defers the "leave" decision,
    /// it does not free the seat); <see cref="EnrollmentStatus.Left"/>/<see cref="EnrollmentStatus.Completed"/>
    /// do not.</summary>
    public int ActiveEnrollmentCount => _enrollments.Count(e =>
        e.Status is EnrollmentStatus.Active or EnrollmentStatus.Paused);

    private StudyGroup() { }

    public static StudyGroup Create(
        string code,
        string name,
        Guid courseId,
        Guid primaryTeacherId,
        GroupFormat format,
        int capacity,
        DateOnly startDate,
        DateOnly? endDate,
        string? meetingUrl,
        Guid? roomId,
        string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (courseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(courseId));
        }
        if (primaryTeacherId == Guid.Empty)
        {
            throw new ArgumentException("PrimaryTeacherId is required.", nameof(primaryTeacherId));
        }
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");
        }

        return new StudyGroup
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim(),
            Name = name.Trim(),
            CourseId = courseId,
            PrimaryTeacherId = primaryTeacherId,
            Format = format,
            Capacity = capacity,
            StartDate = startDate,
            EndDate = endDate,
            MeetingUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim(),
            RoomId = roomId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = StudyGroupStatus.Forming,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        string name,
        Guid primaryTeacherId,
        GroupFormat format,
        int capacity,
        DateOnly startDate,
        DateOnly? endDate,
        string? meetingUrl,
        Guid? roomId,
        string? notes)
    {
        EnsureNotFrozen();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (primaryTeacherId == Guid.Empty)
        {
            throw new ArgumentException("PrimaryTeacherId is required.", nameof(primaryTeacherId));
        }
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");
        }
        if (capacity < ActiveEnrollmentCount)
        {
            throw new CustomException(
                $"Cannot set capacity to {capacity}: {ActiveEnrollmentCount} students are already enrolled.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        Name = name.Trim();
        PrimaryTeacherId = primaryTeacherId;
        Format = format;
        Capacity = capacity;
        StartDate = startDate;
        EndDate = endDate;
        MeetingUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim();
        RoomId = roomId;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    // ─── Lifecycle: Forming → Active → Finished, Forming/Active → Cancelled ────────────────

    /// <summary>Requires at least one enrollment — "группа без учеников не запускается"
    /// (see docs/02 Модули/StudyGroups.md → Инварианты). The schedule-template half of that
    /// invariant is deferred: Scheduling does not exist yet.</summary>
    public void Activate()
    {
        if (Status != StudyGroupStatus.Forming)
        {
            throw new CustomException(
                $"Cannot activate a study group in status {Status}.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }
        if (ActiveEnrollmentCount == 0)
        {
            throw new CustomException(
                "Cannot activate a study group with no enrollments.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        Status = StudyGroupStatus.Active;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Active → Finished. Freezes the roster: every <see cref="EnrollmentStatus.Active"/>/
    /// <see cref="EnrollmentStatus.Paused"/> enrollment is marked <see cref="EnrollmentStatus.Completed"/>
    /// so the group's history reads as "who finished the course", not "who never left".</summary>
    public void Finish(DateOnly finishedOn)
    {
        if (Status != StudyGroupStatus.Active)
        {
            throw new CustomException(
                $"Cannot finish a study group in status {Status}.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        foreach (var enrollment in _enrollments.Where(e => e.Status is EnrollmentStatus.Active or EnrollmentStatus.Paused))
        {
            enrollment.Complete();
        }

        Status = StudyGroupStatus.Finished;
        EndDate ??= finishedOn;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Forming/Active → Cancelled — for groups that never ran or were aborted, distinct
    /// from a normal <see cref="Finish"/>. Does not auto-complete enrollments (nothing to complete).</summary>
    public void Cancel(string? reason)
    {
        if (Status is not (StudyGroupStatus.Forming or StudyGroupStatus.Active))
        {
            throw new CustomException(
                $"Cannot cancel a study group in status {Status}.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        Status = StudyGroupStatus.Cancelled;
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : $"{Notes}\n[Cancelled] {reason.Trim()}".Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    // ─── Enrollments ────────────────────────────────────────────────────────────────────

    public GroupEnrollment Enroll(Guid studentId, DateOnly enrolledOn, Guid? tariffId, decimal discountPercent)
    {
        EnsureAcceptsRosterChanges();
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("StudentId is required.", nameof(studentId));
        }
        if (_enrollments.Any(e => e.StudentId == studentId && e.Status != EnrollmentStatus.Left))
        {
            throw new CustomException(
                $"Student {studentId} already has an active enrollment in this group.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }
        if (ActiveEnrollmentCount >= Capacity)
        {
            throw new CustomException(
                $"Study group {Code} is at capacity ({Capacity}).", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        var enrollment = GroupEnrollment.Create(Id, studentId, enrolledOn, tariffId, discountPercent);
        _enrollments.Add(enrollment);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return enrollment;
    }

    public void Unenroll(Guid enrollmentId, DateOnly leftOn, string? reason)
    {
        EnsureAcceptsRosterChanges();
        var enrollment = FindEnrollment(enrollmentId);
        enrollment.MarkLeft(leftOn, reason);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void PauseEnrollment(Guid enrollmentId)
    {
        EnsureAcceptsRosterChanges();
        FindEnrollment(enrollmentId).Pause();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void ResumeEnrollment(Guid enrollmentId)
    {
        EnsureAcceptsRosterChanges();
        FindEnrollment(enrollmentId).Resume();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private GroupEnrollment FindEnrollment(Guid enrollmentId) =>
        _enrollments.FirstOrDefault(e => e.Id == enrollmentId)
            ?? throw new NotFoundException($"Enrollment {enrollmentId} not found in study group {Id}.");

    // ─── Teachers ───────────────────────────────────────────────────────────────────────

    public GroupTeacher AddTeacher(Guid teacherId, TeacherRole role)
    {
        if (teacherId == Guid.Empty)
        {
            throw new ArgumentException("TeacherId is required.", nameof(teacherId));
        }
        if (_teachers.Any(t => t.TeacherId == teacherId))
        {
            throw new CustomException(
                $"Teacher {teacherId} is already on this group's roster.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        var teacher = GroupTeacher.Create(Id, teacherId, role);
        _teachers.Add(teacher);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return teacher;
    }

    public void RemoveTeacher(Guid teacherId)
    {
        var teacher = _teachers.FirstOrDefault(t => t.TeacherId == teacherId)
            ?? throw new NotFoundException($"Teacher {teacherId} is not on study group {Id}'s roster.");
        _teachers.Remove(teacher);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Appends an operational flag to <see cref="Notes"/> — used by the People/Curriculum
    /// integration-event subscriptions (TeacherDeactivated → group left without a teacher,
    /// CourseArchived → forming group whose course can no longer be activated against). Idempotent:
    /// a repeat delivery of the same event does not duplicate the line.</summary>
    internal void AddSystemFlag(string flag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flag);
        var line = $"[!] {flag.Trim()}";
        if (Notes is not null && Notes.Contains(line, StringComparison.Ordinal))
        {
            return;
        }

        Notes = string.IsNullOrWhiteSpace(Notes) ? line : $"{Notes}\n{line}";
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    // ─── Guards ─────────────────────────────────────────────────────────────────────────

    /// <summary>"Finished замораживает состав: изменения запрещены" — applied to the whole group
    /// record, not just the roster (see docs/02 Модули/StudyGroups.md → Инварианты).</summary>
    private void EnsureNotFrozen()
    {
        if (Status is StudyGroupStatus.Finished or StudyGroupStatus.Cancelled)
        {
            throw new CustomException(
                $"Study group {Code} is {Status} and can no longer be edited.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }
    }

    private void EnsureAcceptsRosterChanges()
    {
        if (Status is not (StudyGroupStatus.Forming or StudyGroupStatus.Active))
        {
            throw new CustomException(
                $"Study group {Code} is {Status}; roster changes are not allowed.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }
    }
}
