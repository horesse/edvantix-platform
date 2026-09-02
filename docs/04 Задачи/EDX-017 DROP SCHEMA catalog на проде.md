---
key: EDX-017
aliases: [EDX-017]
tags: [задача, ops]
status: open
area: ops
priority: p2
blocked-by: []
blocks: []
related: ["[[ADR-002 Catalog заменяется на Curriculum]]"]
created: 2026-09-03
closed:
---

# EDX-017 · Ручной `DROP SCHEMA catalog CASCADE` на реальных БД

## Контекст

Модуль `Catalog` удалён из кода: backend PR #12, frontend PR #23 (этап 7). Схема БД
`catalog` на уже развёрнутых инсталляциях автоматически **не** удаляется — это
прод-чувствительная операция.

Скрипт: `src/Host/Edvantix.Migrations.PostgreSQL/Cleanup/2026-08-27_DropCatalogSchema.sql`
(`DROP SCHEMA IF EXISTS catalog CASCADE;`).

## Что сделать

- [ ] На каждой инсталляции с данными Catalog: снять резервную копию БД.
- [ ] Выполнить скрипт.
- [ ] На пустых/новых инсталляциях — не требуется.

## Ловушки слепого поиска по строке `catalog` (не трогать)

1. `--catalog-only` / `MigratorCommand.CatalogOnly`, комментарии `[tenant-catalog]` —
   это «каталог тенантов» (реестр школ), к модулю Catalog отношения не имеет.
2. `clients/dashboard/src/api/permissions-catalog.ts` — каталог прав из [[Identity]].
3. `GetPermissionCatalog` в Identity — то же самое.

## Зависимости

- Блокируется: —
- Связано: [[ADR-002 Catalog заменяется на Curriculum]]
