---
key: EDX-023
aliases: [EDX-023]
tags: [задача, backend]
status: blocked
area: backend
priority: p1
blocked-by: ["[[EDX-022 Клиент бота и приём обновлений Telegram]]"]
blocks: ["[[EDX-024 Канал доставки Telegram]]", "[[EDX-026 Блок Telegram в настройках уведомлений]]", "[[EDX-027 Подключение Telegram для людей без учётной записи]]", "[[EDX-030 Команды бота today и tomorrow]]"]
related: ["[[EDX-021 Telegram-уведомления]]", "[[People]]"]
created: 2026-09-08
closed:
---

# EDX-023 · Привязка получателя к Telegram

## Контекст

Чтобы отправить сообщение, нужно знать `chat_id` получателя. Получатель в [[Notifications]] —
это `RecipientKey` (`UserId` или `email:{addr}`, см. `SchoolNotificationFanout.DispatchAsync`).
Задача связывает `RecipientKey` тенанта с чатом через одноразовую ссылку
`https://t.me/{BotUsername}?start={token}` (решения № 2–5 зонтика).

Бот один на платформу, а получатели — в тенантах. Токен — единственное, что говорит боту,
в какой школе искать: обработчик `/start` резолвит тенант по токену **до** построения
`NotificationsDbContext` (приём тот же, что `?tenant=` в iCal, [[EDX-012 Экспорт расписания в iCal]],
но источник тенанта — строка токена в глобальной таблице).

## Домен

```
TelegramLinkToken (IGlobalEntity — ищется без тенанта)
  Id, TenantId, RecipientKey, TokenHash (SHA-256 от секрета), ExpiresAtUtc,
  CreatedByUserId, UsedAtUtc?
  — одна активная строка на (TenantId, RecipientKey): выпуск нового аннулирует старый

TelegramLink (тенант-изолирована)
  Id, RecipientKey (уникален в тенанте), ChatId, TelegramUsername?, DisplayName?,
  LinkedAtUtc, IsActive, DeactivatedAtUtc?, DeactivationReason? (UserStopped | Blocked | Unlinked)
  — ChatId + TenantId уникальны: один чат — один получатель в школе
```

Секрет токена в БД не хранится, только хэш. `RecipientKey` для людей без учётки —
нормализованный e-mail в нижнем регистре (как в `Distinct`).

## Что сделать

**Выпуск токена**
- [ ] `ITelegramLinkService.IssueTokenAsync(recipientKey, issuedByUserId)` → `TelegramLinkInviteDto
      { Url, ExpiresAtUtc }`; `Url = https://t.me/{BotUsername}?start={token}`. Срок 30 дней.
- [ ] `POST /api/v1/notifications/telegram/link-token` — **свой** токен текущего пользователя
      (право `Notifications.Inbox.View`, как остальные личные эндпоинты модуля).
- [ ] `POST /api/v1/notifications/telegram/invites` `{ studentId | guardianId | teacherId }` —
      токен для человека из [[People]] по его **e-mail** (с учёткой — по `UserId`).
      Права: `Students.Update` / `Guardians.Update` / `Teachers.Update` соответственно.
      Контакт резолвится через `IPeopleLookupService`; для представителя по id нужен
      аддитивный `GetGuardianContactAsync(guardianId)` в `People.Contracts` (сейчас представители
      достаются только через ученика). Нет e-mail и нет учётки → `422` «Укажите e-mail или создайте учётную запись».

**Статус и отвязка**
- [ ] `GET /api/v1/notifications/telegram` → `TelegramLinkStatusDto { IsLinked, Username?,
      LinkedAtUtc?, IsActive, DeactivationReason?, BotUsername }` — свой статус.
- [ ] `GET /api/v1/notifications/telegram/status?recipient=…` — статус человека из People
      для карточек (те же права, что у invites). Возвращает только `IsLinked/IsActive/Username`.
- [ ] `DELETE /api/v1/notifications/telegram` — отвязать себя; бот шлёт «Уведомления отключены».

**Обработчик обновлений** (реализация `ITelegramUpdateHandler` из EDX-022)
- [ ] `/start <token>`: хэш → `TelegramLinkToken` (глобально) → проверка срока и `UsedAtUtc` →
      установить тенант-контекст `TenantId` → upsert `TelegramLink` (если у чата была
      привязка к другому получателю в этой школе — заменить, старую деактивировать
      `Unlinked`) → пометить токен использованным → ответ «Готово! Уведомления школы «{Название}»
      будут приходить сюда. Отключить — /stop». Название школы — из `AppTenantInfo`.
- [ ] `/start` без токена или с неверным/истёкшим: «Чтобы подключить уведомления, откройте
      ссылку из кабинета школы или попросите её у администратора». Никаких подсказок, чем
      именно плох токен.
- [ ] `/stop`: чат может быть привязан в нескольких школах → деактивировать **все** его
      привязки (`UserStopped`), ответ перечисляет школы. Повторный `/start` по новой ссылке
      реактивирует.
- [ ] `/help` и любой другой текст: короткая справка с тремя командами. Не эхо.
- [ ] Ограничение: не больше 10 `/start` с одного чата в минуту (in-memory, чтобы не
      перебирать токены).

**Инфраструктура**
- [ ] Миграция `AddTelegramLinks` в `Migrations.PostgreSQL/Notifications/`. Индексы:
      `TelegramLinkToken(TokenHash)` уникальный; `TelegramLink(TenantId, RecipientKey)`,
      `TelegramLink(TenantId, ChatId)` уникальные.
- [ ] Hangfire `TelegramLinkTokenCleanupJob` раз в сутки — удалять использованные и
      истёкшие токены старше 7 дней.
- [ ] Аудит: `TelegramLink` — `IAuditable`; привязка/отвязка видны в `/audits` без chat-id
      (в `entity-labels` — «Подключение Telegram»).

**Документация**
- [ ] `docs/02 Модули/Notifications.md` (домен, эндпоинты, сценарий `/start`),
      `docs/02 Модули/People.md` (новый метод lookup-сервиса), `.agents/rules/modules/notifications.md`.

## Зависимости

- Блокируется: [[EDX-022 Клиент бота и приём обновлений Telegram]]
- Блокирует: [[EDX-024 Канал доставки Telegram]], [[EDX-026 Блок Telegram в настройках уведомлений]],
  [[EDX-027 Подключение Telegram для людей без учётной записи]], [[EDX-030 Команды бота today и tomorrow]]

## Проверка

- `Notifications.Tests/Telegram/`: выпуск токена аннулирует предыдущий; истёкший и
  использованный отклоняются; `/stop` деактивирует привязки во всех школах; повторный
  `/start` в другой школе не трогает первую.
- `Integration.Tests`: изоляция тенантов на `TelegramLink` (обязательный тест на новую сущность);
  сквозной `POST link-token → POST /telegram/webhook {"/start <token>"} → GET /telegram` даёт
  `IsLinked = true`; тот же токен второй раз → отказ, привязка не меняется.
- Руками в `Mode=Polling`: ссылка из ответа `link-token` открывается в Telegram, после
  «Start» приходит подтверждение с названием школы.
