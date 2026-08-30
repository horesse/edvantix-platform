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

`WebhooksPermissions`, ресурс `Webhooks`: `View`/`Create`/`Delete`/`Test` — всё
**тенант-скоуп**. Все пять эндпоинтов требуют именно эти права; подписки изолированы по
тенанту (`WebhookDbContext`), школа управляет только своими. Школьные роли `SchoolAdmin`
и `Manager` получают полный набор штатно (ресурс не root и не Identity-managed).

`SystemPermissions.Platform.Webhooks` (`IsRoot: true`) объявлено в `BuildingBlocks/Shared`,
но модулем **не используется** — кросс-тенантного администрирования вебхуков нет.

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

## Тело вебхука

Тело вебхука — **публичный контракт**, которым владеет модуль Webhooks, а не издающий
модуль: рефакторинг события-контракта не должен молча менять форму на проводе.

Каждая доставка — конверт `WebhookEnvelope` (`Modules.Webhooks.Contracts/WebhookPayloads/v1/`),
версионируется неймспейсом (`v1` → `v2` при ломающем изменении, старый течёт до миграции
интеграторов):

```json
{
  "id": "<guid события>",
  "type": "StudentEnrolledIntegrationEvent",
  "occurredAt": "2026-08-30T10:00:00Z",
  "tenantId": "<школа>",
  "data": { /* поля события без транспортных ключей */ }
}
```

`data` — поля события-контракта с вырезанными транспортными ключами
(`id`, `occurredOnUtc`, `tenantId`, `correlationId`, `source`): они уже подняты в
типизированные поля конверта. Состав `data` по каждому типу совпадает с полезной
нагрузкой соответствующего интеграционного события — [[Интеграционные события]].
Стабильность `data` гарантируется правилом версионирования событий (ломающее изменение
= новый тип `V2`).

`type` дублируется в заголовке `X-Webhook-Event`. Сборка конверта —
`WebhookEnvelopeBuilder`; тестовая доставка (`POST /subscriptions/{id}/test`) шлёт тот же
конверт с `type = "webhook.test"`.

## Зависимости

**Ссылается на:** `Identity.Contracts`, `Multitenancy.Contracts`,
`BuildingBlocks/Eventing`, Polly.

**Подписан на события:** всех модулей, через Outbox.

## Связанное

[[Интеграционные события]] · `.agents/rules/modules/webhooks.md` · `.agents/rules/resilience.md`
