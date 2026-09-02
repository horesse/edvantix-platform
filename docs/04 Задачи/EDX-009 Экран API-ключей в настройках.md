---
key: EDX-009
aliases: [EDX-009]
tags: [задача, frontend, dashboard]
status: blocked
area: dashboard
priority: p3
blocked-by: ["[[EDX-008 API-ключи в Identity]]"]
blocks: []
related: []
created: 2026-09-03
closed:
---

# EDX-009 · Экран API-ключей в личных настройках

## Контекст

`clients/dashboard/src/pages/settings/api-keys.tsx` рендерит статичный «coming soon»
и **ведёт кнопкой на `github.com/fullstackhero/dotnet-starter-kit`** — остаток
добрендинга; ссылка и текст «FSH API» подлежат замене на Edvantix независимо от фичи.

## Что сделать

- [ ] Пока `EDX-008` не готов: убрать внешнюю ссылку на fullstackhero, поправить копирайт
      («FSH API» → «Edvantix API»), оставить честный пустой стейт без ссылки на чужой репозиторий.
- [ ] После `EDX-008`: список ключей (имя, префикс, дата, последнее использование, статус),
      создание с одноразовым показом секрета, отзыв с подтверждением.
- [ ] API-модуль в `src/api/`, гейт на `Permissions.Identity.ApiKeys.*`.
- [ ] Playwright (route-mocked) на список / создание / отзыв.
- [ ] Обновить [[Карта экранов]].

## Зависимости

- Блокируется: [[EDX-008 API-ключи в Identity]]
- Связано: —

## Проверка

- `npm run build`; экран больше не ссылается на внешние репозитории.
