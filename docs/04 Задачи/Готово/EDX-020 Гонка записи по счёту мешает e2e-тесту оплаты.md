---
key: EDX-020
aliases: [EDX-020]
tags: [задача, backend, tests]
status: done
area: backend
priority: p3
blocked-by: []
blocks: []
related: ["[[EDX-015 Автоблокировка доступа при задолженности]]"]
created: 2026-09-03
closed: 2026-09-03
---

# EDX-020 · Гонка записи по счёту мешает e2e-тесту «оплата → доступ вернулся»

## Контекст

При закрытии [[EDX-015 Автоблокировка доступа при задолженности]] не удалось написать
детерминированный интеграционный тест на сценарий «оплатил просроченный счёт → доступ к
материалам вернулся» через HTTP.

Симптом: `POST /api/v1/student-invoices/{id}/payments` в харнессе Testcontainers
стабильно отдаёт **409** «The invoice was modified by another operation. Reload it and
try again.» — `InvoiceWrite.WithConcurrencyRetryAsync`
(`src/Modules/Payments/Modules.Payments/Data/InvoiceWrite.cs`, 3 попытки) исчерпывает
бюджет, и внешний ретрай в тесте (5×, пауза 500 мс) тоже всё время получает 409. То
есть строку счёта что-то переписывает в тугом цикле в течение всего окна теста.

`StudentInvoice` намеренно без row-version (см. коммент в `InvoiceWrite.cs` и коммит #29
«fix(payments): не отдавать 500 при гонке записи по счёту»), поэтому конфликт ловится по
«affected 0 rows», а не по токену — и ретрай не сходится, если писатель не прекращает.

Кандидаты на «писателя»:
- рекуррентные Hangfire-задания, регистрируемые в `PaymentsModule.MapEndpoints`
  (`DetectOverdueInvoicesJob`, `PaymentReminderJob`, `MonthlyInvoiceDraftJob`) — в
  интеграционном хосте поднят `AddHangfireServer` на InMemory-хранилище
  (`FshWebApplicationFactory`), poll-интервал 1 с;
- обработчик интеграционного события `StudentInvoiceIssued` / начисления, трогающий счёт;
- SaveChanges-интерсептор (аудит/eventing) на `PaymentsDbContext`, помечающий
  прочитанные-но-не-изменённые счета как `Modified`.

Пока обход в тесте: `LessonMaterialsDebtBlockTests` проверяет разблок выключением флага
тенанта; «оплата → разблок» покрыта юнит-тестом
`Payments.Tests/Services/MaterialsAccessServiceTests.Not_Restricted_When_The_Only_Past_Due_Invoice_Is_Paid`.

## Что сделать

- [x] Воспроизвести локально — `Integration.Tests/Tests/Payments/ConfirmPaymentRaceTests`:
      `POST /student-invoices/{id}/payments` сразу после `issue` отдаёт `409` **детерминированно**,
      с первого раза, без всякого внешнего писателя.
- [x] Найти причину. Это **не гонка** и не «тугой цикл» — гипотеза из контекста ниже неверна.
      `StudentInvoice`/`InvoiceLine`/`PaymentConfirmation`/`Tariff` сами присваивают
      `Id = Guid.CreateVersion7()`, но в EF-конфигурации ключ оставался store-generated
      (дефолт для Guid PK). При `DetectChanges` новый `PaymentConfirmation`, добавленный через
      **уже отслеживаемый** агрегат `invoice` (`invoice.ConfirmPayment(...)` → `_payments.Add`),
      классифицировался как *существующая* строка (`Modified`, не `Added`) → EF генерил
      `UPDATE "PaymentConfirmations" … WHERE "Id" = @id`, он задевал 0 строк →
      `DbUpdateConcurrencyException` → `InvoiceWrite.WithConcurrencyRetryAsync` жёг все 3 попытки
      (каждая перезагрузка повторяла ту же ошибочную классификацию) → `409`. То же касалось
      `ReversePayment` и `ReplaceLines` (новые `InvoiceLine`).
- [x] Устранить. `.Property(x => x.Id).ValueGeneratedNever()` на всех четырёх сущностях графа
      счёта (`StudentInvoiceConfiguration`, `InvoiceLineConfiguration`,
      `PaymentConfirmationConfiguration`, `TariffConfiguration`). Миграция
      `20260903171258…` → `20260903183523_PaymentsClientAssignedKeys` — **без DDL** (колонки
      Guid PK и так без database default), меняется только снапшот модели.
      Row-version на `StudentInvoice` не добавлялся: реальной гонки нет, а `WithConcurrencyRetryAsync`
      остаётся как страховка от настоящих гонок (второй платёж, `DraftInvoiceRefreshService`) —
      коммит #29 не регрессирует.
- [x] `Integration.Tests/Tests/Curriculum/LessonMaterialsDebtBlockTests` —
      финальный шаг заменён: блок `403` → **полная оплата счёта по HTTP** → материалы снова `200`
      (вместо снятия флага тенанта).

## Итог

- Правка чисто в EF-маппинге Payments (4 конфигурации + пустая миграция-снапшот). Домен,
  хендлеры, контракты, HTTP API — без изменений.
- Тесты: `Payments.Tests` 74/74; `Integration.Tests` — `ConfirmPaymentRaceTests` (оплата +
  сторно по HTTP, с первого раза), `LessonMaterialsDebtBlockTests` (реальная оплата),
  `Payments`/`Billing` счётные сценарии, `Architecture.Tests` 51/51 — зелёные.

## Зависимости

- Связано: [[EDX-015 Автоблокировка доступа при задолженности]], коммит #29, `InvoiceWrite.cs`.

## Проверка

- Интеграционный тест `LessonMaterialsDebtBlockTests` с оплатой по HTTP — зелёный
  без ретрая с паузами.
- `dotnet test src/Tests/Integration.Tests` (Payments) — гонок 409 в счётных сценариях нет.
