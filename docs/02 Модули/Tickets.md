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

Два потока обращений, которые модуль пока не различает:

| Поток | Кто → кому | Пример |
|---|---|---|
| Внутренний | пользователь школы → поддержка Edvantix | «не генерируется расписание» |
| Школьный | ученик или представитель → администрация школы | «хотим сменить группу» |

Второй важнее для продукта: он заменяет переписку в мессенджерах.

Ограничение текущей модели: `ReporterUserId` обязателен — обращение может создать
только пользователь с учётной записью. Для представителей это выполняется.

**Контекст обращения.** `CreateTicketCommand`/`UpdateTicketCommand` принимают
опциональные `RelatedStudentId` / `RelatedStudyGroupId` / `RelatedInvoiceId`;
`SearchTicketsQuery` (и `GET /tickets`) фильтруют по ним. `PUT /tickets/{id}` —
полная замена: не переданная ссылка очищается. `Guid.Empty` → «не задано»
(нормализуется в домене, плюс отклоняется валидатором). Обращение «верните деньги»
теперь можно привязать к счёту; на карточке ученика — история его обращений.

## Зависимости

**Ссылается на:** `Identity.Contracts`, `Multitenancy.Contracts`.

**Подписаны на его события:** [[Notifications]].

## Связанное

[[Notifications]] · [[People]] · `.agents/rules/modules/tickets.md`
