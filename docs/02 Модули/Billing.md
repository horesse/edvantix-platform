---
tags: [модуль, каркас, billing, деньги]
статус: реализован
порядок: 500
схема: billing
---

# Billing

← [[Edvantix]] · [[Карта модулей]] · задачи: [[Задачи · Доработки каркаса]]

> ✅ Реализован · порядок `500` · схема `billing`

## Назначение

SaaS-биллинг платформы: **школа платит Edvantix**. Планы, подписки, счета, учёт
потребления.

Деньги учеников — другой модуль, [[Payments]]
([[ADR-004 Payments отдельно от Billing]]).

## Домен

| Сущность | Назначение |
|---|---|
| `BillingPlan` | тарифный план платформы, лимиты |
| `Subscription` | подписка школы на план, период |
| `Invoice`, `InvoiceLineItem` | счёт школе от Edvantix |
| `UsageSnapshot` | снимок потребления для лимитов и тарификации |

Перечисления — `BillingEnums`.

## Контракты

`Modules.Billing.Contracts`

### Команды

| Область | Команды |
|---|---|
| Plans | `CreatePlanCommand` `UpdatePlanCommand` |
| Subscriptions | `AssignSubscriptionCommand` |
| Invoices | `GenerateInvoicesCommand` `IssueInvoiceCommand` `MarkInvoicePaidCommand` `VoidInvoiceCommand` |
| Usage | `CaptureUsageSnapshotsCommand` |

### Запросы

`GetPlansQuery` · `GetPlanTermQuery` · `GetSubscriptionQuery` · `GetInvoicesQuery` ·
`GetInvoiceByIdQuery` · `GetMyInvoicesQuery` · `GetUsageSnapshotsQuery`

### DTO

`BillingPlanDto` · `SubscriptionDto` · `InvoiceDto` · `InvoiceLineItemDto` ·
`UsageSnapshotDto`

### Публикуемые события

`InvoiceIssuedIntegrationEvent`

### Подписки

| Событие ([[Multitenancy]]) | Обработчик |
|---|---|
| `TenantSubscribedIntegrationEvent` | `TenantSubscribedIntegrationEventHandler` |
| `TenantRenewedIntegrationEvent` | `TenantRenewedIntegrationEventHandler` |

Плюс `TenantSubscriptionMaintenance` — фоновое обслуживание подписок.

### Сервисы

`IBillingService` · `IInvoicePdfRenderer`

`IInvoicePdfRenderer` переиспользуется модулем [[Payments]] для школьных счетов —
интерфейс общий, реализации разные.

## Права

`BillingPermissions`, ресурс `Billing`. Кросс-тенантные операции —
`SystemPermissions.Platform.Plans`, `.Subscriptions`, `.Invoices` (`IsRoot: true`):
доступны только `SuperAdmin`.

## HTTP API

```
GET    /api/v1/billing/plans                    + создание, правка
GET    /api/v1/billing/subscription
POST   /api/v1/billing/subscriptions
GET    /api/v1/billing/invoices
GET    /api/v1/billing/invoices/{id}
GET    /api/v1/billing/invoices/{id}/pdf
GET    /api/v1/billing/invoices/my
POST   /api/v1/billing/invoices/generate
POST   /api/v1/billing/invoices/{id}/issue
POST   /api/v1/billing/invoices/{id}/paid
POST   /api/v1/billing/invoices/{id}/void
GET    /api/v1/billing/usage                    + снятие снимков
```

## Граница с Payments

> [!warning] Два `Invoice` в системе
> | | `Billing.Invoice` | `Payments.StudentInvoice` |
> |---|---|---|
> | Плательщик | школа | ученик или представитель |
> | Получатель | Edvantix | школа |
> | Видимость | SuperAdmin + админ школы | менеджеры, ученик, представитель |
> | Подтверждение | провайдер или оператор | менеджер вручную |
>
> В UI разведены: «Подписка» против «Счета учеников». При код-ревью следить,
> чтобы не смешивали.

Чего в Billing быть не должно: счета ученикам, тарифы на занятия, подтверждение
школьных оплат.

## Зависимости

**Ссылается на:** `Multitenancy.Contracts`, `Identity.Contracts`,
`BuildingBlocks/Quota`.

**На него ссылается:** [[Payments]] — только ради `IInvoicePdfRenderer`.

## Связанное

[[ADR-004 Payments отдельно от Billing]] · [[Payments]] · [[Multitenancy]] · `.agents/rules/modules/billing.md`
