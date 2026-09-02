---
key: EDX-020
aliases: [EDX-020]
tags: [задача, backend, tests]
status: open
area: backend
priority: p3
blocked-by: []
blocks: []
related: ["[[EDX-015 Автоблокировка доступа при задолженности]]"]
created: 2026-09-03
closed:
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

- [ ] Воспроизвести локально: `GET /student-invoices/{id}` в цикле сразу после `issue` —
      меняется ли `updatedAtUtc` / версия строки без внешних действий.
- [ ] Найти, что пишет `StudentInvoice` в окне между чтением и `SaveChanges` хендлера
      оплаты (задание, обработчик события, интерсептор).
- [ ] Устранить лишнюю запись **или** добавить `StudentInvoice` настоящий
      concurrency-токен (`xmin` в Npgsql / `rowversion`), чтобы reload-and-retry сходился
      детерминированно; проверить, что коммит #29 (нет 500 при гонке) не регрессирует.
- [ ] В `Integration.Tests/Tests/Curriculum/LessonMaterialsDebtBlockTests` заменить
      обход (разблок флагом) на реальный сценарий: блок 403 → полная оплата счёта →
      материалы снова 200.

## Зависимости

- Связано: [[EDX-015 Автоблокировка доступа при задолженности]], коммит #29, `InvoiceWrite.cs`.

## Проверка

- Интеграционный тест `LessonMaterialsDebtBlockTests` с оплатой по HTTP — зелёный
  без ретрая с паузами.
- `dotnet test src/Tests/Integration.Tests` (Payments) — гонок 409 в счётных сценариях нет.
