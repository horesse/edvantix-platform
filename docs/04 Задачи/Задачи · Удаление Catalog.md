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

> [!warning] Три ловушки слепого поиска по строке `catalog`
> 1. **`--catalog-only`** и `MigratorCommand.CatalogOnly` — это «каталог тенантов»
>    (реестр школ), к модулю Catalog отношения не имеет. **Не трогать.**
>    Там же комментарии `[tenant-catalog]` в `DbMigrator/Program.cs`.
> 2. **`clients/dashboard/src/api/permissions-catalog.ts`** — каталог прав из [[Identity]].
>    **Не трогать.**
> 3. **`GetPermissionCatalog`** в Identity — то же самое. **Не трогать.**

## Backend

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

- `src/Host/Edvantix.DbMigrator/DemoSeed/DemoSeeder.cs` — убрать сид товаров, брендов,
  категорий. Заменить на сид курсов, групп, учеников (см. [[Задачи · Новые модули]]).
- `src/Host/Edvantix.DbMigrator/MigratorCommand.cs` — строка 67, справка `seed-demo`:
  слово «catalog» в описании демо-тенантов.

### Схема БД

Отдельной миграцией в новой папке (не в удаляемой `Catalog/`):

```sql
DROP SCHEMA IF EXISTS catalog CASCADE;
```

> [!warning] На проде — только после резервной копии
> Если где-то уже развёрнута база с данными Catalog, сначала бэкап. На пустой
> инсталляции можно просто удалить папку миграций и пересоздать БД.

## Frontend (`clients/dashboard`)

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

- `src/Tests/Catalog.Tests/` — удалить
- `src/Tests/Architecture.Tests` — убрать Catalog из списков модулей, если перечислен явно
- `src/Tests/Integration.Tests` — сценарии с товарами
- `clients/dashboard/tests/` — спеки каталога
- `clients/admin/tests/roles/roles.spec.ts` — проверить: если ожидает права Catalog
  в каталоге прав, поправить

## Прочее в репозитории

| Файл | Что |
|---|---|
| `.agents/rules/modules/catalog.md` | удалить, создать `curriculum.md` |
| `AGENTS.md` | в индексе правил заменить `catalog` на `curriculum` |
| `README.md` | упоминания демо-каталога |
| `.github/workflows/` | проверить пути в path-фильтрах |

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

Затем — поиск остатков (помня о трёх ловушках выше):

```bash
grep -rn "Catalog" --include=*.cs --include=*.ts --include=*.tsx src clients --exclude-dir=obj --exclude-dir=bin --exclude-dir=node_modules
```

Ожидаемые допустимые совпадения: `permissions-catalog`, `GetPermissionCatalog`,
`CatalogOnly` / `--catalog-only`, `[tenant-catalog]`.

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
