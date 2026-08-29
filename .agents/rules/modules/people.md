# Module: People

Ученики, преподаватели, представители — профили, контакты, связи между ними. Аутентификация не здесь (см. [[Identity]]) — связь с `FshUser` опциональна: `Student.UserId`/`Teacher.UserId`/`Guardian.UserId` все **nullable**, ребёнку без логина ничего не мешает существовать в системе. Менеджер отдельной сущностью не моделируется — это роль пользователя (`ADR-003`). Module `Order = 550` (после Billing 500, до Catalog 600). Справочник: `docs/02 Модули/People.md`.

**Entities / DbContext:** `Student`, `Teacher`, `Guardian`, `StudentGuardian` (join, `IsPrimaryPayer`), `StudentNote`. Все — `ISoftDeletable`. `PeopleDbContext`, схема `people`.

**Areas:** Students (CRUD, поиск, жизненный цикл `Lead→Active→Paused→Archived`, представители, заметки, привязка учётки, импорт CSV), Teachers (CRUD, поиск, деактивация), Guardians (CRUD, поиск).

## Gotchas / patterns to copy

- **People — низовой модуль**: ни на что не подписывается (`Подписки: Нет` в справочнике). Загружается до StudyGroups/Scheduling/Payments, которые от него зависят через `.Contracts`. Следствие: если новый запрос People нуждается в данных StudyGroups/Scheduling/Payments (как оказалось с `GetTeacherWorkloadQuery` — исходно числился здесь, реализован в `Modules.Scheduling`, эндпоинт `GET /teachers/{id}/workload` мапит Scheduling под чужим именем ресурса), контракт/хендлер уезжает в тот из вышестоящих модулей, которому реально доступны обе стороны — не тащить People.Contracts → StudyGroups/Scheduling.Contracts.
- **Не звать `AddEventingCore()` в `PeopleModule`** — `IdentityModule` уже регистрирует его (шина + `OutboxDispatcherHostedService`); повторный вызов заводит второй hosted-dispatcher, читающий тот же outbox параллельно. Модуль вызывает только `AddEventingForDbContext<PeopleDbContext>()`.
- **`Students.ViewNotes`** — отдельное от `Students.View` право: внутренние заметки менеджеров, преподаватель их не видит.
- **`IsPrimaryPayer`** — ровно один представитель на ученика с этим флагом. Инвариант в хендлере (`SetPrimaryPayerCommand` снимает флаг с прежнего плательщика в той же транзакции), не в БД — правило похоже на «единственный thumbnail» в Catalog (`database.md`).
- **`IPeopleScopeResolver`** — отвечает «кто этот пользователь в предметной области» (`StudentId?`, `TeacherId?`, `GuardianId?`, `WardStudentIds`). Кэш в `HybridCache`, инвалидация по `StudentCreatedIntegrationEvent`/`GuardianLinkedToStudentIntegrationEvent`. Используется StudyGroups/Scheduling/Payments для row-level проверок «своих данных» — без него проверки расползутся по обработчикам.
- **`IPeopleLookupService`** — обязательно batch (`GetStudentsBriefAsync(ids)`), не по одному: списки в Scheduling/Payments иначе упрутся в N+1. `GetStudentContactsAsync(ids)` / `GetTeacherContactAsync(id)` → `ContactDto` (`UserId?`/`Email?`/`DisplayName`/`ContactRole`): адресаты для Notifications и участники чата учебной группы. `UserId` null = нет учётки (in-app недоступен, e-mail — да); опекуны отдаются с флагом `PrimaryPayerGuardian`. Один join-запрос на весь батч.
- **Контакты — источник правды здесь, не в Identity.** `Phone`/`Email` на `Student`/`Teacher`/`Guardian` живут в People; в Identity e-mail нужен только для входа. Расхождение (поменяли в профиле, не в учётке) — принятый компромисс ADR-003.
- **ФИО — три поля** (`LastName`/`FirstName`/`MiddleName`), не одна строка: нужна сортировка по фамилии; отображаемое имя вычисляется, не хранится.
