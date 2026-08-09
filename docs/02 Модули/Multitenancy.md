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
| `TenantExpiryNotice` | уведомления о приближении окончания срока |
| `TenantProvisioning`, `TenantProvisioningStep` | пошаговый провижининг со статусами и повтором |

`AppTenantInfo` объявлен в `src/BuildingBlocks/Shared/Multitenancy/` — защищённом
каталоге; прикладные поля школы (часовой пояс, валюта, настройки) туда не добавляются,
для них заводится `TenantSettings` в этом модуле по образцу `TenantTheme`.
См. [[Задачи · Доработки каркаса]].

`TenantDbContext` + `TenantDbContextFactory` — реестр тенантов, глобальный
(вне изоляции).

## Контракты

`Modules.Multitenancy.Contracts`

### Команды

`CreateTenantCommand` · `ChangeTenantActivationCommand` · `AdjustTenantValidityCommand` ·
`RenewTenantCommand` · `UpdateTenantThemeCommand` · `ResetTenantThemeCommand` ·
`RetryTenantProvisioningCommand`

### Запросы

`GetTenantsQuery` · `GetTenantStatusQuery` · `GetTenantThemeQuery` ·
`GetTenantMigrationsQuery` · `GetTenantProvisioningStatusQuery`

### DTO

`TenantDto` · `TenantStatusDto` · `TenantThemeDto` · `TenantLifecycleResultDto` ·
`TenantMigrationStatusDto` · `TenantProvisioningStatusDto`

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

`ITenantService` · `ITenantThemeService`

## Права

`MultitenancyPermissions`, ресурс `Tenants`. Кросс-тенантные операции — через
`SystemPermissions.Platform.Tenants` с флагом `IsRoot: true`: доступны только
`SuperAdmin` в root-тенанте.

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
