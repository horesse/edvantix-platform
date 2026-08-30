# Module: Billing

Plans, subscriptions, usage metering, monthly invoicing. **Manual payment marking — no payment provider.** Module `Order = 500`.

**Entities / DbContext:** `BillingPlan`, `Subscription`, `Invoice` (+ `InvoiceLineItem`), `UsageSnapshot`. **`BillingDbContext : DbContext`** (NOT `BaseDbContext`) — billing lives in the main DB with an explicit `TenantId` column for cross-tenant admin visibility, filtered in query services. Contracts = DTOs; `IBillingService`/`IUsageReporter` are internal.
**Areas:** Plans, Subscriptions, Invoices (generate/issue/mark-paid/void), Usage (capture/get). Monthly invoice job (`5 0 1 * *`). Full list: `Features/v1/` or `/scalar`.

## Gotchas

- **`BillingPlan` is `IGlobalEntity`** — platform-wide catalogue rows, **not tenant-scoped** (opts out of tenant isolation). A plan's `Key` matches the quota config key (e.g. `"pro"`): limits come from `QuotaOptions`, prices/overage from the plan.
- **Seeded plan keys are load-bearing** — `free` / `pro` / `pro-annual` (`BillingDbInitializer`). `QuotaOptions.Plans` and existing subscriptions key off them, so rename the school-facing `Name`/`Description` (blurb, ≤512, nullable, on Create/Update commands + `BillingPlanDto`), never the key.
- **`BillingDbContext` is a plain `DbContext`** — tenant filtering is done explicitly in query services, not by the `BaseDbContext` auto-filter. Don't assume the global tenant filter applies here.
- **Invoice state machine** — `Draft → Issued → Paid | Void`. Line items only addable in Draft; a Paid invoice can't be voided; totals recompute on add; Issue defaults due = +14 days.
- **Usage metering is idempotent** — `IUsageReporter.CaptureForPeriodAsync` reads `IQuotaService` and persists one `UsageSnapshot` per `QuotaResource` per (tenant, period), so invoicing math is reproducible even after a mid-period plan change.
- **Domain quota gauges** — `QuotaResource` (`src/BuildingBlocks/Shared/Quota`) carries `ActiveStudents` / `ActiveTeachers` / `StudyGroups` / `MonthlySessions` alongside the infra ones. Each is a gauge fed by an `internal sealed IQuotaGaugeProvider` in the owning module (People/StudyGroups/Scheduling), pattern = Identity's `UserCountQuotaGaugeProvider` (tenant-scoped DbContext, `IgnoreQueryFilters()` + explicit `EF.Property<string>(x,"TenantId")`). **Only append to the enum** — it persists as `int` in `UsageSnapshots` and `BillingPlan` overage rates. Per-plan numeric limits live in `QuotaOptions.Plans`, not the DB.
- **Soft plan-limit block** — create handlers (`CreateStudent`/`CreateTeacher`/`CreateStudyGroup`/`CreateSession`) call `IQuotaService.EnsureHeadroomAsync(tenantId, resource, amount)` (`src/BuildingBlocks/Quota`) before persisting; over-limit → `QuotaExceededException` → **HTTP 402**, reads stay open. It only calls `CheckAsync` (no counter mutation — gauges are read live). Restore/reactivate and bulk generation (`GenerateSessions`, `ImportStudents`) are deliberately not gated. `QuotaOptions.Enabled` is `true` in `appsettings.json`, `false` in `appsettings.Development.json` (integration suite runs as Development).
