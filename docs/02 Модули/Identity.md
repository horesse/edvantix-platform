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

### Роли школы

`SchoolRoleConstants` (`SchoolAdmin`, `Manager`, `Teacher`, `Student`, `Guardian`) —
обычные, не системные роли, сидируемые для каждого нетроевого тенанта
(`IdentityDbInitializer.SeedRolesAsync`) и донасыщаемые новыми правами по мере
регистрации новых модулей (`RolePermissionSyncer.SyncAsync`). В отличие от
`Admin`/`Basic` (`RoleConstants.DefaultRoles`, защищённый файл BuildingBlocks),
школа может их редактировать и удалять как любую свою роль. Бандл прав каждой
роли строится `SchoolRolePermissions.Resolve` фильтром по каталогу
`PermissionConstants.All`, а не явным перечислением — растёт сам по мере того,
как [[People]], [[Curriculum]], [[StudyGroups]], [[Scheduling]], [[Payments]]
регистрируют свои права. Подробности решений — [[Задачи · Доработки каркаса]].

## Контракты

`Modules.Identity.Contracts`

### Команды

| Область | Команды |
|---|---|
| Users | `RegisterUserCommand` `InviteUserCommand` `UpdateUserCommand` `DeleteUserCommand` `ToggleUserStatusCommand` `SetProfileImageCommand` |
| Users · почта | `ConfirmEmailCommand` `AdminConfirmEmailCommand` `ResendConfirmationEmailCommand` |
| Users · пароль | `ChangePasswordCommand` `ForgotPasswordCommand` `ResetPasswordCommand` |
| Users · роли | `AssignUserRolesCommand` |
| Roles | `UpsertRoleCommand` `DeleteRoleCommand` `UpdatePermissionsCommand` |
| Groups | `CreateGroupCommand` `UpdateGroupCommand` `DeleteGroupCommand` `AddUsersToGroupCommand` `RemoveUserFromGroupCommand` |
| Sessions | `RevokeSessionCommand` `RevokeAllSessionsCommand` `AdminRevokeSessionCommand` `AdminRevokeAllSessionsCommand` |
| Tokens | `GenerateTokenCommand` `RefreshTokenCommand` |
| 2FA | `EnrollTwoFactorCommand` `VerifyEnrollTwoFactorCommand` `DisableTwoFactorCommand` |
| Impersonation | `StartImpersonationCommand` `EndImpersonationCommand` `RevokeImpersonationGrantCommand` |

### Приглашение по e-mail

`InviteUserCommand` (email, ФИО, роль — `RegisterUserCommand` без `Password`/`ConfirmPassword`)
реализован, замыкая последний пункт [[Задачи · Доработки каркаса]] по Identity. Гейт —
`IdentityPermissions.Users.Invite` (в бандле `Manager`).

`UserRegistrationService.InviteAsync`:

1. Создаёт `FshUser` со случайным неиспользуемым паролем (никому не показывается, только
   чтобы `UserManager.CreateAsync` было что валидировать по политике паролей) и
   `EmailConfirmed = false`. Дубликат e-mail (`RequireUniqueEmail`) роняет `CreateAsync` раньше,
   чем сгенерирован токен или поставлено письмо в очередь, — отдельной проверки «уже
   приглашён» не нужно.
2. Роль ограничена `SchoolRoleConstants.All` — валидатор команды отбрасывает произвольную
   строку (сознательное решение первой итерации, см. [[Задачи · Доработки каркаса]]).
3. Пользователь получает баланс `Basic` + дефолтные группы доступа, как любой другой способ
   создания пользователя (`RegisterAsync`, вход через внешнего провайдера) — иначе
   Teacher/Student/Guardian (бандл только `*.ViewOwn`) не смогут даже посмотреть свои сессии.
4. Токен — `UserManager.GeneratePasswordResetTokenAsync`, тот же вызов и та же цель токена,
   что и в `ForgotPasswordCommand`. Отдельной сущности токена нет: TTL и одноразовость по факту
   даёт `DataProtectionTokenProviderOptions` + инвалидация security stamp при успешном сбросе —
   заводить что-то новое не потребовалось.
5. Письмо — тем же каналом, что `ForgotPasswordCommand` (`IMailService` + `IJobService.Enqueue`,
   `CancellationToken.None` — фоновая задача не должна зависеть от токена HTTP-запроса).
   Отдельного механизма подстановки в шаблон в `BuildingBlocks/Mailing` нет — оба письма
   собираются обычной C#-интерполяцией строки, как и раньше; исследование из бэклога
   [[Задачи · Доработки каркаса]] → Notifications не дублировалось. Ссылка —
   `{Origin}/accept-invite?email=…&token=…&tenant=…` (origin из конфигурации, как в
   `ForgotPasswordCommand`, а не из хоста API-запроса, как в `RegisterUserCommand` — страница
   `/accept-invite` живёт в дашборде, а не в API). `tenant` в спецификации не значился явно, но
   добавлен по факту — страница `/accept-invite` вызывает `/reset-password`, а тому заголовок
   `X-FSH-App`/tenant обязателен (как и странице `/reset-password`, которая его тоже требует).
6. `UserRegisteredIntegrationEvent` **не публикуется** для приглашённого пользователя (в отличие
   от `RegisterAsync`/входа через внешнего провайдера). Причина — `UserRegisteredEmailHandler`
   на это событие уже шлёт письмо «Welcome! thanks for registering», которое наложилось бы на
   письмо-приглашение с точно противоположным смыслом («у вас ещё нет пароля, установите его по
   ссылке»). Другие подписчики на это событие вне Identity на сегодня отсутствуют, так что
   ничего не теряется; если появится модуль, которому нужно узнавать про новых пользователей
   независимо от способа создания — заводить для этого более узкое событие, а не переиспользовать
   `UserRegisteredIntegrationEvent`.

**Приём приглашения переиспользует существующий `ResetPasswordCommand`** — новой команды не
заводилось. Единственное дополнение — `UserPasswordService.ResetPasswordAsync` при успешном
сбросе выставляет `EmailConfirmed = true`, если оно ещё не было `true`.

> [!note] Как отличить приём приглашения от обычного «забыл пароль»
> Никак — и это осознанный выбор, а не недоделка. `InviteUserCommandHandler` минтит токен тем
> же вызовом `UserManager.GeneratePasswordResetTokenAsync` (та же цель токена в
> `DataProtectionTokenProviderOptions`), что и `ForgotPasswordCommand`, — то есть два сценария
> неразличимы на уровне `ResetPasswordCommand` по построению, отдельный флаг «это был инвайт» в
> команду/токен добавлять незачем. Единственный доступный на этот момент сигнал — текущее
> значение `EmailConfirmed`: `false` бывает только у аккаунта, созданного через `InviteAsync` и
> ещё не принявшего приглашение (обычная регистрация подтверждает почту через
> `ConfirmEmailCommand` до входа). Успешный `ResetPasswordAsync` уже доказывает владение
> почтовым ящиком не хуже, чем токен подтверждения адреса, — так что проставить
> `EmailConfirmed = true` в этот момент корректно для обоих сценариев, а не только для инвайта.

**Привязка приглашённого пользователя к `Guardian`/`Student`** — отдельным шагом, вызовом
[[People]]: `InviteUserCommand` возвращает `userId`, дальше —
`POST /api/v1/people/students/{studentId}/link-user` или
`.../guardians/{guardianId}/link-user` с этим `userId` в теле. Identity не может звать People
напрямую (правило 1 из `AGENTS.md` — только через `.Contracts`, а People уже зависит от
Identity.Contracts, обратная ссылка создала бы цикл), поэтому оркестрация — на стороне
вызывающего (dashboard UI или скрипт админа), не на бэкенде Identity.

`/self-register` (`SelfRegisterUserCommand`) не тронут — как и раньше, анонимная
самостоятельная регистрация. Приглашение стало основным способом получить доступ
представителю/ученику на практике, но self-register остаётся рабочим путём (backend), просто
дашборд-UI на него больше не ссылается.

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
`ManageRoles` `Impersonate` `ConfirmEmail` `Invite`.

`Users.Invite` — приглашение по e-mail без установки пароля приглашающим (см.
[[Задачи · Доработки каркаса]] → раздел «Приглашение по e-mail» выше). В бандле роли
`Manager` — единственное действие `Users`, которое получает эта роль, остальное управление
пользователями остаётся за `SchoolAdmin`.

Реестр прав — `PermissionConstants` в `BuildingBlocks/Shared`; каждый модуль
регистрирует свои в `ConfigureServices`. Механика — [[Модель прав доступа]].

## HTTP API

```
POST   /api/v1/token                            вход
POST   /api/v1/token/refresh
GET    /api/v1/users                            + CRUD, поиск
POST   /api/v1/users/register
POST   /api/v1/users/invite                     приглашение по e-mail (Users.Invite)
POST   /api/v1/users/reset-password             тоже приём приглашения (см. раздел выше)
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
