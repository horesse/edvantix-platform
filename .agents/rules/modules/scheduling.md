# Module: Scheduling

Расписание занятий и посещаемость. Шаблон повторения (`ScheduleTemplate`) → сгенерированные занятия
(`Session`) → отметки присутствия (`Attendance`). Самый сложный из новых модулей: часовые пояса,
повторяемость, конфликты ресурсов. `Session.LessonId` — **nullable** ссылка на урок программы
Curriculum (см. `ADR-006 Урок программы и занятие расписания`). Module `Order = 620` — после
StudyGroups (610): занятие принадлежит учебной группе. Справочник: `docs/02 Модули/Scheduling.md`.

**Entities / DbContext:** `SchedulingDbContext`, схема `scheduling`, **плоская персистентность** —
пять независимых `DbSet` (`ScheduleTemplate`, `Session`, `Attendance`, `Room`, `NonWorkingDay`), не
вложенный агрегат (в отличие от StudyGroups) — посещаемость и занятия ищутся/пагинируются/
отчитываются независимо от шаблона. `RoomId`/`TeacherId`/`StudyGroupId`/`LessonId` — обычные `Guid`/
`Guid?` без DB-level FK (кросс-модульные ссылки по правилу 1 architecture.md; `RoomId` — тоже, хоть
`Room` и в этом же модуле: деградирует до «место не указано», если комнату удалили, а не ломает
занятие).

## Gotchas / patterns to copy

- **Время — единственное место в системе с хранимым локальным временем.** `ScheduleTemplate.StartTime`
  — локальное время школы (`TimeOnly`); `Session.StartUtc`/`EndUtc` — UTC. Конвертация —
  `ScheduleGeneratorService.ToUtc` (internal, не private — см. ниже), пересчитывается на каждое
  occurrence через `TimeZoneInfo.ConvertTimeToUtc`, **не** прибавлением 7 дней к UTC-моменту —
  иначе шаблон «каждый вторник в 18:00» после перехода на летнее время съедет по местному времени.
  Часовой пояс школы — `ITenantSettingsService.GetCurrentAsync().TimeZoneId` (IANA).
- **`ToUtc` — `internal`, не `private`, специально ради тестируемости.** `[assembly:
  InternalsVisibleTo("Scheduling.Tests")]` в `AssemblyInfo.cs` позволяет тесту вызвать DST-математику
  напрямую, без протаскивания мокаемого «сейчас» через весь `PlanAsync`. Обязательный тест на переход
  на летнее время (`ScheduleGeneratorServiceTests.ToUtc_Should_KeepLocalWallClockTime_...`) держится
  именно на этом.
- **Коллизия имён «сущность vs папка фичи» — реальная ловушка C#, не гипотетическая.** Первая попытка
  назвать папку `Features/v1/Attendance` (как сущность `Attendance`) вызвала CS0118/CS0234: для файла
  под `Features.v1.Sessions.HoldSession` (и вообще любого файла под `Features.v1.*`) необёрнутый
  идентификатor `Attendance` резолвился в СОСЕДНИЙ namespace `Features.v1.Attendance`, а не в тип
  `Domain.Attendance` — резолюция C# ищет вложенные namespace'ы в объемлющих, это не требует `using`.
  Папка переименована в `Features/v1/AttendanceRecords`. Правило на будущее: имя папки фичи не должно
  дословно совпадать с именем сущности домена, если сущность используется в хендлерах ДРУГИХ фич
  того же модуля (тот же принцип, что уже соблюдён у StudyGroups — `Enrollments`/`GroupEnrollment`,
  `Rooms`/`Room` тут же безопасны, поскольку разные слова).
- **`ISessionConflictChecker` — не в Contracts.** Внутренняя деталь модуля (`Services/`), в отличие от
  `IAttendanceQueryService`/`ISessionPlanQueryService`, которые в корне `Modules.Scheduling.Contracts`
  (по образцу `IStudyGroupQueryService`/`ICourseQueryService`). Проверяет три ресурса — преподаватель,
  аудитория (только `IsVirtual = false`), группа — по пересечению `[StartUtc, EndUtc)` среди
  `Planned`/`Held` занятий (отменённые/перенесённые не считаются). `excludeSessionId` обязателен при
  правке существующего занятия — иначе оно конфликтует само с собой.
- **Генератор идемпотентен по факту существования строки, не по флагу.** `ScheduleGeneratorService.
  PlanAsync` перед конфликт-чеком проверяет `Sessions.AnyAsync(ScheduleTemplateId == id && StartUtc ==
  candidateStart)` — уже сгенерированные даты молча пропускаются (не в `ToCreate`, не в `Skipped`;
  повторный прогон — не «пропуск», а no-op). Партиционный уникальный индекс `(ScheduleTemplateId,
  StartUtc) WHERE ScheduleTemplateId IS NOT NULL` — страховка на уровне БД на случай гонки.
- **`ScheduleTemplate.TeacherId` — опциональное переопределение, не обязательное поле.** Пусто →
  берётся `StudyGroup.PrimaryTeacherId` — потребовало добавить `PrimaryTeacherId` в
  `StudyGroupBriefDto`/`IStudyGroupQueryService.GetBriefAsync` (аддитивное расширение контракта
  StudyGroups, единственный вызывающий код уже обновлён). Аналогично `IStudyGroupQueryService.
  GetActiveStudyGroupIdsForStudentAsync` — добавлен ради `GetMyScheduleQuery` (обратной связи «группы
  ученика» не было).
- **`RescheduleSessionCommand` — атомарная замена, не «cancel + create».** Старое занятие получает
  `Status = Rescheduled` через `MarkRescheduled()`, новое создаётся с `RescheduledFromId` = старое —
  оба изменения в одном `SaveChangesAsync`. Конфликт-чек идёт на НОВЫЙ слот с `excludeSessionId` =
  старое занятие.
- **`Attendance` создаётся при `Hold`, не при создании занятия.** `HoldSessionCommandHandler` сеет по
  `IStudyGroupQueryService.GetActiveStudentIdsAsync(studyGroupId, localDate)` — на локальную дату
  занятия (через часовой пояс школы), не на UTC-дату. Идемпотентно: повторный `Hold` уже held-занятия
  не создаёт дублей (проверка `existingStudentIds` перед вставкой). Default-статус новой строки —
  `Present`, не `Absent` — так правка «отметить пропуски» короче для типичного случая.
- **Кросс-модульная резолюция темы занятия (ADR-006).** `GetSessionByIdQueryHandler.ResolveTopicAsync`:
  `Session.Topic` пусто → `IStudyGroupQueryService.GetBriefAsync` (для `CourseId`) →
  `ICourseQueryService.GetLessonsInOrderAsync(courseId)` → найти по `LessonId`. Материалы урока
  **не** подтягиваются на бэкенде — фронтенд берёт их напрямую из Curriculum endpoints (ADR-006).
- **Подписки — избирательные, не «для галочки».** Из пяти документированных в справочнике реализован
  реальный обработчик только для `StudyGroupFinishedIntegrationEvent` (деактивирует шаблоны группы) и
  `TeacherDeactivatedIntegrationEvent` из People (помечает будущие `Planned`-занятия через
  переиспользование `Session.TeacherComment`, идемпотентно). `StudyGroupActivatedIntegrationEvent` —
  без обработчика: «разрешить генерацию» — синхронная проверка `group.Status == Active` в
  `GenerateSessionsCommandHandler`, пересчитывается на каждый вызов, нечего кэшировать в подписке.
  `StudentEnrolled`/`UnenrolledIntegrationEvent` — без обработчика: `Attendance` для будущих занятий
  не хранится заранее (см. пункт выше), реагировать не на что.
- **`Sessions.Hold` не отдельное право.** Гейтится `Sessions.Update` — в справочнике «Права» нет
  действия `Hold`. `NonWorkingDay` (создание/удаление/список) гейтится `ScheduleTemplates.Manage`/
  `.View` — нет отдельного ресурса под школьный календарь. `Attendance.MarkAny` зарегистрировано, но
  не проверяется в `MarkAttendanceCommandHandler` — нечего сверять без Payments (см. «выставлен ли
  счёт за занятие»).
- **`ISessionRealtimeNotifier`** — тонкая обёртка над `IHubContext<AppHub>`, группа `tenant:{id}`,
  событие `SessionScheduleChanged`, payload `SessionDto`. Подключена к CRUD/lifecycle-хендлерам
  занятий, но **не** к генерации (десятки/сотни занятий разом — точечные broadcast не подходят) и не
  к `MarkAttendance` (не двигает карточки календаря).
- **`GenerateSessionsJob`/`SessionReminderJob` — фреш DI-scope на каждый тенант.** `SchedulingDbContext`
  тенант-фильтрован, а фоновая задача не имеет ambient tenant-контекста — оба задания обходят
  `IMultiTenantStore<AppTenantInfo>.GetAllAsync()`, на каждый тенант открывают
  `IServiceScopeFactory.CreateScope()` и ставят `IMultiTenantContextSetter.MultiTenantContext`
  ДО обращения к `SchedulingDbContext` этого scope'а (по образцу `TenantExpiryScanJob`). Падение одной
  связки тенант/шаблон не останавливает остальные (try/catch внутри цикла, лог + продолжение).
  `GenerateSessionsJob` переиспользует `GenerateSessionsCommand` через `IMediator` вместо дублирования
  логики генератора — тот же путь, что и ручной вызов из API.
- **Юнит-тесты — EF Core InMemory, не Testcontainers.** `TestSchedulingDbContextFactory` — по образцу
  `Webhooks.Tests.Services.WebhookFanoutHandlerTests`: `DbContextOptionsBuilder<SchedulingDbContext>
  .UseInMemoryDatabase(...)` + самодельный `IMultiTenantContextAccessor`/`IMultiTenantContextSetter`.
  Быстрое покрытие LINQ-сервисов (`SessionConflictChecker`, `ScheduleGeneratorService`), не замена
  интеграционным тестам на реальном Postgres.
- **Мапить эндпоинт под чужим именем ресурса — устоявшийся приём, не исключение.** `GetTeacherWorkload`
  (query `GetTeacherWorkloadQuery` в `Modules.Scheduling.Contracts`) мапит `GET /teachers/{id}/workload`
  — «teachers» принадлежит People, а не Scheduling. Причина: People обязан оставаться низовым модулем
  (`Order = 550`, ничего не подключает сверху), а нагрузка преподавателя нужна и данные StudyGroups
  (`IStudyGroupQueryService.GetActiveGroupIdsForTeacherAsync` — ещё одно аддитивное расширение), и
  собственные `Session`. Тот же приём уже стоял в `GetStudentAttendanceEndpoint` (`/students/{id}/
  attendance`) — гейтится Scheduling-правом (`Sessions.View`), не People-правом. Если понадобится
  что-то подобное в другом модуле — не тянуть contracts-зависимость «вниз» по порядку, смотреть, не
  дешевле ли смапить чужой маршрут из модуля, которому уже доступны обе стороны данных.
