# Module: Identity

Auth (JWT + ASP.NET Identity), users, roles, permissions, sessions, impersonation, 2FA.

## Service shape

`IUserService` is a **facade** that delegates to focused single-responsibility services — change behavior in the specific service, not the facade:

| Interface | Concern |
|---|---|
| `IUserRegistrationService` | register, external-principal create, email/phone confirm |
| `IUserProfileService` | get/list/count, update profile, image, existence checks |
| `IUserStatusService` | activate/deactivate (`DeleteAsync` == deactivate), audited toggles |
| `IUserRoleService` | role assignment, admin-role guards |
| `IUserPasswordService` | forgot/reset/change password, history + expiry |
| `IUserPermissionService` | effective permissions, cache invalidation |

`ChangePassword`/`Update`/`Delete` etc. flow facade → service → EF/UserManager. `CancellationToken` is `= default` on these interfaces and propagated into EF sinks (note: `UserManager`/`RoleManager` have no CT overloads, so private helpers that only call them don't take one).

## Permission gating footgun

`RequiredPermissionAttribute` implements `FSH.Framework.Shared.Identity.Authorization.IRequiredPermissionMetadata`. **Never let a second/duplicate `IRequiredPermissionMetadata` appear** — it silently disables **all** `.RequirePermission()` gates across the app. Permission constants live in `Shared/Identity/*Permissions.cs`.

## Hosted services (background)

- `RolePermissionSyncHostedService` — best-effort sync of the permission catalog; loops, catches `Exception` *with* an `OperationCanceledException` filter, logs and continues.
- `SessionCleanupHostedService` — hourly expired-session purge; OCE handled by a preceding catch.

These are the model for background loops: stay alive, log with context, never swallow cancellation. See `api-conventions.md`.

## School roles

`SchoolRoleConstants` (`SchoolAdmin`, `Manager`, `Teacher`, `Student`, `Guardian`) are ordinary, non-system roles seeded per non-root tenant — **not** added to `RoleConstants.DefaultRoles` (that list is BuildingBlocks-protected and marks framework roles a school can't edit/delete; these five must stay editable). `SchoolRolePermissions.Resolve(roleName, catalog)` builds each role's bundle by *filtering* `PermissionConstants.All` (SchoolAdmin = all non-root; Manager = same minus non-`View` Identity actions plus `Users.Invite`; Teacher/Student/Guardian = `*.ViewOwn` convention + a short exception list) rather than enumerating permission names — so bundles grow automatically as People/Curriculum/StudyGroups/Scheduling/Payments register permissions in later releases. Two call sites must stay in sync: `IdentityDbInitializer.SeedRolesAsync` (initial seed, skips root) and `RolePermissionSyncer.SyncAsync` (periodic top-up for already-provisioned tenants — required here, not optional, since those five modules don't exist yet).

## Tokens / sessions

Login `POST /api/v1/identity/token/issue` (header `X-FSH-App` enforces the operator/tenant app boundary). Refresh `POST /api/v1/identity/token/refresh` cross-checks subject. Session rows are written best-effort during login — failures log a warning and login still succeeds. Admin can't demote/deactivate the last admin or the root-tenant seed admin (guards in `UserRoleService`/`UserStatusService`).

## Invite-by-e-mail

`InviteUserCommand` → `UserRegistrationService.InviteAsync` (gated by `IdentityPermissions.Users.Invite`, granted only to `Manager`+`SchoolAdmin`). Creates an `FshUser` with a random never-revealed password and `EmailConfirmed = false`, restricted to a `SchoolRoleConstants.All` role (validator rejects free-form roles), then mints a link via `UserManager.GeneratePasswordResetTokenAsync` — the exact same call `ForgotPasswordCommand` uses, no separate token entity. Acceptance reuses `ResetPasswordCommand` as-is: `UserPasswordService.ResetPasswordAsync` sets `EmailConfirmed = true` whenever it was `false` on success, since a successful reset already proves mailbox control and the two flows share one token purpose — there is no "was this an invite" flag anywhere, and none should be added. `UserRegisteredIntegrationEvent` is deliberately **not** published for invited users (would double up with `UserRegisteredEmailHandler`'s welcome email, which contradicts the invite's "set your password" message). Binding the invited user to a `Guardian`/`Student` is a separate People call (`.../link-user`) made by the caller — Identity cannot call People (module boundary). Full rationale: docs/02 Модули/Identity.md → "Приглашение по e-mail".

## Tests

`Identity.Tests` is the largest unit suite. When asserting a forwarded `CancellationToken`, assert the specific token (see `testing.md`). DB-touching behavior (role assignment past `CreateAsync`, `EmailConfirmed` flip, token single-use) is covered in `Integration.Tests` instead — `UserManager<FshUser>` substituted in a unit test can't exercise EF Core Identity's real duplicate-email or security-stamp mechanics.
