---
tags: [модуль, новый, scheduling]
статус: проектируется
порядок: 620
схема: scheduling
---

# Scheduling

← [[Edvantix]] · [[Карта модулей]] · задачи: [[Задачи · Новые модули]]

> 🟡 Проектируется · порядок `620` · схема `scheduling`

## Назначение

Расписание занятий и посещаемость. Шаблон повторения → сгенерированные занятия →
отметки присутствия. Самый сложный из новых модулей: часовые пояса, повторяемость,
конфликты ресурсов.

## Домен

```mermaid
erDiagram
    ScheduleTemplate ||--o{ Session : "генерирует"
    Session ||--o{ Attendance : ""
    Session ||--o| Session : "RescheduledFromId"
    Room ||--o{ Session : ""

    ScheduleTemplate {
        Guid Id PK
        Guid StudyGroupId "→ StudyGroups"
        DayOfWeek DayOfWeek
        TimeOnly StartTime "локальное время школы"
        int DurationMinutes
        Guid RoomId "nullable"
        Guid TeacherId "nullable, переопределение"
        DateOnly ValidFrom
        DateOnly ValidTo "nullable"
        bool IsActive
    }
    Session {
        Guid Id PK
        Guid StudyGroupId "→ StudyGroups"
        Guid LessonId "nullable → Curriculum"
        Guid TeacherId "→ People"
        Guid RoomId "nullable"
        DateTimeOffset StartUtc
        DateTimeOffset EndUtc
        SessionStatus Status
        string Topic "переопределение темы урока"
        string MeetingUrl
        string CancelReason
        Guid RescheduledFromId "nullable"
        Guid ScheduleTemplateId "nullable"
        string TeacherComment
    }
    Attendance {
        Guid Id PK
        Guid SessionId FK
        Guid StudentId "→ People"
        AttendanceStatus Status
        string Comment
        string MarkedByUserId
        DateTimeOffset MarkedAtUtc
    }
    Room {
        Guid Id PK
        string Name
        int Capacity
        string Location
        bool IsVirtual
    }
    NonWorkingDay {
        Guid Id PK
        DateOnly Date
        string Description
    }
```

### Перечисления

`SessionStatus` — `Planned` `Held` `Cancelled` `Rescheduled`
`AttendanceStatus` — `Present` `Absent` `Late` `Excused`

### Инварианты

- `EndUtc > StartUtc`; занятие принадлежит ровно одной группе.
- `LessonId` **опционален**: консультация, отработка, пробное занятие программе не
  соответствуют ([[ADR-006 Урок программы и занятие расписания]]).
- `Topic` пусто → тема берётся из `Lesson.Title`.
- Записи `Attendance` создаются при переводе занятия в `Held`, по списку активных
  на дату учеников (`IStudyGroupQueryService.GetActiveStudentIdsAsync`) — не при
  создании занятия: состав группы к тому моменту ещё изменится.
- Уникальный индекс `(ScheduleTemplateId, StartUtc)` — повторный прогон генератора
  не создаёт дублей.
- Отменённое занятие не порождает начислений в [[Payments]].
- Посещаемость по занятию, вошедшему в выставленный счёт, меняется только по праву
  `Attendance.MarkAny`.

### Время

> [!danger] Хранится UTC, считается в часовом поясе школы
> `StartUtc` / `EndUtc` — `DateTimeOffset` в UTC. часовой пояс школы (`TenantSettings.TimeZoneId`, IANA)
> применяется при **генерации** занятий из шаблона и при **отображении**.
>
> Шаблон «каждый вторник в 18:00» после перехода на летнее время должен остаться
> в 18:00 по местному — значит UTC-момент сдвигается. Генератор пересчитывает через
> `TimeZoneInfo`, а не прибавляет 7 дней к UTC.
>
> `ScheduleTemplate.StartTime` — локальное время, `Session.StartUtc` — UTC.
> Это единственное место в системе, где локальное время хранится в БД.

### Генерация

```mermaid
flowchart TB
    T["ScheduleTemplate"] --> G{Генератор}
    P["Горизонт: N недель"] --> G
    TZ["TenantSettings.TimeZoneId"] --> G
    H["NonWorkingDay"] --> G
    G --> Chk{"ISessionConflictChecker"}
    Chk -->|конфликт| Rep["Отчёт: пропущено"]
    Chk -->|ок| S["Session · Planned"]
```

Периодическое задание Hangfire держит горизонт; ручной запуск — по праву
`Sessions.Generate`. Перед применением доступен предпросмотр.

### Конфликты

`ISessionConflictChecker` проверяет три пересечения:

| Ресурс | Правило |
|---|---|
| Преподаватель | не ведёт два занятия одновременно |
| Аудитория | одно занятие за раз; только при `IsVirtual = false` |
| Группа | у группы нет двух занятий одновременно |

При генерации конфликт → занятие пропускается и попадает в отчёт.
При ручном создании → `409 Conflict` с описанием. Флаг `force: true` разрешает
принудительно (подмена преподавателя иногда легальна).

## Контракты

`Modules.Scheduling.Contracts`

### Команды

| Команда | Область |
|---|---|
| `CreateSessionCommand` · `UpdateSessionCommand` | Sessions |
| `HoldSessionCommand` | Sessions — создаёт записи посещаемости |
| `CancelSessionCommand` · `RescheduleSessionCommand` | Sessions |
| `CreateScheduleTemplateCommand` · `UpdateScheduleTemplateCommand` · `DeleteScheduleTemplateCommand` | Templates |
| `GenerateSessionsCommand` | Templates |
| `MarkAttendanceCommand` | Attendance — массовая отметка по занятию |
| `CreateRoomCommand` · `UpdateRoomCommand` · `DeleteRoomCommand` | Rooms |
| `AddNonWorkingDayCommand` · `RemoveNonWorkingDayCommand` | Calendar |

### Запросы

| Запрос | Возвращает |
|---|---|
| `SearchSessionsQuery` | диапазон дат, группа, преподаватель, аудитория |
| `GetSessionByIdQuery` | `SessionDetailDto` — с материалами урока и посещаемостью |
| `GetMyScheduleQuery` | своё расписание по роли |
| `GetCalendarQuery` | агрегированный вид для календаря |
| `PreviewGenerationQuery` | что будет создано + конфликты, без записи |
| `GetSessionAttendanceQuery` | `IReadOnlyList<AttendanceDto>` |
| `GetStudentAttendanceQuery` | история ученика |
| `GetGroupAttendanceReportQuery` | сводка по группе за период |
| `GetScheduleTemplatesQuery` · `GetRoomsQuery` · `GetNonWorkingDaysQuery` | справочники |

### DTO

`SessionDto` · `SessionDetailDto` · `CalendarEntryDto` · `AttendanceDto` ·
`AttendanceReportDto` · `ScheduleTemplateDto` · `GenerationPreviewDto` ·
`SessionConflictDto` · `RoomDto` · `NonWorkingDayDto`

### Публикуемые события

| Событие | Содержимое |
|---|---|
| `SessionScheduledIntegrationEvent` | `SessionId`, `StudyGroupId`, `StartUtc` |
| `SessionCancelledIntegrationEvent` | `SessionId`, `StudyGroupId`, `Reason` |
| `SessionRescheduledIntegrationEvent` | `SessionId`, `NewSessionId`, `OldStartUtc`, `NewStartUtc` |
| `SessionHeldIntegrationEvent` | `SessionId`, `StudyGroupId`, `LessonId?`, `HeldAtUtc` |
| `AttendanceMarkedIntegrationEvent` | `SessionId`, `StudentId`, `Status` |

### Сервисы для других модулей

```csharp
public interface IAttendanceQueryService
{
    ValueTask<int> CountHeldSessionsAsync(
        Guid studentId, Guid studyGroupId, DateOnly from, DateOnly to,
        CancellationToken ct = default);

    ValueTask<AttendanceBreakdown> GetBreakdownAsync(
        Guid studentId, Guid studyGroupId, DateOnly from, DateOnly to,
        CancellationToken ct = default);
}

public interface ISessionPlanQueryService
{
    ValueTask<int> CountPlannedSessionsAsync(
        Guid studyGroupId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
```

Первый — основа потарифного начисления, второй — пропорционального расчёта
помесячного тарифа в [[Payments]].

### Подписки

| Событие | Реакция |
|---|---|
| `StudyGroupActivatedIntegrationEvent` | разрешить генерацию |
| `StudyGroupFinishedIntegrationEvent` | остановить генерацию |
| `StudentEnrolledIntegrationEvent` | учесть в будущих занятиях |
| `StudentUnenrolledIntegrationEvent` | снять с будущих занятий |
| `TeacherDeactivatedIntegrationEvent` (People) | пометить занятия без преподавателя |

### Реальное время

Изменения расписания транслируются в dashboard через SignalR: у преподавателя открыт
календарь, менеджер переносит занятие — обновление без перезагрузки.

## Права

| Ресурс | Действия |
|---|---|
| `Sessions` | `View` `ViewOwn` `Create` `Update` `Cancel` `Reschedule` `Generate` |
| `Attendance` | `View` `ViewOwn` `Mark` `MarkAny` |
| `Rooms` | `View` `Manage` |
| `ScheduleTemplates` | `View` `Manage` |

`Sessions.Generate` отделено от `Create`: затрагивает сотни записей.
`Attendance.MarkAny` — правка задним числом после закрытия периода.

## HTTP API

```
GET    /api/v1/sessions
GET    /api/v1/sessions/my
GET    /api/v1/sessions/calendar
POST   /api/v1/sessions
GET    /api/v1/sessions/{id}
PUT    /api/v1/sessions/{id}
POST   /api/v1/sessions/{id}/hold
POST   /api/v1/sessions/{id}/cancel
POST   /api/v1/sessions/{id}/reschedule

GET    /api/v1/study-groups/{id}/schedule-templates
POST   /api/v1/study-groups/{id}/schedule-templates
PUT    /api/v1/schedule-templates/{id}
DELETE /api/v1/schedule-templates/{id}
POST   /api/v1/schedule-templates/{id}/preview
POST   /api/v1/schedule-templates/{id}/generate

GET    /api/v1/sessions/{id}/attendance
PUT    /api/v1/sessions/{id}/attendance
GET    /api/v1/students/{id}/attendance
GET    /api/v1/study-groups/{id}/attendance-report

GET    /api/v1/rooms                            + CRUD
GET    /api/v1/non-working-days                 + CRUD
```

## Задания Hangfire

| Задание | Расписание | Что делает |
|---|---|---|
| `GenerateSessionsJob` | ежедневно | держит горизонт занятий в N недель |
| `SessionReminderJob` | ежечасно | напоминания за 24 часа → [[Notifications]] |

## Зависимости

**Ссылается на:** `StudyGroups.Contracts`, `People.Contracts`, `Curriculum.Contracts`,
`Identity.Contracts`, `Multitenancy.Contracts` (`ITenantSettingsService` — часовой пояс).

**На него ссылаются:** [[Payments]].
**Подписаны на его события:** [[Notifications]], [[Payments]], [[Webhooks]].

## Связанное

[[ADR-006 Урок программы и занятие расписания]] · [[StudyGroups]] · [[Payments]] · [[Задачи · Новые модули]] · [[Открытые вопросы]]
