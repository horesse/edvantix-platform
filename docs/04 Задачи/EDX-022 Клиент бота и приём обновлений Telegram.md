---
key: EDX-022
aliases: [EDX-022]
tags: [задача, backend, ops]
status: open
area: backend
priority: p1
blocked-by: []
blocks: ["[[EDX-023 Привязка получателя к Telegram]]", "[[EDX-031 Собственный бот школы]]"]
related: ["[[EDX-021 Telegram-уведомления]]", "[[EDX-024 Канал доставки Telegram]]"]
created: 2026-09-08
closed:
---

# EDX-022 · Клиент бота и приём обновлений Telegram

## Контекст

Фундамент для [[EDX-021 Telegram-уведомления]]: конфигурация, HTTP-клиент к Bot API
и входящий поток обновлений. Ни привязок, ни сообщений здесь нет — только транспорт.
Живёт в `Modules.Notifications`, папка `Telegram/`.

Решения зонтика, которые здесь исполняются: № 1 (один бот платформы), № 6 (вебхук +
polling), № 7 (свой клиент), № 14 (без PII в логах).

## Что сделать

**Конфигурация**
- [ ] `TelegramOptions` (`BindConfiguration("TelegramOptions")`, валидация при старте):
      `Enabled` (default `false`), `BotToken`, `BotUsername`, `Mode` (`Webhook` | `Polling`),
      `PublicApiBaseUrl` (для `setWebhook`), `WebhookSecret` (16–256 символов, `[A-Za-z0-9_-]`),
      `DashboardBaseUrl` (для кнопки «Открыть» в EDX-024). При `Enabled=false` модуль
      ничего не регистрирует, кроме health «disabled» — чтобы стенд без токена поднимался.
- [ ] `appsettings.json` — секция с пустыми значениями и комментарием в `deploy/`;
      Aspire: параметр-секрет `telegram-bot-token`, прокинуть в API как переменную окружения.

**Клиент**
- [ ] `ITelegramBotClient` + `TelegramBotClient` (typed `HttpClient`, `BaseAddress =
      https://api.telegram.org/bot{token}/`, `.AddHeroResilience(config)`): `GetMeAsync`,
      `SetWebhookAsync(url, secret, allowedUpdates: ["message"])`, `DeleteWebhookAsync`,
      `GetUpdatesAsync(offset, timeout)`, `SendMessageAsync(chatId, html, replyMarkup?)`.
      Ответ Bot API — `{ ok, result | error_code, description, parameters.retry_after }`;
      клиент разворачивает в `TelegramResult<T>` с типизированной ошибкой
      (`TooManyRequests(retryAfter)`, `Forbidden`, `BadRequest`, `Other`). Токен не логируется
      никогда — `HttpClient` логирование запросов на этот клиент отключить (URL содержит токен).
- [ ] DTO минимальные, свои: `TelegramUpdate { UpdateId, Message? }`,
      `TelegramMessage { Chat.Id, From.Id/Username/FirstName, Text }`. Всё лишнее игнорируется
      (`JsonIgnoreCondition`/unknown properties).

**Входящий поток**
- [ ] `ITelegramUpdateHandler.HandleAsync(TelegramUpdate, ct)` — интерфейс, который EDX-023
      реализует. Здесь — заглушка, логирующая тип обновления (без текста и id).
- [ ] Вебхук: `POST /api/v1/telegram/webhook` — `AllowAnonymous`, вне тенанта (как
      `/health`; тенант появится в EDX-023 из токена), `RequireRateLimiting("auth")`,
      сверка заголовка `X-Telegram-Bot-Api-Secret-Token` с `WebhookSecret` константным временем,
      иначе `401`. Тело → `ITelegramUpdateHandler`. Всегда отвечает `200` (Telegram ретраит
      не-2xx и копит очередь); ошибки обработчика — в лог.
- [ ] Polling: `TelegramPollingService : BackgroundService` при `Mode = Polling` —
      `getUpdates` с long-poll 30 с, `offset = last + 1`, тот же `ITelegramUpdateHandler`,
      backoff при ошибках. При старте вызывает `deleteWebhook` (иначе `getUpdates` даёт 409).
- [ ] При `Mode = Webhook` — `IHostedService`, который на старте делает `setWebhook`
      (`{PublicApiBaseUrl}/api/v1/telegram/webhook`, секрет). Идемпотентно.
- [ ] Дедупликация `update_id`: последний обработанный id в `TelegramBotState` (одна
      глобальная строка, `IGlobalEntity`), повторы пропускаются — вебхук Telegram доставляет
      «как минимум раз».

**Наблюдаемость**
- [ ] Health check `telegram` (`getMe`, кэш 5 мин; `Degraded` при ошибке, `Healthy`
      с тегом `disabled` при `Enabled=false`).
- [ ] Метрики OpenTelemetry: `telegram.updates.received`, `telegram.api.errors{code}`.

**Документация**
- [ ] `docs/02 Модули/Notifications.md` → раздел «Telegram» (конфигурация, режимы),
      `.agents/rules/modules/notifications.md`, `deploy/` — переменные окружения.

## Зависимости

- Блокируется: — (решения зафиксированы в [[EDX-021 Telegram-уведомления]])
- Блокирует: [[EDX-023 Привязка получателя к Telegram]], [[EDX-031 Собственный бот школы]]

## Проверка

- `Notifications.Tests/Telegram/`: разбор ответов Bot API (ok / 429 с `retry_after` /
  403), сериализация `sendMessage` с `reply_markup`, проверка секрета вебхука
  (совпал / не совпал / отсутствует).
- `Integration.Tests`: `POST /telegram/webhook` без секрета → `401`; с секретом и
  повторным `update_id` → `200`, обработчик вызван один раз.
- Локально: `Mode=Polling`, токен тестового бота, `/start` в боте → в логах «update
  received: message» без текста и id.
- `Architecture.Tests` зелёные (обработчики `public sealed`, валидатор на команды).
