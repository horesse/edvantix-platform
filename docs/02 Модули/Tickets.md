---
tags: [модуль, каркас, tickets]
статус: реализован
порядок: 700
схема: tickets
---

# Tickets

← [[Edvantix]] · [[Карта модулей]] · задачи: [[Задачи · Доработки каркаса]]

> ✅ Реализован · порядок `700` · схема `tickets`

## Назначение

Обращения в поддержку с защищённым жизненным циклом. Закрывает требование «тикеты».

## Домен

`Ticket` — агрегат (`AggregateRoot<Guid>`, `ISoftDeletable`):

| Поле | |
|---|---|
| `Number` | человекочитаемый номер |
| `Title`, `Description` | |
| `Status`, `Priority` | `TicketStatus`, `TicketPriority` |
| `Category` | `TicketCategory`: `General`/`Payment`/`Schedule`/`GroupChange`/`TeachingQuality`/`Technical`. По умолчанию `General` |
| `Audience` | `TicketAudience`: `School`/`Platform` — кто обрабатывает. По умолчанию выводится из `Category` (`TicketClassificationDefaults`: только `Technical` → `Platform`), можно задать явно при создании |
| `ReporterUserId` | автор, обязателен |
| `AssignedToUserId` | исполнитель, nullable |
| `ResolutionNote` | |
| `RelatedStudentId`, `RelatedStudyGroupId`, `RelatedInvoiceId` | контекст обращения, nullable — непрозрачные id, модуль не ссылается на People/StudyGroups/Payments в рантайме; частичные индексы под фильтр «обращения по этому ученику/группе/счёту». Меняются на любом статусе (это метаданные, не жизненный цикл) |
| `CreatedAtUtc`, `UpdatedAtUtc`, `ResolvedAtUtc`, `ClosedAtUtc` | |

`TicketComment` — переписка, принадлежит агрегату.

> [!important] Переходы состояний защищены агрегатом
> Каждый публичный метод бросает `CustomException` при вызове из недопустимого
> состояния, поэтому API отдаёт чистый `409`, а не «тихо» ломает данные.
> Логика жизненного цикла — в домене, не в обработчиках.

Доменные события: `TicketCreated`, `TicketAssigned`, `TicketStatusChanged`,
`TicketCommentAdded`.

## Контракты

`Modules.Tickets.Contracts`

### Команды

`CreateTicketCommand` · `UpdateTicketCommand` · `AssignTicketCommand` ·
`ResolveTicketCommand` · `ReopenTicketCommand` · `CloseTicketCommand` ·
`DeleteTicketCommand` · `RestoreTicketCommand` · `AddTicketCommentCommand`

### Запросы

`SearchTicketsQuery` · `GetTicketByIdQuery` · `ListTicketCommentsQuery` ·
`ListTrashedTicketsQuery`

### DTO

`TicketDto` · `TicketCommentDto` · `TicketStatus` · `TicketPriority`

## Права

`TicketsPermissions`, ресурс `Tickets`:
`View` (basic) · `Create` · `Update` · `Delete` · `Restore` · `Assign` ·
`Resolve` · `Reopen` · `Close` · `Comment`.

Отдельные права на каждый переход — школа может разрешить менеджеру закрывать,
но не удалять.

## HTTP API

```
GET    /api/v1/tickets
POST   /api/v1/tickets
GET    /api/v1/tickets/{id}
PUT    /api/v1/tickets/{id}
DELETE /api/v1/tickets/{id}
POST   /api/v1/tickets/{id}/restore
POST   /api/v1/tickets/{id}/assign
POST   /api/v1/tickets/{id}/resolve
POST   /api/v1/tickets/{id}/reopen
POST   /api/v1/tickets/{id}/close
GET    /api/v1/tickets/{id}/comments
POST   /api/v1/tickets/{id}/comments
GET    /api/v1/tickets/trash
```

## Применение в Edvantix

Два потока обращений различаются полем `Audience`:

| Поток | `Audience` | Кто → кому | Пример |
|---|---|---|---|
| Внутренний | `Platform` | пользователь школы → поддержка Edvantix | «не генерируется расписание» |
| Школьный | `School` | ученик или представитель → администрация школы | «хотим сменить группу» |

Второй важнее для продукта: он заменяет переписку в мессенджерах.

**Категория и адресат.** `Category` определяет `Audience` по умолчанию
(`TicketClassificationDefaults`, только `Technical` → `Platform`); при создании
`Audience` можно переопределить явно, при `UpdateTicket` — пересчитывается из новой
категории (если не задан явно). `SearchTicketsQuery` (и `GET /tickets`) фильтруют
по `category` и `audience`. Дефолтный **исполнитель** по категории (конкретный
`AssignedToUserId`) — отдельная задача: нужен пер-тенантный справочник персонала.

Ограничение текущей модели: `ReporterUserId` обязателен — обращение может создать
только пользователь с учётной записью. Для представителей это выполняется.

**Контекст обращения.** `CreateTicketCommand`/`UpdateTicketCommand` принимают
опциональные `RelatedStudentId` / `RelatedStudyGroupId` / `RelatedInvoiceId`;
`SearchTicketsQuery` (и `GET /tickets`) фильтруют по ним. `PUT /tickets/{id}` —
полная замена: не переданная ссылка очищается. `Guid.Empty` → «не задано»
(нормализуется в домене, плюс отклоняется валидатором). Обращение «верните деньги»
теперь можно привязать к счёту; на карточке ученика — история его обращений.

## Вложения

Файлы прикрепляются через общие эндпоинты [[Files]] с `ownerType=Ticket`,
`ownerId={ticketId}` — в контрактах команд Tickets ничего для вложений нет
(как и у [[Chat]] по факту: вложение — это отдельный `FileAsset`, а не поле команды).
Единственный гейт — `TicketFileAccessPolicy` (`Modules.Tickets/Authorization/`):

| Операция | Кто |
|---|---|
| Attach / Read | автор (`ReporterUserId`) или исполнитель (`AssignedToUserId`) обращения |
| Delete / смена видимости | только загрузивший |

Более широкий доступ (менеджер, не назначенный на тикет) — через назначение
на обращение (`AssignTicket`); аналог membership-правила [[Chat]]. Проверки прав
`Tickets.View` в политику не заводили, чтобы не тянуть в модуль `Identity.Contracts`.

## Зависимости

**Ссылается на:** `Identity.Contracts`, `Multitenancy.Contracts`, `Files.Contracts`.

**Подписаны на его события:** [[Notifications]].

## Связанное

[[Notifications]] · [[People]] · `.agents/rules/modules/tickets.md`
