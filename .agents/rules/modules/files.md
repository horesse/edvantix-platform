# Module: Files

Presigned-URL file lifecycle (upload → finalize → serve → delete) shared by Catalog images, Chat attachments, avatars. Module `Order = 350` (loads before consumer modules).

**Entities / DbContext:** `FileAsset` (aggregate, soft-deletable): status `PendingUpload → Available | Quarantined`, `Visibility` (Public/Private), `ScanStatus`. `FilesDbContext`. Publishes `FileFinalizedIntegrationEvent`.
**Areas:** RequestUploadUrl, FinalizeUpload, GetFileDownloadUrl/Metadata, ChangeVisibility, Delete/Restore, ListMy/Shared/Trashed. Purge jobs (orphaned hourly, deleted daily). Full list: `Features/v1/` or `/scalar`. Storage mechanics: `storage.md`.

## Gotchas

- **Presigned flow** — never stream uploads through the API. RequestUploadUrl validates category/extension/size + quota **pre-check** and persists a `PendingUpload`; client uploads directly to storage; **FinalizeUpload debits the quota** (not at request time) and flips to Available/Quarantined.
- **Storage quota per plan** — the pre-check in RequestUploadUrl is `IQuotaService.CheckAsync(QuotaResource.StorageBytes, declaredSize)`; over the plan limit (`QuotaOptions.Plans[<plan>]:StorageBytes`, 2 GiB `free` / 50 GiB `pro`) → **HTTP 507**, no URL issued. FinalizeUpload records the actual bytes, `PurgeDeletedFilesJob` refunds on hard purge. Enforced only when `QuotaOptions.Enabled` (true in `appsettings.json`, false in Development).
- **Category ↔ owner-type binding** — `FileCategoryOptions.OwnerTypes` (config, `Files:Categories:<name>:OwnerTypes`). When set, the binding is symmetric and enforced by `FileCategoryPolicy` in RequestUploadUrl: those owner types may **only** upload through a category naming them, and the category rejects every other owner type. Used so `OwnerType=LessonMaterial` gets its own curated whitelist (`LessonMaterial` category — docs/images/audio/archives, **no video containers**, 25 MiB) that a caller can't sidestep via `Document`. Leave `OwnerTypes` empty for a general category (Image/Document/Archive).
- **`FileAccessPolicyRegistry`** resolves `IFileAccessPolicy` by **OwnerType** — case-insensitive, **closed by default** (unknown OwnerType → forbidden), **last-write-wins** on duplicates (intentional, for test substitution). Each owning module registers its own policy in its `ConfigureServices` (Catalog/Tickets load after Files). Files ships `DefaultUploaderOnlyPolicy` for built-in OwnerTypes `"MyFiles"`/`"User"`.
- `CanChangeVisibilityAsync` defaults to the delete rule (uploader-only); domain-bound files (e.g. product images) may override to forbid visibility flips.
- Tenant scoping is implicit via `BaseDbContext` (no explicit `TenantId` on `FileAsset`).

To support uploads for a new owner type: implement `IFileAccessPolicy`, register it in the owning module, and use that OwnerType in RequestUploadUrl.
