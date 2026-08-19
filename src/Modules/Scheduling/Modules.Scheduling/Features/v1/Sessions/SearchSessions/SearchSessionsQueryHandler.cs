using FSH.Framework.Shared.Persistence;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.SearchSessions;

public sealed class SearchSessionsQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<SearchSessionsQuery, PagedResponse<SessionDto>>
{
    public async ValueTask<PagedResponse<SessionDto>> Handle(SearchSessionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var q = dbContext.Sessions.AsNoTracking().AsQueryable();

        if (query.StudyGroupId is { } studyGroupId)
        {
            q = q.Where(s => s.StudyGroupId == studyGroupId);
        }

        if (query.TeacherId is { } teacherId)
        {
            q = q.Where(s => s.TeacherId == teacherId);
        }

        if (query.RoomId is { } roomId)
        {
            q = q.Where(s => s.RoomId == roomId);
        }

        if (query.From is { } from)
        {
            q = q.Where(s => s.StartUtc >= from);
        }

        if (query.To is { } to)
        {
            q = q.Where(s => s.StartUtc <= to);
        }

        if (query.Status is { } status)
        {
            q = q.Where(s => s.Status == status);
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var sessions = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<SessionDto>
        {
            Items = sessions.Select(s => s.ToDto()).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
        };
    }

    private static IQueryable<Session> ApplySort(IQueryable<Session> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "STATUS" => desc ? q.OrderByDescending(s => s.Status) : q.OrderBy(s => s.Status),
            _ => desc ? q.OrderByDescending(s => s.StartUtc) : q.OrderBy(s => s.StartUtc),
        };
    }
}
