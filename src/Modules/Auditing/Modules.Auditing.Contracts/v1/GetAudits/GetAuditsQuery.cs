using FSH.Framework.Shared.Persistence;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Auditing.Contracts.v1.GetAudits;

public sealed class GetAuditsQuery : IPagedQuery, IQuery<PagedResponse<AuditSummaryDto>>
{
    public int? PageNumber { get; set; }

    public int? PageSize { get; set; }

    public string? Sort { get; set; }

    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    public string? TenantId { get; set; }

    public string? UserId { get; set; }

    public AuditEventType? EventType { get; set; }

    /// <summary>Hide a single event type (e.g. <c>Activity</c> to drop system-level HTTP noise).
    /// Applied as a not-equals filter so paging + totals stay correct.</summary>
    public AuditEventType? ExcludeEventType { get; set; }

    public AuditSeverity? Severity { get; set; }

    public AuditTag? Tags { get; set; }

    public string? Source { get; set; }

    /// <summary>Entity-change history filter: the audited entity's CLR type name
    /// (<c>EntityChangeEventPayload.EntityName</c>), e.g. <c>Student</c>. Non-entity-change
    /// events carry no <c>entityName</c> and are excluded when this is set.</summary>
    public string? EntityName { get; set; }

    /// <summary>Entity-change history filter: the unified key
    /// (<c>EntityChangeEventPayload.Key</c>), e.g. <c>Id:3f2504e0-...</c> or a composite
    /// <c>TenantId:1|UserId:42</c>. Requires <see cref="EntityName"/>.</summary>
    public string? EntityKey { get; set; }

    public string? CorrelationId { get; set; }

    public string? TraceId { get; set; }

    public string? Search { get; set; }
}