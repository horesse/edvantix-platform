---
tags: [модуль, каркас, notifications]
статус: реализован
порядок: 750
схема: notifications
---

# Notifications

← [[Edvantix]] · [[Карта модулей]] · задачи: [[Бэклог]]

> ✅ Реализован · порядок `750` · схема `notifications`

## Назначение

Уведомления в приложении: лента, счётчик непрочитанных, отметка прочтения.
Самый «тонкий» из каркасных модулей — для Edvantix требует заметного расширения.

## Домен

`Notification` — адресат, тип, заголовок, тело, ссылка, признак и время прочтения.

Доставка в браузер — через SSE (`clients/dashboard/src/sse/`).
Почта идёт мимо этого модуля, через `BuildingBlocks/Mailing` (используется в
[[Identity]]: подтверждение адреса, сброс пароля).

## Шаблоны сообщений с подстановкой

`Templating/` — минимальный движок подстановки внутри модуля. `BuildingBlocks/Mailing`
шаблонизатора не содержит (проверено; правило 4 — трогать `BuildingBlocks` без
согласования нельзя), поэтому механизм живёт здесь и намеренно скромный: только
подстановка `{{token}}`, без условий и циклов.

| Тип | Назначение |
|---|---|
| `NotificationTypes` | стабильные строковые ключи всех типов уведомлений Edvantix (пишутся в `Notification.Type`, они же ключи шаблонов) — зеркало таблицы «Каталог уведомлений Edvantix» ниже |
| `NotificationTemplate` | `TitleTemplate` / `BodyTemplate` / `LinkTemplate` для ленты + опциональные `EmailSubjectTemplate` / `EmailHtmlBodyTemplate` для письма |
| `INotificationTemplateCatalog` | справочник встроенных шаблонов, один на тип |
| `INotificationTemplateRenderer` | `Render(key, tokens)` → `RenderedNotification`; значения токенов HTML-экранируются только при подстановке в `EmailHtmlBodyTemplate`, в остальные поля — как есть |

Отсутствующий токен рендерится пустой строкой и логируется (обработчик событий не
должен падать из-за расхождения шаблона и вызова); неизвестный ключ шаблона — это
ошибка сборки, `Render` кидает `KeyNotFoundException`. Оба сервиса без состояния —
зарегистрированы синглтонами. Тесты — `src/Tests/Notifications.Tests/Templating/`.

## Каналы доставки (`Channels/`)

Точка расширения под Telegram/SMS, которые школы попросят. Добавление канала —
одна регистрация `INotificationChannel`, больше ничего.

| Тип | Назначение |
|---|---|
| `NotificationChannelKind` | `[Flags]`: `InApp` \| `Email` (\| будущие). Комбинация в запросе, один бит на канале |
| `INotificationChannel` | `Kind` + `SendAsync(NotificationDelivery, ct)` |
| `InAppNotificationChannel` | пишет строку `Notification` + пуш `NotificationCreated` в SignalR-группу `user:{id}`. Не best-effort — сбой всплывает в исходный запрос |
| `EmailNotificationChannel` | письмо через `BuildingBlocks/Mailing`, только если у шаблона есть тело письма и известен адрес. Best-effort — сбой транспорта логируется, не бросается |
| `NotificationRequest` | что обработчик события отдаёт диспетчеру: адресат, ключ шаблона, токены, `Source`, `Channels`, `ExpectedTenantId` |
| `INotificationDispatcher` | рендерит шаблон один раз, разливает по запрошенным каналам; при `ExpectedTenantId` сверяет ambient-тенант перед записью |

Обработчики интеграционных событий вызывают `INotificationDispatcher.DispatchAsync`,
а не пишут `Notification` напрямую. `MentionedInChannelIntegrationEventHandler`
переведён на диспетчер (канал `InApp`). Тесты — `src/Tests/Notifications.Tests/Channels/`.

## Подписчики школьных событий (`IntegrationEventHandlers/`)

`SchoolNotificationFanout` резолвит адресатов (через `IStudyGroupQueryService` +
`IPeopleLookupService.GetStudentContactsAsync/GetTeacherContactAsync`),
`NotificationTimeFormatter` печатает время в часовом поясе школы (`TenantSettings`).

| Событие | Кому | Каналы |
|---|---|---|
| `SessionCancelledIntegrationEvent` ([[Scheduling]]) | ученики + опекуны + преподаватель группы | приложение + почта |
| `SessionRescheduledIntegrationEvent` | те же | приложение + почта |
| `SessionReminderDueIntegrationEvent` | ученики + опекуны | приложение + почта |
| `AttendanceMarkedIntegrationEvent` (только `Absent`) | опекуны ученика | приложение + почта |
| `StudentInvoiceIssuedIntegrationEvent` ([[Payments]]) | плательщик (опекун-плательщик, иначе ученик) | приложение + почта |
| `StudentInvoiceOverdueIntegrationEvent` | плательщик | приложение + почта |
| `StudentPaymentConfirmedIntegrationEvent` | плательщик | только приложение |
| `StudentEnrolledIntegrationEvent` ([[StudyGroups]]) | ученик + опекуны | приложение + почта |

У кого нет учётки — уходит только письмо (адрес хранит People). Дедупликация
адресатов — `SchoolNotificationFanout.Distinct` (по `UserId`, иначе e-mail).

## Настройки подписок

`NotificationPreference` (сущность, одна строка на `(UserId, Type)`, тенант-изолирована):
`InAppEnabled` / `EmailEnabled`. Нет строки → `NotificationDefaults.IsOn(type, channel)`:
in-app по умолчанию включён для всего, e-mail — только для «high-signal» четвёрки
(отмена/перенос занятия, счёт, задолженность), остальное — opt-in (см. предупреждение
«Уведомления — главный источник раздражения»).

| | |
|---|---|
| `INotificationPreferenceService.EffectiveChannelsAsync(userId, type, requested)` | `requested & (разрешённые каналы)` — вызывает `INotificationDispatcher`, если у запроса задан `PreferenceUserId` |
| `GET /api/v1/notifications/preferences` | весь каталог с эффективными значениями (дефолты + оверрайды) |
| `PUT /api/v1/notifications/preferences` | upsert набора `{type, inApp, email}`; неизвестный `type` → 400 |

`PreferenceUserId` в `NotificationRequest` ставится только для адресатов с учёткой
(у email-only настраивать нечем — стоят дефолты каталога). Тесты —
`Notifications.Tests` (`NotificationDefaultsTests`, dispatcher-маскирование) +
`Integration.Tests/Tests/Notifications/NotificationPreferencesTests.cs`.

## Тихие часы

`NotificationQuietHours` (одна строка на тенант, тенант-изолирована): `Enabled`,
`StartLocal`/`EndLocal` (`TimeOnly`, время школы). `StartLocal > EndLocal` — окно
через полночь. Живёт **в модуле Notifications**, не в `TenantSettings` — чтобы не
тянуть кросс-модульное изменение Multitenancy ради одной настройки.

`INotificationQuietHoursService.IsQuietNowAsync()` переводит `TenantSettings.TimeZoneId`
в локальное время и проверяет попадание в окно. `INotificationDispatcher` при
`IsQuietNowAsync == true` снимает бит `Email` из эффективных каналов (in-app остаётся —
колокольчик пассивный). Применяется ко всем адресатам тенанта, не завязано на
`PreferenceUserId`.

`GET`/`PUT /api/v1/notifications/quiet-hours` (права `SchoolSettings.View`/`.Manage`
из [[Multitenancy]]). Тесты — `Notifications.Tests/Domain/NotificationQuietHoursTests.cs`
(окно через полночь), dispatcher-тест на удержание письма,
`Integration.Tests/Tests/Notifications/NotificationQuietHoursTests.cs`.

## Дедупликация и группировка (дайджест)

`NotificationDefaults.IsDigestable(type)` — типы, приходящие пачками (отмена/перенос
занятия, пропуск): их **письмо** не отправляется по одному. `INotificationDispatcher`
для такого типа пишет `PendingNotificationDigest` (строка на письмо, тенант-изолирована)
вместо канала `Email`; in-app идёт как обычно. Считается **после** тихих часов — если
письмо и так придержано, дайджест не пишется.

`NotificationDigestJob` (Hangfire, `*/5 * * * *`): по тенантам, группирует несланные
строки по e-mail, и если самая старая в группе старше окна агрегации
(`AggregationWindow = 7 мин`) — шлёт одно сводное письмо («N updates from your school»
+ `<ul>` заголовков/тел), помечает строки `SentAtUtc`. Best-effort: сбой отправки
оставляет строки на следующий тик. Root-тенант **не** пропускается (в отличие от
scan-джобов — здесь работаем только по уже накопленным строкам). Тесты —
dispatcher-тест на буферизацию + `Integration.Tests/Tests/Notifications/NotificationDigestTests.cs`.

> [!note] Что осталось и осознанные упрощения
> - **«Группа без преподавателя»** и **«Новый материал урока»** — не сделаны: у первой
>   нет события и нет резолва менеджеров, вторая — [[Curriculum]] (вне этапа 6).
> - **Копирайт под данные событий.** `SessionCancelled` не несёт времени → текст без
>   даты/времени; `AttendanceMarked` не несёт группы/даты → текст без них.
> - **Обогащены события** (аддитивно, без изменения поведения издателей):
>   `SessionRescheduledIntegrationEvent` +`StudyGroupId`; Payments-события
>   `Issued`/`Overdue`/`PaymentConfirmed` +`Number`/`Currency` (и `StudentId`/
>   `PayerGuardianId`, где их не было). См. справочники [[Scheduling]]/[[Payments]].

## Контракты

`Modules.Notifications.Contracts`

### Команды

`MarkNotificationReadCommand` · `MarkAllNotificationsReadCommand`

### Запросы

`ListNotificationsQuery` · `GetUnreadCountQuery`

### DTO

`NotificationDto`

## Права

`NotificationPermissions`, ресурс `Notifications.Inbox`.

## HTTP API

```
GET    /api/v1/notifications
GET    /api/v1/notifications/unread-count
GET    /api/v1/notifications/preferences
PUT    /api/v1/notifications/preferences
GET    /api/v1/notifications/quiet-hours
PUT    /api/v1/notifications/quiet-hours
POST   /api/v1/notifications/{id}/read
POST   /api/v1/notifications/read-all
GET    /api/v1/notifications/stream          SSE
```

## Каталог уведомлений Edvantix

Что модуль должен рассылать в готовой системе:

| Повод | Кому | Канал |
|---|---|---|
| Занятие завтра | ученик, представитель | приложение + почта, за 24 ч |
| Занятие отменено | ученик, представитель, преподаватель | приложение + почта, немедленно |
| Занятие перенесено | те же | приложение + почта, немедленно |
| Пропуск без уважительной причины | представитель | приложение + почта |
| Счёт выставлен | плательщик | приложение + почта |
| Оплата подтверждена | плательщик | приложение |
| Задолженность просрочена | плательщик, менеджер | приложение + почта |
| Зачислен в группу | ученик, представитель | приложение + почта |
| Новый материал урока | ученики группы | приложение |
| Упоминание в чате | упомянутый | приложение |
| Ответ по обращению | автор | приложение + почта |
| Группа без преподавателя | менеджеры | приложение |

Источники — события [[Scheduling]], [[Payments]], [[StudyGroups]], [[Chat]], [[Tickets]];
каталог целиком в [[Интеграционные события]].

> [!warning] Уведомления — главный источник раздражения
> Переусердствовать легко: родитель получает письмо о каждой мелочи и отписывается,
> пропуская важное. Принятое правило: по умолчанию включены только отмена и перенос
> занятия, счёт и задолженность. Остальное — по желанию пользователя.

## Зависимости

**Ссылается на:** `Identity.Contracts`, `Multitenancy.Contracts`, `People.Contracts`,
`StudyGroups.Contracts`, `Scheduling.Contracts`, `Payments.Contracts`, `Chat.Contracts`,
`Billing.Contracts`, `BuildingBlocks/Mailing`, `BuildingBlocks/Eventing`.

**Подписан на события:** [[Chat]] (`MentionedInChannel`), [[Scheduling]]
(`SessionCancelled`/`Rescheduled`/`ReminderDue`, `AttendanceMarked`), [[Payments]]
(`StudentInvoiceIssued`/`Overdue`, `StudentPaymentConfirmed`), [[StudyGroups]]
(`StudentEnrolled`), [[Billing]] (tenant billing письма). Ещё не подписан:
[[Tickets]], [[Curriculum]].

## Связанное

[[Интеграционные события]] · [[Scheduling]] · [[Payments]] · `.agents/rules/modules/notifications.md`
