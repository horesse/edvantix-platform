---
key: EDX-012
aliases: [EDX-012]
tags: [задача, backend, frontend]
status: done
area: backend
priority: p3
blocked-by: []
blocks: []
related: ["[[Открытые вопросы]]", "[[EDX-008 API-ключи в Identity]]"]
created: 2026-09-03
closed: 2026-09-07
---

> [!success] Готово · 2026-09-07 · branch `claude/edx-012-3bb591`
> Анонимный iCal-фид `GET /api/v1/my/schedule.ics?tenant=&token=`, личный отзываемый
> `IcalSubscriptionToken` (сущность модуля Scheduling), эндпоинты `rotate`/`revoke`/`get`
> под `Sessions.ViewOwn`, блок «Подписка на календарь» на `/my/schedule`.
> Тесты: `Scheduling.Tests` 60/60 (writer + сервис токена), `Architecture.Tests` 51/51,
> dashboard Playwright `tests/cabinet/my-screens.spec.ts` 6/6. Интеграционный тест
> `ScheduleIcsFeedTests` добавлен (гоняется в CI — Docker).

# EDX-012 · Экспорт расписания в iCal

## Контекст

Из [[Открытые вопросы]] → Scheduling: «Попросят почти сразу. Реализуемо тонким
эндпоинтом поверх `GetMyScheduleQuery`. Вопрос только в приоритете». Развязано в задачу.

## Что сделано

- [x] `GET /api/v1/my/schedule.ics` — VCALENDAR из ближайших занятий пользователя
      (общий `IMyScheduleReader` — та же выборка, что `GetMyScheduleQuery`), окно по
      умолчанию `[-7 дней, +60 дней]`. Все метки времени — UTC с суффиксом `Z`
      (VTIMEZONE не пишется; клиент показывает в поясе зрителя, для школьного фида это
      и есть пояс школы). `UID` = `RescheduledFromId ?? Id` — перенос двигает событие,
      а не дублирует. В фид попадают только `Planned`/`Held`. `IcalWriter` — свой
      минимальный сериализатор RFC 5545 (CRLF, фолдинг по 75 октетов, экранирование).
- [x] Аутентификация: по токену в query — `IcalSubscriptionToken` (сущность
      `Modules.Scheduling`, тенант-скоуп, одна строка на пользователя, уникальный индекс
      по `UserId` и по `Token`), отзываемый. Секрет — Base64Url 32 случайных байт.
      Эндпоинт анонимный (`AllowAnonymous` + `RequireRateLimiting("auth")`); тенант
      резолвится из `?tenant=` штатной `WithDelegateStrategy` Finbuckle до построения
      `SchedulingDbContext`, поэтому поиск токена уже тенант-фильтрован.
      `LastUsedAtUtc` штампуется не чаще раза в час.
- [x] Управление токеном (JWT-гейт `Sessions.ViewOwn`):
      `GET /my/schedule/ical-subscription` (204, если нет),
      `POST /my/schedule/ical-subscription/rotate` (создать / сменить секрет),
      `DELETE /my/schedule/ical-subscription` (отозвать).
- [x] FE: на `/my/schedule` — блок «Подписка на календарь» с кнопкой «Добавить в
      календарь» (`rotate`), полем со ссылкой (`env.apiBase` + относительный `path` из
      DTO), «Скопировать», «Обновить ссылку», «Отозвать».
- [x] `docs/02 Модули/Scheduling.md`, `docs/03 Frontend/Карта экранов.md`,
      `.agents/rules/modules/scheduling.md`.

## Согласование с EDX-008

Это **не** механизм API-ключей: фид даёт ровно один read-only ресурс и не несёт прав.
Полноценные персистентные токены с правами/скоупами остаются задачей
[[EDX-008 API-ключи в Identity]] — переиспользовать там нечего, кроме идеи «личный
отзываемый секрет».

## Зависимости

- Блокируется: —
- Связано: [[EDX-008 API-ключи в Identity]], [[Открытые вопросы]]

## Проверка

- `POST /my/schedule/ical-subscription/rotate` → взять `path` → `GET {apiBase}{path}`
  без авторизации → `200 text/calendar`, тело `BEGIN:VCALENDAR…END:VCALENDAR`.
- Неверный `token` → `404` (неотличимо от пустого расписания).
- `DELETE` → та же ссылка отдаёт `404`.
- Импорт .ics в Google Calendar / Apple Calendar показывает занятия в правильном поясе.
