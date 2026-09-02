---
tags: [задачи, миграция, удаление]
---

# Задачи · Удаление Catalog

← [[Бэклог]] · справочник: [[Catalog (удаляется)]] · [[ADR-002 Catalog заменяется на Curriculum]]

> [!success] Сделано — backend (PR #12, 2026-08-27) и frontend (PR #23, этап 7)
> **Backend.** Модуль `Catalog` (оба проекта), `Catalog.Tests`, 9 миграций + snapshot,
> все четыре точки регистрации (`Edvantix.Api`/`Edvantix.DbMigrator` — Mediator markers
> ×2 + `moduleAssemblies`), `ProjectReference` в трёх `.csproj` и запись в
> `src/Edvantix.slnx` удалены. `HandlerValidatorPairingTests` очищен от Catalog-исключений.
> `DemoSeeder.SeedTenantCatalogAsync` → `SeedTenantSchoolAsync` (курсы/разделы/уроки,
> преподаватели, ученики+опекуны, группы, расписание ±2 недели с посещаемостью, счета).
> Роль `Manager` лишилась `CatalogPermissions.*`, получила People/Curriculum/StudyGroups.
> Попутно исправлен баг `StudentInvoice.GenerateNumber` (брал верхние биты таймстампа
> Guid v7 → коллизии на пакетном выпуске; переключено на младшие 8 hex).
>
> **Frontend.** Экраны `catalog/*`, `api/catalog.ts`, `product-image-manager.tsx`,
> ленивые импорты и маршруты, вкладки корзины, команды палитры убраны; `PagedResponse`
> вынесен в `src/api/pagination.ts`; `image-input.tsx` оставлен (нужен для `settings/
> profile.tsx`). Playwright-спеки каталога удалены, `trash.spec.ts` переписан.
>
> **Проверено:** `dotnet build` 0/0 · `Architecture.Tests` 51/51 · `Integration.Tests`
> 665/666 (1 preexisting skip) · сквозной `DbMigrator apply` + `seed-demo` дважды
> (идемпотентность) · `tsc -b` / `eslint` / `vite build` чисто · Playwright 233 passed.

## Открытые пункты

- [ ] **Прод**: выполнить `DROP SCHEMA IF EXISTS catalog CASCADE;` против реальных БД,
      где уже развёрнуты данные Catalog. Скрипт лежит в
      `src/Host/Edvantix.Migrations.PostgreSQL/Cleanup/2026-08-27_DropCatalogSchema.sql`,
      автоматически **не** выполняется (прод-чувствительная операция, только после
      резервной копии). На пустой инсталляции не требуется.
- [ ] Публичный docs-репозиторий и changelog — правило 10 (общий пункт, см. [[Бэклог]]).

> [!warning] Три ловушки слепого поиска по строке `catalog` (на будущее)
> 1. `--catalog-only` / `MigratorCommand.CatalogOnly`, комментарии `[tenant-catalog]` —
>    это «каталог тенантов» (реестр школ), к модулю Catalog отношения не имеет.
> 2. `clients/dashboard/src/api/permissions-catalog.ts` — каталог прав из [[Identity]].
> 3. `GetPermissionCatalog` в Identity — то же самое.

## Связанное

[[Catalog (удаляется)]] · [[Задачи · Новые модули]] · [[Этапы внедрения]] · [[Бэклог]]
