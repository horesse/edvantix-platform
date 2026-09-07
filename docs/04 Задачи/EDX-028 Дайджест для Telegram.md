---
key: EDX-028
aliases: [EDX-028]
tags: [задача, backend]
status: blocked
area: backend
priority: p2
blocked-by: ["[[EDX-024 Канал доставки Telegram]]"]
blocks: []
related: ["[[EDX-021 Telegram-уведомления]]"]
created: 2026-09-08
closed:
---

# EDX-028 · Дайджест для Telegram

## Контекст

Пачечные типы (`NotificationDefaults.IsDigestable`: отмена, перенос, пропуск) в почте
собираются в одно письмо через `PendingNotificationDigest` + `NotificationDigestJob`
(окно 7 минут). В [[EDX-024 Канал доставки Telegram]] (решение № 11) сообщения в Telegram
идут по одному: отмена восьми занятий группы на каникулы — восемь сообщений подряд каждому
родителю. Терпимо для запуска, но это ровно тот «источник раздражения», о котором
предупреждает [[Notifications]].

## Что сделать

- [ ] `PendingNotificationDigest`: колонка `Channel` (`Email` | `Telegram`) + миграция;
      уникальность/индексы по `(TenantId, Channel, RecipientKey, SentAtUtc)`.
- [ ] `NotificationDispatcher`: для digestable-типов снимать бит `Telegram` так же, как
      `Email`, и буферизовать строку с `Channel = Telegram` (только если есть активная
      привязка — иначе нечего копить).
- [ ] `NotificationDigestJob`: вторая ветка — группировать Telegram-строки по получателю,
      по истечении окна слать **одно** сообщение: «<b>Изменения в расписании</b>» +
      список «• {Title} — {Body}» (не более 15 пунктов, дальше «и ещё N»), кнопка
      «Открыть расписание» → `/my/schedule`. Через `SendTelegramMessageJob`.
- [ ] Окно для Telegram — те же 7 минут (`AggregationWindow`); отдельной настройки не заводить.
- [ ] Один элемент в пачке → отправлять как обычное сообщение, без заголовка «Изменения».

## Зависимости

- Блокируется: [[EDX-024 Канал доставки Telegram]]

## Проверка

- `Notifications.Tests`: диспетчер буферизует Telegram для digestable и не буферизует для
  остальных; job собирает одно сообщение из N строк, помечает `SentAtUtc`; одиночная
  строка уходит без заголовка.
- `Integration.Tests/Notifications/NotificationDigestTests` — расширить на канал Telegram.
- Руками: отменить пять занятий группы подряд → родителю одно сообщение через ~7 минут.
