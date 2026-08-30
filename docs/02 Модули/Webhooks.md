---
tags: [модуль, каркас, webhooks]
статус: реализован
порядок: 400
схема: webhooks
---

# Webhooks

← [[Edvantix]] · [[Карта модулей]] · задачи: [[Задачи · Доработки каркаса]]

> ✅ Реализован · порядок `400` · схема `webhooks`

## Назначение

Исходящие HTTP-уведомления во внешние системы. Закрывает требование «вебхуки»;
для Edvantix нужно лишь пополнить каталог типов событий.

## Домен

| Сущность | Поля |
|---|---|
| `WebhookSubscription` | URL, набор типов событий, секрет для подписи, активность |
| `WebhookDelivery` | журнал попытки: код ответа, тело, время, номер попытки |

Доставка с ретраями через Polly (`.agents/rules/resilience.md`), тело подписывается
секретом подписки.

## Контракты

`Modules.Webhooks.Contracts`

### Команды

| Команда | Что делает |
|---|---|
| `CreateWebhookSubscriptionCommand` | создать подписку |
| `DeleteWebhookSubscriptionCommand` | удалить |
| `TestWebhookSubscriptionCommand` | тестовая отправка на URL подписки |

### Запросы

`GetWebhookSubscriptionsQuery` · `GetWebhookDeliveriesQuery`

### DTO

`WebhookSubscriptionDto` · `WebhookDeliveryDto`

## Права

`WebhooksPermissions`, ресурс `Webhooks`. Кросс-тенантное администрирование —
`SystemPermissions.Platform.Webhooks` (`IsRoot: true`).

## HTTP API

```
GET    /api/v1/webhooks/subscriptions
POST   /api/v1/webhooks/subscriptions
DELETE /api/v1/webhooks/subscriptions/{id}
POST   /api/v1/webhooks/subscriptions/{id}/test
GET    /api/v1/webhooks/subscriptions/{id}/deliveries
GET    /api/v1/webhooks/event-types
```

## Каталог типов событий

`GET /api/v1/webhooks/event-types` (`GetWebhookEventCatalogQuery`, право `Webhooks.View`)
отдаёт `WebhookEventCatalog.All` — 24 типа событий, на которые школа может подписаться
(People, Curriculum, StudyGroups, Scheduling, Payments). **Имя типа = простое имя
типа-контракта интеграционного события** (`StudentEnrolledIntegrationEvent`) — ровно то,
на что матчит open-generic `WebhookFanoutHandler` (`typeof(TEvent).Name`), и что уходит
наружу в заголовке `X-Webhook-Event`.

Каталог — поверхность обнаружения для UI, **не allow-list**: подписка вправе назвать
событие вне каталога (совместимость с событиями будущих релизов), `*` подписывает на все.
Валидатор `CreateWebhookSubscriptionCommand` отклоняет только пустые токены в `Events`.
Полный список событий системы — [[Интеграционные события]].

## Применение в Edvantix

| Сценарий школы | Типы событий |
|---|---|
| Выгрузка оплат в бухгалтерию | `StudentPaymentConfirmed`, `StudentInvoiceIssued` |
| Синхронизация с внешней CRM | `StudentCreated`, `StudentStatusChanged` |
| Рассылки через внешний сервис | `SessionCancelled`, `StudentInvoiceOverdue` |
| Интеграция с сайтом школы | `CoursePublished` |
| Аналитика и BI | все |

Полный каталог событий системы — [[Интеграционные события]].

> [!warning] Персональные данные в теле
> Тела содержат ФИО, телефоны, суммы. Требования: только HTTPS, обязательная подпись,
> без чувствительных полей в URL. Внутренние заметки о учениках (`StudentNote`)
> не отправляются никогда.

Тело вебхука — **публичный контракт**. Внутренние DTO переиспользовать нельзя:
они меняются при рефакторинге, а интегратор к телу привязан.

## Зависимости

**Ссылается на:** `Identity.Contracts`, `Multitenancy.Contracts`,
`BuildingBlocks/Eventing`, Polly.

**Подписан на события:** всех модулей, через Outbox.

## Связанное

[[Интеграционные события]] · `.agents/rules/modules/webhooks.md` · `.agents/rules/resilience.md`
