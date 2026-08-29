# Module: StudyGroups

Учебные группы — привязывают опубликованный курс (Curriculum) к преподавателю и составу учеников
на период: жизненный цикл `Forming → Active → Finished/Cancelled`, зачисления с проверкой
`Capacity`, перевод/пауза/возобновление. Даты и люди — здесь, а не в Curriculum (шаблоны курса вне
времени, см. `ADR-006`). Названо `StudyGroup`, не `Group` — `Identity.Group` уже занимает это имя
для групп доступа (`ADR-005`). Module `Order = 610` — после People (550) и Curriculum (600), оба
маркера уже доступны на момент загрузки. Справочник: `docs/02 Модули/StudyGroups.md`.

**Entities / DbContext:** `StudyGroup` (`AggregateRoot`, `ISoftDeletable`), владеет `Enrollments` и
`Teachers` как EF owned-коллекциями (в отличие от плоской модели Curriculum — см. `curriculum.md`);
`GroupEnrollment` (историческая запись, не `ISoftDeletable` — `Left` вместо удаления строки),
`GroupTeacher` (жёсткое удаление, без истории). `StudyGroupsDbContext`, схема `study_groups`.

**Areas:** StudyGroups (CRUD, поиск, `/my`, жизненный цикл activate/finish/cancel), Enrollments
(enroll/unenroll/pause/resume/transfer, состав группы, «мои группы» ученика), Teachers
(add/remove на ростере группы).

## Gotchas / patterns to copy

- **Вложенный агрегат, не плоская персистентность** — `Enrollments`/`Teachers` мутируются только
  через методы `StudyGroup` (`Enroll`, `Unenroll`, `PauseEnrollment`, `AddTeacher`, …), никогда не
  сохраняются напрямую. Причина: инварианты «одно активное зачисление на ученика» и `Capacity`
  проверяются в памяти агрегата за один SaveChanges, без второго round-trip — состав группы малый
  (десятки, не сотни строк), в отличие от `Course`/`CourseModule`/`Lesson` в Curriculum.
- **`GroupEnrollment` — историческая запись.** `Unenroll` не удаляет строку, а ставит
  `Status = Left` + `LeftOn`/`LeaveReason` — так посещаемость/начисления за период до ухода не
  теряются. Повторное зачисление того же ученика после `Left` создаёт новую строку
  (`StudyGroup.Enroll` разрешает это явно — фильтр `e.Status != Left` на проверке дубликата).
- **`ActiveEnrollmentCount` считает `Active` И `Paused`.** Пауза откладывает решение «уходить или
  нет», но не освобождает место в `Capacity` — не путать с `Left`/`Completed`, которые место
  освобождают. Используется и в проверке `Capacity` при `Enroll`, и в проверке `Update` (нельзя
  понизить `Capacity` ниже текущего активного состава).
- **`Finish` замораживает состав**: все `Active`/`Paused` зачисления становятся `Completed`
  bulk'ом, так что история группы читается как «кто закончил курс», а не «кто ни разу не ушёл».
  После `Finished`/`Cancelled` группа полностью заморожена (`EnsureNotFrozen`) — `Update`,
  `Enroll`, `Unenroll`, `Pause`/`Resume` все бросают `CustomException` 409.
- **`TransferEnrollmentCommand` — атомарный перевод**, не «unenroll + отдельный enroll» с двумя
  транзакциями: оба вызова (`sourceGroup.Unenroll` + `targetGroup.Enroll`) идут в одном
  `SaveChangesAsync`. Коммерческие условия (`TariffId`, `DiscountPercent`) переносятся как есть —
  перевод не новый договор.
- **`Course.IsPublished` проверяется через `ICourseQueryService`**, не локальным FK — разные
  модули, связь только через `.Contracts` (`architecture.md`, правило 1). Проверка синхронная и
  только на `Create`: уже созданная `Forming`-группа, чей курс архивируется позже, не блокируется
  автоматически — это ловит подписка на `CourseArchivedIntegrationEvent` (см. ниже), не хендлер.
- **`ChatChannelId` заполняется по событию из Chat.** `StudyGroup.SetChatChannel(channelId)`
  (идемпотентно) вызывается из `StudyGroupChannelLinkedIntegrationEventHandler`
  (`IntegrationEventHandlers/`) в ответ на `StudyGroupChannelLinkedIntegrationEvent`, который
  Chat публикует после провижининга канала группы. Модуль ссылается на `Chat.Contracts` (только
  событие). Прямой ссылки Chat → StudyGroups в рантайме нет — связь закрыта событием.
- **Подписки на People/Curriculum — операционные флаги, не автокоррекция.**
  `StudentArchivedIntegrationEvent` → закрывает активные/приостановленные зачисления студента.
  `TeacherDeactivatedIntegrationEvent` → помечает группу через `AddSystemFlag`, только если на
  ростере нет другого преподавателя (`Teachers.Any(t => t.TeacherId != event.TeacherId)`) —
  ничего не переназначает и не блокирует. `CourseArchivedIntegrationEvent` → аналогично помечает
  `Forming`-группы этого курса (ловит то, что синхронная проверка на `Create` не может — курс
  архивировали после создания группы). `AddSystemFlag` идемпотентен (не дублирует строку в
  `Notes` при повторной доставке события).
- **`IStudyGroupQueryService` — без кэша**, как `ICourseQueryService` (в отличие от кэшируемого
  `IPeopleScopeResolver`): Scheduling/Payments вызывают его на команду, не на элемент горячего
  списка. `GetActiveStudentIdsAsync`/`IsStudentActiveInGroupAsync` считают «активен на дату» как
  `EnrolledOn <= onDate && (LeftOn == null || LeftOn > onDate)` — ушедший ровно в `onDate` не
  считается активным в этот день.
- **`GetMyStudyGroupsQuery` не показывает группы подопечных представителя** — только «сам
  преподаватель или сам ученик» через `IPeopleScopeResolver`. Группы ребёнка представитель видит
  через `GetStudentEnrollmentsQuery`/профиль ученика в People, не через этот эндпоинт.
- **Не звать `AddEventingCore()` в `StudyGroupsModule`** — `IdentityModule` уже регистрирует его;
  модуль вызывает только `AddEventingForDbContext<StudyGroupsDbContext>()`.
  `StudyGroupsDbContext` объявляет `DbSet<OutboxMessage>`/`DbSet<InboxMessage>` с первой миграции
  (не второй, как пришлось чинить People).
- **Роутинг плоский**, без сегмента `/study-groups` сверх имени ресурса — как у People/Curriculum:
  `/api/v1/study-groups`, `/api/v1/study-groups/{id}/enrollments`, и т. д. `/study-groups/my`
  зарегистрирован рядом с `/study-groups/{id}` — маршрут `{id:guid}` не перехватывает `my`.
