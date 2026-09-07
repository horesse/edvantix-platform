---
key: EDX-024
aliases: [EDX-024]
tags: [задача, backend]
status: blocked
area: backend
priority: p1
blocked-by: ["[[EDX-023 Привязка получателя к Telegram]]", "[[EDX-025 Русские шаблоны уведомлений]]"]
blocks: ["[[EDX-026 Блок Telegram в настройках уведомлений]]", "[[EDX-027 Подключение Telegram для людей без учётной записи]]", "[[EDX-028 Дайджест для Telegram]]", "[[EDX-029 Напоминание за час до занятия]]"]
related: ["[[EDX-021 Telegram-уведомления]]"]
created: 2026-09-08
closed:
---

# EDX-024 · Канал доставки Telegram

## Контекст

Сам канал: диспетчер [[Notifications]] уже рендерит шаблон один раз и обходит
`IEnumerable<INotificationChannel>` по битам `NotificationChannelKind`. Нужен третий бит,
канал, фоновая отправка и третий тумблер в настройках подписок. Решения зонтика № 8–12, 14.

## Что сделать

**Канал**
- [ ] `NotificationChannelKind.Telegram = 4`; `All` включает его.
- [ ] `TelegramNotificationChannel : INotificationChannel` — ищет активную `TelegramLink`
      по `delivery.RecipientUserId` (это и есть `RecipientKey`); нет привязки → тихий выход.
      Есть → `BackgroundJob.Enqueue<SendTelegramMessageJob>(chatId, html, buttonUrl?)`.
      Best-effort: сбой постановки в очередь логируется (`Type`, без chat-id), не бросается.
- [ ] `SendTelegramMessageJob` (`[AutomaticRetry(Attempts = 3)]`, очередь `notifications`):
      `sendMessage(parse_mode=HTML, disable_web_page_preview=true)`.
      `429` → `retry_after` секунд ожидания и ретрай (не считать попыткой);
      `403 Forbidden: bot was blocked by the user` / `chat not found` → `TelegramLink.Deactivate(Blocked)`,
      без ретрая; прочие ошибки → ретрай Hangfire.
- [ ] Формат (`TelegramMessageFormatter`, чистая функция, тесты):
      `<b>{Title}</b>\n{Body}`; значения токенов HTML-экранируются (`<`, `>`, `&`);
      длина ≤ 4096 символов — обрезка с «…». Если `RenderedNotification.Link` не пуст и задан
      `TelegramOptions.DashboardBaseUrl` — inline-кнопка «Открыть» → `{DashboardBaseUrl}{Link}`.
      Без `DashboardBaseUrl` кнопка не рисуется (ссылки в ленте относительные).
- [ ] Порядок каналов: регистрировать после `InApp` и `Email`; в `NotificationDispatcher`
      **тихие часы** снимают бит `Telegram` вместе с `Email`.
- [ ] `SchoolNotificationFanout.DispatchAsync`: для контакта без учётки
      `Channels = channels & (Email | Telegram)` вместо `& Email`. `PreferenceUserId`
      по-прежнему только для учёток — у людей без учётки действуют дефолты.
- [ ] `MentionedInChannelIntegrationEventHandler` и остальные обработчики, где `Channels`
      задан явно, — просмотреть: упоминание в чате в Telegram **не** шлём (шум), остальные
      школьные события — по дефолтам ниже.

**Настройки подписок**
- [ ] `NotificationPreference.TelegramEnabled` (+ миграция `AddTelegramPreference`,
      бэкофилл существующих строк значением дефолта типа — иначе те, кто уже настраивал,
      получат «выключено» для всего).
- [ ] `NotificationDefaults.IsOn(type, Telegram)`: `true` для `SessionCancelled`,
      `SessionRescheduled`, `InvoiceIssued`, `InvoiceOverdue`, **`SessionReminder`**;
      остальное — opt-in. Тест синхронности каталога.
- [ ] `NotificationPreferenceDto` + `NotificationPreferenceItem` + `PUT /preferences`:
      поле `telegram` (аддитивно; клиенты, не передающие поле, сохраняют дефолт).
- [ ] `ListNotificationPreferencesQuery` — в ответ добавить `telegramLinked: bool`, чтобы
      фронт мог показать тумблеры Telegram неактивными до привязки (без второго запроса).

**Служебные сообщения бота** (не уведомления, идут напрямую, минуя диспетчер)
- [ ] Подтверждение привязки, `/stop`, отвязка из настроек — через тот же
      `SendTelegramMessageJob`; тексты — в `TelegramBotTexts` (RU).

**Документация**
- [ ] `docs/02 Модули/Notifications.md`: таблица каналов, дефолты, тихие часы;
      `.agents/rules/modules/notifications.md`; `docs/01 Архитектура/Интеграционные события.md`
      — колонка каналов, если она там есть.

## Зависимости

- Блокируется: [[EDX-023 Привязка получателя к Telegram]] (нужен `TelegramLink`),
  [[EDX-025 Русские шаблоны уведомлений]] (первое сообщение родителю — на русском)
- Блокирует: [[EDX-026 Блок Telegram в настройках уведомлений]], [[EDX-027 Подключение Telegram для людей без учётной записи]],
  [[EDX-028 Дайджест для Telegram]], [[EDX-029 Напоминание за час до занятия]]

## Проверка

- `Notifications.Tests`: форматтер (экранирование, обрезка, кнопка/без кнопки);
  диспетчер снимает `Telegram` в тихие часы; канал молчит без привязки; job деактивирует
  привязку на `403` и ждёт `retry_after` на `429`; дефолты `IsOn(…, Telegram)`.
- `Integration.Tests/Notifications`: `PUT /preferences` с `telegram=false` для
  `InvoiceIssued` → диспетчер не ставит job; без строки — ставит.
- Руками: отменить занятие группы, где у родителя есть привязка, — сообщение приходит
  в течение секунд с кнопкой «Открыть», ведущей на карточку занятия.
- `Architecture.Tests` зелёные.
