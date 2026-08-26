---
tags: [задачи, миграция, удаление]
---

# Задачи · Удаление Catalog

← [[Бэклог]] · справочник: [[Catalog (удаляется)]]

Точный список изменений при удалении демо-модуля [[Catalog (удаляется)]].
Обоснование — [[ADR-002 Catalog заменяется на Curriculum]].

> [!danger] Сначала Curriculum, потом удаление
> Catalog — эталон конвенций проекта. Держать его рабочим, пока [[Curriculum]] не написан
> и не проходит тесты. Иначе примеры придётся выкапывать из истории git.

> [!success] Backend — готово (2026-08-27)
> Модуль `Catalog` (оба проекта), `Catalog.Tests`, 9 миграций + snapshot удалены; все четыре
> точки регистрации (`Edvantix.Api/Program.cs`, `Edvantix.DbMigrator/Program.cs` — Mediator
> markers ×2 + `moduleAssemblies`) сняты вместе с `ProjectReference` в трёх `.csproj`
> (`Edvantix.Api`, `Edvantix.DbMigrator`, `Edvantix.Migrations.PostgreSQL`) и записью в
> `src/Edvantix.slnx`. `HandlerValidatorPairingTests` очищен от Catalog-исключений.
> `DemoSeeder.SeedTenantCatalogAsync` заменён на `SeedTenantSchoolAsync` (курсы/разделы/уроки,
> преподаватели, ученики+опекуны, группы, расписание на ±2 недели с посещаемостью, выставленные
> и частично/полностью оплаченные счета) — Acme получает полный объём из «Предложения»
> ([[Открытые вопросы]] → Демо-данные: 3 курса/5 групп/30 учеников/4 преподавателя), Globex —
> облегчённую версию (как и для остального демо-контента этого тенанта). Роль `Manager`
> лишилась прав `CatalogPermissions.*`, взамен получила права People/Curriculum/StudyGroups.
> `.agents/rules/modules/catalog.md` удалён (curriculum.md уже существовал), индекс правил в
> `AGENTS.md` поправлен. `DROP SCHEMA catalog CASCADE` — отдельный SQL-скрипт (не EF-миграция,
> модуля больше нет) в `src/Host/Edvantix.Migrations.PostgreSQL/Cleanup/
> 2026-08-27_DropCatalogSchema.sql`, не выполнялся автоматически (прод-чувствительная операция).
>
> **Проверено:** `dotnet build src/Edvantix.slnx` (0 warnings/errors, `TreatWarningsAsErrors`) ·
> `dotnet test src/Tests/Architecture.Tests` (51/51) · `dotnet test src/Tests/Integration.Tests`
> (665/666, 1 preexisting skip) · `dotnet test src/Tests/Integration.Middleware.Tests` (5/5) ·
> юнит-тесты People/Curriculum/StudyGroups/Scheduling/Payments/Identity — все зелёные ·
> сквозной прогон `DbMigrator apply` + `seed-demo` (дважды, включая повторный запуск на уже
> заполненной БД — идемпотентность подтверждена) на одноразовом Postgres-контейнере.
>
> **Попутные находки, исправлены в этом же PR (иначе `seed-demo` падал):**
> `StudentInvoice.GenerateNumber` (Payments) брал первые 8 hex-символов
> `Guid.CreateVersion7()` — это верхние биты миллисекундного таймстампа, они почти не меняются
> в окне ~65 секунд, поэтому ЛЮБЫЕ два счёта, выставленные в этом окне (например, пакетный
> выпуск счетов на класс), падали на `IX_StudentInvoices_Number`. Переключено на последние 8
> hex-символов (случайные биты). Это баг Payments, не связанный с удалением Catalog напрямую,
> но блокировал требуемые «выставленные счета» в демо-сиде — исправлен точечно, одна строка.
>
> **Не сделано:** публичный docs-репозиторий (`github.com/fullstackhero/docs`) и changelog —
> правило 10 `AGENTS.md`, как и для «Задачи · Новые модули» ни у одной сессии не было доступа к
> этому репозиторию. **Frontend не начат** — раздел ниже, [[Задачи · Frontend]].

> [!warning] Три ловушки слепого поиска по строке `catalog`
> 1. **`--catalog-only`** и `MigratorCommand.CatalogOnly` — это «каталог тенантов»
>    (реестр школ), к модулю Catalog отношения не имеет. **Не трогать.**
>    Там же комментарии `[tenant-catalog]` в `DbMigrator/Program.cs`.
> 2. **`clients/dashboard/src/api/permissions-catalog.ts`** — каталог прав из [[Identity]].
>    **Не трогать.**
> 3. **`GetPermissionCatalog`** в Identity — то же самое. **Не трогать.**

## Backend — готово ✅

### Удалить целиком

```
src/Modules/Catalog/                                    оба проекта
src/Tests/Catalog.Tests/
src/Host/Edvantix.Migrations.PostgreSQL/Catalog/        9 миграций + snapshot
```

### `src/Edvantix.slnx`

```xml
<!-- удалить -->
<Folder Name="/Modules/Catalog/">
  <Project Path="Modules/Catalog/Modules.Catalog.Contracts/Modules.Catalog.Contracts.csproj" />
  <Project Path="Modules/Catalog/Modules.Catalog/Modules.Catalog.csproj" />
</Folder>
<!-- и строку -->
<Project Path="Tests/Catalog.Tests/Catalog.Tests.csproj" />
```

### `src/Host/Edvantix.Api/Program.cs`

| Строка | Что убрать |
|---|---|
| 11 | `using FSH.Modules.Catalog;` |
| 56 | `typeof(FSH.Modules.Catalog.Contracts.CatalogContractsMarker),` |
| 57 | `typeof(FSH.Modules.Catalog.CatalogModule),` |
| 76 | `typeof(CatalogModule).Assembly,` |

### `src/Host/Edvantix.DbMigrator/Program.cs`

| Строка | Что убрать |
|---|---|
| 8 | `using FSH.Modules.Catalog;` |
| 94 | `typeof(FSH.Modules.Catalog.Contracts.CatalogContractsMarker),` |
| 95 | `typeof(FSH.Modules.Catalog.CatalogModule),` |
| 115 | `typeof(CatalogModule).Assembly,` |

**Не трогать** строки 188–243 — там «каталог тенантов».

### Демо-данные

- [x] `src/Host/Edvantix.DbMigrator/DemoSeed/DemoSeeder.cs` — сид товаров/брендов/категорий
  убран. Заменён на `SeedTenantSchoolAsync` (курсы+разделы+уроки, преподаватели,
  ученики+опекуны, группы, расписание, счета) — см. подробности в статусе выше и
  [[Задачи · Новые модули]].
- [x] `src/Host/Edvantix.DbMigrator/MigratorCommand.cs` — справка `seed-demo` переписана,
  слово «catalog» (товарный каталог) убрано из описания демо-тенантов.

### Схема БД

- [x] SQL-скрипт (не EF-миграция — модуля больше нет) добавлен в
  `src/Host/Edvantix.Migrations.PostgreSQL/Cleanup/2026-08-27_DropCatalogSchema.sql`:

```sql
DROP SCHEMA IF EXISTS catalog CASCADE;
```

> [!warning] На проде — только после резервной копии, скрипт НЕ выполнялся автоматически
> Если где-то уже развёрнута база с данными Catalog, сначала бэкап. На пустой
> инсталляции можно просто удалить папку миграций и пересоздать БД. Эта сессия только
> добавила скрипт — прогон против любой реальной БД (dev/staging/prod) должен быть отдельным,
> осознанным шагом оператора.

## Frontend — остаётся (`clients/dashboard`)

### Удалить файлы

```
src/pages/catalog/brands.tsx
src/pages/catalog/categories.tsx
src/pages/catalog/products.tsx
src/pages/catalog/product-detail.tsx
src/api/catalog.ts
src/components/file/product-image-manager.tsx
```

`src/components/file/image-input.tsx` — **проверить**: если используется только
`product-image-manager`, удалить; если переиспользуем для обложек курсов и аватаров — оставить.

### `src/routes.tsx`

Убрать:
- строки 55–61 — ленивые импорты `BrandsPage`, `CategoriesPage`, `ProductsPage`,
  `ProductDetailPage`
- строки 217–224 — маршруты `catalog`, `catalog/brands`, `catalog/categories`,
  `catalog/products`, `catalog/products/:productId`
- строка 13 — комментарий про Catalog в описании стратегии разбиения бандла

### `src/lib/trash-permissions.ts`

Убрать строки 13–15:

```ts
products:   "Permissions.Catalog.Products.Restore",
brands:     "Permissions.Catalog.Brands.Restore",
categories: "Permissions.Catalog.Categories.Restore",
```

Заменить на права корзины новых модулей. Соответственно поправить вкладки
`src/pages/system/trash.tsx`.

### Прочее

| Файл | Что |
|---|---|
| `src/components/layout/nav-data.ts` | пункты каталога, иконки `Package`, `Tags`, `FolderTree` (если больше не нужны) |
| `src/components/command-palette/command-palette-dialog.tsx` | команды каталога |
| `src/pages/overview.tsx` | плитки со статистикой товаров |
| `src/pages/login.demo-accounts.ts` | упоминания прав каталога |
| `src/pages/identity/role-detail.tsx` | проверить группировку прав по модулю Catalog |
| `src/components/route-error.tsx` | проверить ссылки |
| `tests/` | Playwright-тесты каталога |

## Тесты

- [x] `src/Tests/Catalog.Tests/` — удалён
- [x] `src/Tests/Architecture.Tests` — Catalog убран из allowlist'а
      `HandlerValidatorPairingTests` (8 записей), 51/51 зелёных
- [x] `src/Tests/Integration.Tests` — `Tests/Catalog/` (10 файлов) удалён, 665/666
      (1 пропуск не связан), `TestConstants.CatalogBasePath` убран из обоих проектов
      (`Integration.Tests`, `Integration.Middleware.Tests`) как мёртвый код
- [ ] `clients/dashboard/tests/` — спеки каталога (frontend, вне скоупа этой сессии)
- [ ] `clients/admin/tests/roles/roles.spec.ts` — проверить: если ожидает права Catalog
  в каталоге прав, поправить (frontend, вне скоупа этой сессии)

## Прочее в репозитории

| Файл | Что | Статус |
|---|---|---|
| `.agents/rules/modules/catalog.md` | удалить, создать `curriculum.md` | ✅ удалён (`curriculum.md` уже существовал) |
| `AGENTS.md` | в индексе правил заменить `catalog` на `curriculum` | ✅ сделано (заодно вписаны people/study-groups/scheduling/payments — тот же список тоже был устаревшим) |
| `README.md` | упоминания демо-каталога | ✅ сделано |
| `.github/workflows/` | проверить пути в path-фильтрах | ✅ явных path-фильтров не было; но `backend.yml`'s unit-test loop содержал жёсткий список `Тесты/{module}.Tests` — `Catalog` убран, заодно вписаны Curriculum/People/StudyGroups/Scheduling/Payments (были пропущены в CI до этой сессии) |
| `.csproj` (Api/DbMigrator/Migrations.PostgreSQL) | — (не было в доке) | ✅ `ProjectReference` на `Modules.Catalog*` убраны — обнаружено через `dotnet build` (MSB9008), не через grep |

## Проверка после удаления

```bash
dotnet build src/Edvantix.slnx
```

```bash
dotnet test src/Tests/Architecture.Tests
```

```bash
cd clients/dashboard && npm run build
```

> [!success] Backend-проверки прогнаны 2026-08-27
> `dotnet build` — 0 warnings/0 errors · `Architecture.Tests` 51/51 · `Integration.Tests`
> 665/666 (1 preexisting skip) · `Integration.Middleware.Tests` 5/5 · юнит-тесты пяти школьных
> модулей + Identity.Tests — зелёные · сквозной `DbMigrator apply` + `seed-demo` дважды подряд
> (идемпотентность) на одноразовом Postgres-контейнере, данные проверены через `psql`.
> `cd clients/dashboard && npm run build` **не запускался** — frontend вне скоупа
> backend-сессии, см. [[Задачи · Frontend]].

Затем — поиск остатков (помня о трёх ловушках выше):

```bash
grep -rn "Catalog" --include=*.cs --include=*.ts --include=*.tsx src clients --exclude-dir=obj --exclude-dir=bin --exclude-dir=node_modules
```

Ожидаемые допустимые совпадения: `permissions-catalog`, `GetPermissionCatalog`,
`CatalogOnly` / `--catalog-only`, `[tenant-catalog]`, плюс исторические упоминания Catalog в
doc-комментариях модулей People/Curriculum/Files/Identity (сравнение с бывшим эталоном
конвенций — не остаток зависимости, проверено построчно в этой сессии).

## Что НЕ вырезаем

| Модуль | Почему остаётся |
|---|---|
| [[Billing]] | монетизация платформы — школа платит Edvantix ([[ADR-004 Payments отдельно от Billing]]) |
| [[Multitenancy]] | школа = тенант ([[ADR-001 Школа как тенант]]) |
| [[Chat]], [[Tickets]], [[Webhooks]], [[Notifications]], [[Files]], [[Auditing]], [[Identity]] | прямые требования задачи |
| `BuildingBlocks/Quota` | лимиты по планам |
| `BuildingBlocks/Mailing` | письма |

## Связанное

[[Catalog (удаляется)]] · [[Задачи · Новые модули]] · [[Этапы внедрения]] · [[Бэклог]]
