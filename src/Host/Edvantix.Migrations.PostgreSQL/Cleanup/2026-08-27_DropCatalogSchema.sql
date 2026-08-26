-- Drops the `catalog` schema left behind by the removed demo Catalog module
-- (Brand/Category/Product/ProductImage — see docs/05 Решения (ADR)/ADR-002
-- Catalog заменяется на Curriculum.md and docs/04 Задачи/Задачи · Удаление Catalog.md).
--
-- This is a standalone script, NOT an EF Core migration — the Catalog module (and its
-- DbContext) no longer exists, so there is nothing left to generate/apply a migration
-- for. Run it by hand, per tenant database, after the backend removal has shipped.
--
-- ⚠ PRODUCTION-SENSITIVE — take a backup first if this tenant DB has ever run
-- `seed-demo` or any real Catalog traffic. On an empty/dev installation it's simpler
-- to just recreate the database than to run this script.
--
-- Usage (adjust connection target as needed):
--   psql "$DatabaseOptions__ConnectionString" -f "2026-08-27_DropCatalogSchema.sql"

DROP SCHEMA IF EXISTS catalog CASCADE;
