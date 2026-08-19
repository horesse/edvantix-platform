# Module: Scheduling

> 🟡 В разработке — см. план в `docs/04 Задачи/Задачи · Новые модули.md` → Scheduling.

Расписание занятий и посещаемость. Шаблон повторения (`ScheduleTemplate`) → сгенерированные занятия
(`Session`) → отметки присутствия (`Attendance`). Самый сложный из новых модулей: часовые пояса,
повторяемость, конфликты ресурсов (преподаватель/аудитория/группа). `Session.LessonId` —
**nullable** ссылка на урок программы Curriculum (см. `ADR-006 Урок программы и занятие расписания`).
Module `Order = 620` — после StudyGroups (610): занятие принадлежит учебной группе. Справочник:
`docs/02 Модули/Scheduling.md`.

**Entities / DbContext:** `SchedulingDbContext`, схема `scheduling`. Домен добавляется в шаге 2 плана
реализации — см. справочник для полной ER-диаграммы (`ScheduleTemplate`, `Session`, `Attendance`,
`Room`, `NonWorkingDay`).

Этот файл наполняется по мере реализации (по шагам плана), финально приводится в порядок на
последнем шаге — по образцу `.agents/rules/modules/study-groups.md`.
