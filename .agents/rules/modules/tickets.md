# Module: Tickets

Support ticket lifecycle with comments. Module `Order = 700`.

**Entities / DbContext:** `Ticket` (aggregate, soft-deletable, state machine) + `TicketComment`. `TicketsDbContext`. `TicketStatus`/`TicketPriority` enums in Contracts; domain events internal.
**Areas:** Create, Assign, Resolve, Reopen, Restore, AddComment, ListComments, GetById, Search, ListTrashed. Full list: `Features/v1/` or `/scalar`.

## Gotchas

- **State machine** (`Domain/Ticket.cs`): `Open → InProgress → Resolved → Closed`. Illegal transitions throw **`CustomException` with `HttpStatusCode.Conflict` (409)** — not a generic 400. Assigning auto-starts (Open→InProgress); unassigning an InProgress ticket reverts to Open; creating with an assignee starts at InProgress. Closed tickets reject comments/resolve until reopened. Keep all transition guards in the aggregate.
- Soft-delete/restore/trash pattern is identical to Catalog (filtered unique indexes — see `modules/catalog.md`).
- Endpoints are mapped on the bare `api/v{version}` group (no `/tickets` sub-path); literal routes precede `{ticketId:guid}`.
- **Context links** (`RelatedStudentId`/`RelatedStudyGroupId`/`RelatedInvoiceId` on `Ticket`): opaque `Guid?` — Tickets does **not** reference People/StudyGroups/Payments; no existence check, validator only rejects `Guid.Empty`. `Ticket.SetRelatedContext` is allowed at any status (metadata, not lifecycle). `PUT /tickets/{id}` is full-replace — an omitted link is cleared. Partial indexes (`... IS NOT NULL`) back the `SearchTicketsQuery` filters.
- **Classification** (`Category` + `Audience` on `Ticket`, string-converted enums): `TicketClassificationDefaults.AudienceFor(category)` maps `Technical → Platform`, everything else `→ School`. Create/Update take `Category` + nullable `Audience` — null means "derive from category". `Ticket.SetClassification` allowed at any status. `SearchTicketsQuery` filters by both. Default **assignee** per category is NOT done (needs per-tenant staff config). Migration `TicketClassification` backfills existing rows with `General`/`School` (the string converter round-trips by name — `""` would fail to read).
- **Attachments**: no command field — clients use the generic Files endpoints with `ownerType=Ticket`, `ownerId=ticketId`. `TicketFileAccessPolicy` (`Authorization/`, registered as `IFileAccessPolicy`) is the only gate: attach/read = reporter or assignee; delete = uploader. `currentUserId` is a string → `Guid.TryParse` against `ReporterUserId`/`AssignedToUserId`. Module now references `Files.Contracts` (not `Identity.Contracts` — kept the policy DB-only, no permission check).
