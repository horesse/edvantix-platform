---
key: EDX-008
aliases: [EDX-008]
tags: [задача, backend]
status: open
area: backend
priority: p3
blocked-by: []
blocks: []
related: []
created: 2026-09-03
closed:
---

# EDX-008 · API-ключи (персистентные токены) в Identity

## Контекст

`clients/dashboard/src/pages/settings/api-keys.tsx` — честная заглушка «coming soon»:
маршрут жив, чтобы nav-ссылки не давали 404, но самой фичи нет. В комментарии указан
ожидаемый эндпоинт `/api/v1/identity/api-keys`. Сейчас доступ только через
user-bound JWT со входа.

## Что сделать

- [ ] Сущность `ApiKey` в `Modules.Identity` (тенант-скоуп): имя, префикс, хэш секрета
      (не хранить сам ключ), `Scopes`/права, `ExpiresUtc`, `LastUsedUtc`, `RevokedUtc`.
- [ ] Команды: `CreateApiKeyCommand` (возвращает секрет один раз), `RevokeApiKeyCommand`;
      запрос `ListApiKeysQuery` (пагинированный → нужен валидатор).
- [ ] Аутентификация по ключу: schema/handler, разбор `Authorization: Bearer fsk_…`
      или заголовка `X-Api-Key`, маппинг на принципала с правами ключа.
- [ ] Права `Permissions.Identity.ApiKeys.*`, миграция, тест изоляции тенантов.
- [ ] Учесть rate limiting / quota (см. `.agents/rules/security.md`).

## Зависимости

- Блокируется: —
- Блокирует: [[EDX-009 Экран API-ключей в настройках]]

## Проверка

- Интеграционный тест: создать ключ → вызвать защищённый эндпоинт с ним → отозвать → 401.
