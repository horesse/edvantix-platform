---
tags: [модуль, каркас, multitenancy]
статус: реализован
порядок: 200
схема: tenant
---

# Multitenancy

← [[Edvantix]] · [[Карта модулей]] · задачи: [[Задачи · Доработки каркаса]]

> ✅ Реализован · порядок `200` · схема `tenant`

## Назначение

Тенанты и их жизненный цикл. Один тенант = одна школа
([[ADR-001 Школа как тенант]]). Механика изоляции описана в [[Мультитенантность]].

## Домен

| Сущность | Назначение |
|---|---|
| `AppTenantInfo` | тенант Finbuckle: идентификатор, имя, строка подключения, срок действия |
| `TenantTheme` | брендирование: цвета, логотип |
| `TenantSettings` | часовой пояс (IANA `TimeZoneId`, по умолчанию `UTC`) и валюта (ISO 4217 `Currency`, по умолчанию `USD`) школы |
| `TenantExpiryNotice` | уведомления о приближении окончания срока |
| `TenantProvisioning`, `TenantProvisioningStep` | пошаговый провижининг со статусами и повтором |

`AppTenantInfo` объявлен в `src/BuildingBlocks/Shared/Multitenancy/` — защищённом
каталоге; прикладные поля школы (часовой пояс, валюта, настройки) туда не добавляются,
для них заведён `TenantSettings` в этом модуле по образцу `TenantTheme`: явная
`TenantId`-колонка с уникальным индексом в `TenantDbContext` (глобальный реестр, не
изолированный per-tenant контекст). Задаётся при создании школы (`CreateTenantCommand`,
необязательные поля с дефолтом UTC/USD) и бэкофиллен для существующих тенантов в
миграции `AddTenantSettings`. Root-тенант получает настройки по умолчанию при сидировании
в `Edvantix.DbMigrator`. См. [[Задачи · Доработки каркаса]].

`TenantDbContext` + `TenantDbContextFactory` — реестр тенантов, глобальный
(вне изоляции).

## Контракты

`Modules.Multitenancy.Contracts`

### Команды

`CreateTenantCommand` · `ChangeTenantActivationCommand` · `AdjustTenantValidityCommand` ·
`RenewTenantCommand` · `UpdateTenantThemeCommand` · `ResetTenantThemeCommand` ·
`UpdateTenantSettingsCommand` · `RetryTenantProvisioningCommand`

### Запросы

`GetTenantsQuery` · `GetTenantStatusQuery` · `GetTenantThemeQuery` ·
`GetTenantSettingsQuery` · `GetTenantMigrationsQuery` · `GetTenantProvisioningStatusQuery`

### DTO

`TenantDto` · `TenantStatusDto` · `TenantThemeDto` · `TenantSettingsDto` ·
`TenantLifecycleResultDto` · `TenantMigrationStatusDto` · `TenantProvisioningStatusDto`

### Публикуемые события

| Событие | Когда |
|---|---|
| `TenantSubscribedIntegrationEvent` | школа подписалась на план |
| `TenantRenewedIntegrationEvent` | подписка продлена |
| `TenantNearingExpiryIntegrationEvent` | срок подходит к концу |
| `TenantEnteredGraceIntegrationEvent` | начался льготный период |
| `TenantExpiredIntegrationEvent` | срок истёк |

Слушает их [[Billing]] (`TenantSubscriptionMaintenance`).

### Сервисы

`ITenantService` · `ITenantThemeService` · `ITenantSettingsService`

## Права

`MultitenancyPermissions`, ресурсы `Tenants` и `SchoolSettings`. Кросс-тенантные
операции — через `SystemPermissions.Platform.Tenants` с флагом `IsRoot: true`:
доступны только `SuperAdmin` в root-тенанте. `SchoolSettings` — тенантного уровня:
`View` (`IsBasic: true`, всем аутентифицированным) и `Manage` (роли `Admin` и
`SchoolAdmin` — обе получают полный не-root бандл, см. [[Identity]]).

## HTTP API

```
GET    /api/v1/tenants
POST   /api/v1/tenants
POST   /api/v1/tenants/{id}/activation
POST   /api/v1/tenants/{id}/renew
POST   /api/v1/tenants/{id}/validity
GET    /api/v1/tenants/{id}/migrations
GET    /api/v1/tenants/{id}/provisioning
POST   /api/v1/tenants/{id}/provisioning/retry
GET    /api/v1/tenants/{id}/theme                + обновление и сброс
GET    /api/v1/tenants/settings                  + обновление (PUT)
GET    /api/v1/tenants/me/status
```

## Роль в Edvantix

Изоляция школ включена по умолчанию через `BaseDbContext`: все предметные сущности
[[People]], [[Curriculum]], [[StudyGroups]], [[Scheduling]], [[Payments]] — тенантные.
`IGlobalEntity` к ним не применяется никогда.

Root-тенант — сам оператор Edvantix; роль `SuperAdmin` держит права
`SystemPermissions.Platform.*`.

## Зависимости

**Ссылается на:** `BuildingBlocks` (Core, Persistence, Web), Finbuckle.

**На него ссылаются:** все модули.

## Связанное

[[Мультитенантность]] · [[ADR-001 Школа как тенант]] · [[Billing]] · `.agents/rules/modules/multitenancy.md`
