using FSH.Framework.Shared.Persistence;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using FSH.Modules.StudyGroups.Data;
using FSH.Modules.StudyGroups.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.SearchStudyGroups;

public sealed class SearchStudyGroupsQueryHandler(StudyGroupsDbContext dbContext)
    : IQueryHandler<SearchStudyGroupsQuery, PagedResponse<StudyGroupDto>>
{
    public async ValueTask<PagedResponse<StudyGroupDto>> Handle(SearchStudyGroupsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var q = dbContext.StudyGroups.AsNoTracking().AsQueryable();

        if (query.CourseId is { } courseId)
        {
            q = q.Where(g => g.CourseId == courseId);
        }

        if (query.Status is { } status)
        {
            q = q.Where(g => g.Status == status);
        }

        if (query.Format is { } format)
        {
            q = q.Where(g => g.Format == format);
        }

        if (query.TeacherId is { } teacherId)
        {
            // Matches either the denormalized primary teacher or anyone on the staffing roster
            // (see StudyGroup.PrimaryTeacherId remarks — the two are independent).
            q = q.Where(g => g.PrimaryTeacherId == teacherId
                || dbContext.GroupTeachers.Any(t => t.StudyGroupId == g.Id && t.TeacherId == teacherId));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(g => EF.Functions.ILike(g.Code, $"%{term}%") || EF.Functions.ILike(g.Name, $"%{term}%"));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var groups = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Active-enrollment counts via a grouped subquery instead of Include — a search result page
        // shouldn't drag in every enrollment row for every group (see StudyGroupMappings.ToDto).
        var groupIds = groups.Select(g => g.Id).ToList();
        var counts = await dbContext.GroupEnrollments
            .AsNoTracking()
            .Where(e => groupIds.Contains(e.StudyGroupId)
                && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Paused))
            .GroupBy(e => e.StudyGroupId)
            .Select(g => new { StudyGroupId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var countByGroup = counts.ToDictionary(x => x.StudyGroupId, x => x.Count);

        return new PagedResponse<StudyGroupDto>
        {
            Items = groups.Select(g => g.ToDto(countByGroup.GetValueOrDefault(g.Id))).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
        };
    }

    private static IQueryable<StudyGroup> ApplySort(IQueryable<StudyGroup> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "NAME" => desc ? q.OrderByDescending(g => g.Name) : q.OrderBy(g => g.Name),
            "STARTDATE" => desc ? q.OrderByDescending(g => g.StartDate) : q.OrderBy(g => g.StartDate),
            "STATUS" => desc ? q.OrderByDescending(g => g.Status) : q.OrderBy(g => g.Status),
            _ => desc ? q.OrderByDescending(g => g.Code) : q.OrderBy(g => g.Code),
        };
    }
}
