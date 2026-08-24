# Module: Payments

Деньги внутри школы: ученик платит школе. Тарифы (`Tariff`) → счета (`StudentInvoice`, владеет
`InvoiceLine`/`PaymentConfirmation`). Платёжного шлюза нет — только ручное подтверждение менеджером
и сторнирование вместо редактирования. Module `Order = 630` — сразу после Scheduling (620): начисление
опрашивает `ISessionPlanQueryService`/`IAttendanceQueryService` и активные зачисления StudyGroups
(610). Справочник: `docs/02 Модули/Payments.md`.

**Entities / DbContext:** `PaymentsDbContext`, схема `payments`. `Tariff` — плоская сущность
(`BaseEntity<Guid>`, как `Room` в Scheduling). `StudentInvoice` — агрегат-корень (`AggregateRoot<Guid>`),
владеет `Lines`/`Payments` как отдельные `DbSet` с FK-каскадом (та же модель, что StudyGroup/
Enrollments — не EF `OwnsMany`). `Status` **никогда не задаётся напрямую** — только
`StudentInvoice.Recalculate()`, вызываемый из `ConfirmPayment`/`ReversePayment`.

## Gotchas / patterns to copy

- **`IInvoicePdfRenderer` — своя реализация, не из Billing.** Справочник изначально предполагал
  переиспользование интерфейса из Billing «без слияния модулей», но `Billing.Services.
  IInvoicePdfRenderer` лежит в рантайм-неймспейсе, не в `Modules.Billing.Contracts` — ссылка нарушила
  бы границу модулей (`Architecture.Tests`). Payments объявляет независимый интерфейс/реализацию на
  QuestPDF (`Modules.Payments/Services/InvoicePdfRenderer.cs`), тот же вид документа.
- **Начисление считается вживую, не кешируется.** `ITariffAccrualService` на каждый вызов
  `BulkGenerateInvoicesCommand` опрашивает `ISessionPlanQueryService.CountPlannedSessionsAsync`
  (PerMonth, пропорционально пересечению периода счёта и окна зачисления) и
  `IAttendanceQueryService.GetBreakdownAsync` (PerLesson, минус `Excused`, если не
  `ChargeOnExcusedAbsence`). Из-за этого решения подписки на `SessionHeld`/`SessionCancelled`/
  `StudentUnenrolled` не «накапливают» состояние — `IDraftInvoiceRefreshService` просто пересчитывает
  единственную тарифную строку уже сгенерированных, но ещё не выставленных `Draft`-счетов группы (не
  трогает вручную отредактированные — больше одной строки). `StudentEnrolled` — намеренный no-op,
  задокументированный в самом хендлере: добавлять для него нечего, будущий `BulkGenerateInvoicesCommand`
  и так подхватит нового ученика живым запросом.
- **`StudentArchived` удаляет черновики, не отменяет.** `StudentInvoice.Cancel` сам отказывается
  переводить `Draft` в `Cancelled` («delete it instead» — это не сформированный документ). Выставленные/
  частично оплаченные счета не трогаются — задолженность архивированного ученика сохраняется, как
  требует справочник.
- **Расширения `StudyGroups.Contracts.IStudyGroupQueryService`** ради Payments (все три — аддитивные,
  без изменения существующих сигнатур): `GetActiveEnrollmentsWithTariffAsync` (роструктура + `TariffId`/
  `DiscountPercent`/`EnrolledOn`/`LeftOn` для начисления), `GetActiveStudyGroupIdsAsync` (перебор для
  `MonthlyInvoiceDraftJob`, по образцу `GenerateSessionsJob` над `ScheduleTemplate`).
- **`ReversePayment` ищет счёт по `PaymentId`, не по `InvoiceId`.** `PaymentConfirmation` не имеет
  собственного Mediator-эндпоинта на верхнем уровне — маршрут `/payments/{paymentId}/reverse` находит
  `InvoiceId` через `PaymentConfirmations.Where(p => p.Id == paymentId).Select(p => p.InvoiceId)`, потом
  загружает агрегат `StudentInvoice` целиком (с `Include(Payments)`) и мутирует через него — сторно
  всегда идёт через доменный метод, никогда не создаётся как самостоятельная строка в хендлере.
- **Нумерация счетов — временная схема, открытый вопрос.** `StudentInvoice.GenerateNumber` — 
  `INV-{год}-{первые 8 hex Guid'а}`, не последовательная. Финальный формат — 
  `docs/04 Задачи/Открытые вопросы.md` → «Payments» → «Нумерация счетов»; при его решении поменять
  только этот статический метод, вызывающий код (`StudentInvoice.Create`) не зависит от формата.
- **`StudentInvoiceDueSoonIntegrationEvent` — не из справочника.** Таблица «Публикуемые события» в
  `docs/02 Модули/Payments.md` называет только Issued/Confirmed/Cancelled/Overdue; `PaymentReminderJob`
  («напоминания за N дней до DueDate») без события-носителя не имел смысла, поэтому событие добавлено
  сверх исходной спецификации. `ReminderDays = 3`, фиксированная точка (не «в течение N дней, каждый
  день») — та же идемпотентность без флага, что у Scheduling's `SessionReminderJob`.
- **`DetectOverdueInvoicesJob` не одноразовый.** В отличие от `PaymentReminderJob`, публикует
  `StudentInvoiceOverdue` каждый день, пока счёт остаётся просроченным — нет «уже уведомили» флага,
  свежие `Debt`/`DaysOverdue` в каждой публикации осмысленны сами по себе.
- **`MonthlyInvoiceDraftJob` не читает флаг «включено».** Такого флага нет ни в `TenantSettings`, ни в
  `StudyGroup` (см. `docs/04 Задачи/Открытые вопросы.md`). Вместо этого джоба безусловно прогоняет
  `BulkGenerateInvoicesCommand` по каждой `Active`-группе — команда и так молча пропускает учеников без
  разрешаемого тарифа, так что для школ без настроенных тарифов джоба просто ничего не создаёт.
- **Права без отдельного `Update` у `StudentInvoices`.** Справочник определяет только `View`/`ViewOwn`/
  `Create`/`Issue`/`Cancel`/`Export` — `UpdateStudentInvoiceCommand` гейтится тем же `Create`, что и
  создание черновика (нет отдельного действия «редактировать черновик» в таблице прав).
- **Эндпоинты `Bulk*`/`Reverse*` названы без этих слов в имени класса.** `Architecture.Tests`
  (`EndpointConventionTests.Endpoint_Names_Should_Follow_Convention`) требует, чтобы имя класса
  начиналось с распознанного глагола — «Bulk» и «Reverse» в список не входят. `BulkGenerateInvoicesCommand`
  → класс `GenerateInvoicesEndpoint`; `BulkIssueInvoicesCommand` → `IssueInvoicesEndpoint`;
  `ReversePaymentCommand` → `RevokePaymentEndpoint` (совпадает с именем права `StudentPayments.Revoke`).
  Команда/маршрут/`WithName` при этом сохраняют исходные («Bulk»/«Reverse») — это доменная лексика,
  ограничение только на класс-эндпоинт.
- **`PaymentProofAccessPolicy`** — `IFileAccessPolicy` для `OwnerType = "PaymentProof"`, `OwnerId` =
  `InvoiceId` (по образцу Curriculum's `LessonMaterialAccessPolicy`, которая ключуется на урок, а не на
  отдельный файл). Чтение открыто — гейт на уровне эндпоинта (`StudentPayments.View`), не в политике.
- **Остаток пакета (`PerPackage`) — проекция в `GetStudentBalanceQueryHandler`, не ledger.**
  `StudentBalanceDto.Packages` (`PackageBalanceDto[]`) считается живьём через тот же
  `IAttendanceQueryService.CountHeldSessionsAsync`, которым уже пользуется `PerLesson`-начисление
  — окно `[Invoice.IssuedOn, ExpiresOn или без верхней границы]`, `ExpiresOn = IssuedOn +
  Tariff.ValidDays` (`ValidDays = 0` → бессрочно). После истечения окно подсчёта замораживается
  на дате истечения — `RemainingCount` дальше не меняется. Нет выбора «активного» пакета при
  нескольких одновременных `PerPackage`-счетах — отдаются все независимо друг от друга (известный
  краевой случай — двойной учёт занятия при пересекающихся окнах, принят сознательно). Ledger-
  вариант (декремент по `SessionHeldIntegrationEvent`) отклонён — конфликтовал бы с тем, что
  `SessionHeldIntegrationEventHandler`/`DraftInvoiceRefreshService` уже сознательно не трогают
  выставленные `PerPackage`-строки («fixed at generation time by design»). Обоснование — docs/02
  Модули/Payments.md, примечание в начале файла.
