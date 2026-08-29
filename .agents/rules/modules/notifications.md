# Module: Notifications

Per-user in-app inbox (bell icon) driven by cross-module integration events, with live SignalR push. Module `Order = 750` (**before Chat 800** so its handlers are registered before Chat publishes).

**Entities / DbContext:** `Notification` (aggregate: `UserId`, `Type`, `Title`/`Body`/`Link`, `Source`, `MetadataJson`, `ReadAtUtc`). `NotificationsDbContext`. Consumes integration events from other modules (e.g. Chat's `MentionedInChannelIntegrationEvent`).
**Areas:** List, GetUnreadCount, MarkRead, MarkAllRead. Full list: `Features/v1/` or `/scalar`.

**Templating:** `Templating/` — in-module `{{token}}` substitution only (no engine in `BuildingBlocks/Mailing`; don't add one there — rule 4). `NotificationTypes` = stable `Notification.Type` keys **and** template keys; `INotificationTemplateCatalog` holds one `NotificationTemplate` per type; `INotificationTemplateRenderer.Render(key, tokens)` → `RenderedNotification`. Token values are HTML-escaped only in `EmailHtmlBodyTemplate`. Missing token → empty + log; unknown template key → `KeyNotFoundException` (deploy bug). Both are singletons. A new emailed type = add a `NotificationTypes` const + a catalogue entry (tests assert the two stay in sync).

## Gotchas

- **It's a consumer.** New notification types come from **handling another module's integration event** (`AddIntegrationEventHandlers`), not from new endpoints. The handler renders copy via `INotificationTemplateRenderer`, writes an inbox row **and** pushes `"NotificationCreated"` to SignalR group `user:{userId}` via `IHubContext<AppHub>`.
- **Order matters** — Notifications (750) must load before any publisher whose events it consumes (Chat 800). If a new module publishes events Notifications should react to, mind the `Order`.
- In-memory bus runs handlers **synchronously in the publisher's request scope** — keep the handler minimal; an exception surfaces to the originating request. See `eventing.md`.
- Inbox rows are **denormalized** (Title/Body/Link/MetadataJson copied in) so rendering never calls back into the source module. `MarkRead` is idempotent (`ReadAtUtc ??= now`).
