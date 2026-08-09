---
tags: [модуль, каркас, identity]
статус: реализован
порядок: 100
схема: identity
---

# Identity

← [[Edvantix]] · [[Карта модулей]] · задачи: [[Задачи · Доработки каркаса]]

> ✅ Реализован · порядок `100` · схема `identity`

## Назначение

Аутентификация и авторизация: пользователи, роли, права, группы доступа, сессии,
двухфакторная аутентификация, имперсонация. Предметных сущностей школы здесь нет —
ученики и преподаватели живут в [[People]] ([[ADR-003 People как отдельный модуль]]).

Требование «гибкая ролевая система» закрывается этим модулем без доработок ядра.

## Домен

| Сущность | Назначение |
|---|---|
| `FshUser` | пользователь ASP.NET Identity |
| `FshRole`, `FshRoleClaim` | роль и её права (claim `Permissions.{Resource}.{Action}`) |
| `Group`, `GroupRole`, `UserGroup` | **группа доступа** — массовое назначение ролей |
| `UserSession` | активная сессия с устройством и адресом |
| `ImpersonationGrant` | разрешение на вход под другим пользователем |
| `PasswordHistory` | история паролей для запрета повтора |

> [!warning] `Group` здесь — группа доступа, не учебная
> Учебная группа — `StudyGroup` в [[StudyGroups]].
> Разбор — [[ADR-005 Именование Group и StudyGroup]].

Инфраструктура: `JwtOptions`, `ConfigureJwtBearerOptions`, `PasswordPolicyOptions`,
`PermissionSet` (кэш прав), `RolePermissionSyncer` + hosted service,
`PathAwareAuthorizationHandler`, `RequiredPermissionAuthorizationHandler`.

## Контракты

`Modules.Identity.Contracts`

### Команды

| Область | Команды |
|---|---|
| Users | `RegisterUserCommand` `UpdateUserCommand` `DeleteUserCommand` `ToggleUserStatusCommand` `SetProfileImageCommand` |
| Users · почта | `ConfirmEmailCommand` `AdminConfirmEmailCommand` `ResendConfirmationEmailCommand` |
| Users · пароль | `ChangePasswordCommand` `ForgotPasswordCommand` `ResetPasswordCommand` |
| Users · роли | `AssignUserRolesCommand` |
| Roles | `UpsertRoleCommand` `DeleteRoleCommand` `UpdatePermissionsCommand` |
| Groups | `CreateGroupCommand` `UpdateGroupCommand` `DeleteGroupCommand` `AddUsersToGroupCommand` `RemoveUserFromGroupCommand` |
| Sessions | `RevokeSessionCommand` `RevokeAllSessionsCommand` `AdminRevokeSessionCommand` `AdminRevokeAllSessionsCommand` |
| Tokens | `GenerateTokenCommand` `RefreshTokenCommand` |
| 2FA | `EnrollTwoFactorCommand` `VerifyEnrollTwoFactorCommand` `DisableTwoFactorCommand` |
| Impersonation | `StartImpersonationCommand` `EndImpersonationCommand` `RevokeImpersonationGrantCommand` |

### Запросы

`GetUserQuery` · `GetUsersQuery` · `SearchUsersQuery` · `GetUserRolesQuery` ·
`GetUserGroupsQuery` · `GetCurrentUserProfileQuery` · `GetCurrentUserPermissionsQuery` ·
`GetRoleQuery` · `GetRolesQuery` · `GetRoleWithPermissionsQuery` ·
`GetPermissionCatalogQuery` · `GetGroupByIdQuery` · `GetGroupsQuery` ·
`GetGroupMembersQuery` · `GetMySessionsQuery` · `GetUserSessionsQuery` ·
`GetTenantSessionsQuery` · `GetImpersonationGrantsQuery`

### DTO

`UserDto` · `UserRoleDto` · `UserSessionDto` · `RoleDto` · `GroupDto` ·
`GroupMemberDto` · `TokenDto` · `TokenResponse` · `PermissionCatalogEntryDto` ·
`PasswordExpiryStatusDto` · `TwoFactorEnrollmentResponse` · `ImpersonationGrantDto` ·
`ImpersonationResponse`

### Публикуемые события

`UserRegisteredIntegrationEvent` · `TokenGeneratedIntegrationEvent`

Внутренние доменные: `UserActivatedEvent`, `UserDeactivatedEvent`,
`UserRoleAssignedEvent`, `PasswordChangedEvent`, `SessionRevokedEvent`.

### Сервисы

`ICurrentUserService` · `IRequestContextService` · `IIdentityService` ·
`IUserService` · `IUserProfileService` · `IUserRegistrationService` ·
`IUserStatusService` · `IUserRoleService` · `IUserPermissionService` ·
`IUserPasswordService` · `IPasswordHistoryService` · `IPasswordExpiryService` ·
`IRoleService` · `IGroupRoleService` · `ISessionService` · `ITokenService` ·
`IImpersonationGrantService`

`ICurrentUserService` и `IUserPermissionService` — самые востребованные другими
модулями: текущий пользователь и проверка права в обработчике.

## Права

`IdentityPermissions`, ресурсы: `Users`, `UserRoles`, `Roles`, `RoleClaims`,
`Sessions`, `Groups`, `Impersonation`.

Действия `Users`: `View` (basic) `Search` `Create` `Update` `Delete` `Export`
`ManageRoles` `Impersonate` `ConfirmEmail`.

Реестр прав — `PermissionConstants` в `BuildingBlocks/Shared`; каждый модуль
регистрирует свои в `ConfigureServices`. Механика — [[Модель прав доступа]].

## HTTP API

```
POST   /api/v1/token                            вход
POST   /api/v1/token/refresh
GET    /api/v1/users                            + CRUD, поиск
POST   /api/v1/users/register
POST   /api/v1/users/{id}/roles
GET    /api/v1/roles                            + CRUD
PUT    /api/v1/roles/{id}/permissions
GET    /api/v1/permissions                      каталог прав
GET    /api/v1/groups                           + CRUD, участники
GET    /api/v1/sessions/mine
POST   /api/v1/sessions/{id}/revoke
POST   /api/v1/two-factor/enroll
POST   /api/v1/impersonation/start
```

## Зависимости

**Ссылается на:** `Multitenancy.Contracts`, `BuildingBlocks` (Core, Persistence, Web,
Caching, Mailing).

**На него ссылаются:** практически все модули — через `ICurrentUserService`
и права.

## Связанное

[[Модель прав доступа]] · [[People]] · [[ADR-005 Именование Group и StudyGroup]] · `.agents/rules/modules/identity.md`
