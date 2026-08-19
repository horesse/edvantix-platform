---
tags: [задачи, frontend]
---

# Задачи · Frontend

← [[Бэклог]] · справочники: [[Dashboard (школа)]] · [[Admin (операторская)]] · [[Карта экранов]]

Полная таблица экранов со статусами — [[Карта экранов]]. Здесь работы, сгруппированные
по типу.

> [!danger] Правило 9 из `AGENTS.md`
> Данные для конкретного вызова передавать через `mutate(arg)`, **никогда** через
> состояние, которое замыкают колбэки мутации — гонка на момент выполнения.
> Самая частая ошибка в этом коде; проверять на ревью каждой формы.

---

## dashboard · новые API-модули

- [ ] `src/api/people.ts`
- [ ] `src/api/curriculum.ts`
- [ ] `src/api/study-groups.ts`
- [ ] `src/api/scheduling.ts`
- [ ] `src/api/payments.ts`

Тонкие обёртки над `apiFetch` с типами DTO и ключами TanStack Query.
Кодогенерации в проекте нет — типы пишутся руками по контрактам модуля.

---

## dashboard · экраны по этапам

### Этап 1 · People

> [!note] ✅ Backend готов — можно начинать
> Модуль [[People]] полностью реализован (см. [[Задачи · Новые модули]]): все эндпоинты
> из `HTTP API` в справочнике работают, права `PeoplePermissions` зарегистрированы.
> Единственное, чего нет в API — `GET /teachers/{id}/workload` (ждёт [[StudyGroups]]),
> поэтому раздел «нагрузка» на карточке преподавателя пока не строить.

- [ ] `src/api/people.ts` — обёртка над `apiFetch`: типы `StudentDto`/`StudentDetailDto`/
      `TeacherDto`/`GuardianDto`/`StudentGuardianDto`/`StudentNoteDto`/`PeopleScope`/
      `PagedResponse<T>` вручную по контрактам (см. [[People]] → «Контракты»); ключи
      TanStack Query на `students`/`teachers`/`guardians`
- [ ] `/students` — список, фильтры (статус, менеджер, текст), пагинация.
      `GET /api/v1/students`, право `Students.View`
- [ ] `/students/:id` — профиль (`GET /students/{id}` → `StudentDetailDto`), редактирование
      (`PUT`), архивация/восстановление (`POST .../archive`, `.../restore`), привязка/отвязка
      учётки (`POST .../link-user`, `.../unlink-user`), представители (`GET/POST/DELETE
      .../guardians`, `POST .../guardians/{gid}/primary-payer`), заметки — отдельная вкладка
      под правом `Students.ViewNotes` (`GET/POST .../notes`, `DELETE .../notes/{noteId}`).
      Группы/посещаемость/счета — заглушки до [[StudyGroups]]/[[Scheduling]]/[[Payments]]
- [ ] `/students/import` — загрузка CSV (`multipart/form-data`, поле `file`) →
      `POST /students/import?dryRun=true` для предпросмотра (таблица построчных результатов,
      `ImportStudentsResultDto.Rows`, ошибки без блокировки остальных строк) → повторный
      вызов с `?dryRun=false` для записи
- [ ] `/teachers` — список (`GET /teachers`, право `Teachers.View`)
- [ ] `/teachers/:id` — профиль, специализации (`string[]`), ставка, деактивация/активация
      (`POST .../deactivate`, `.../activate`), привязка/отвязка учётки. Блок «нагрузка» —
      не строить, эндпоинта нет (см. заметку выше)
- [ ] `/guardians` — список, подопечные (по клику — переход на карточку ученика через
      `StudentGuardianDto`), привязка/отвязка учётки

### Этап 2 · Curriculum

> [!note] ✅ Backend готов — можно начинать
> Модуль [[Curriculum]] полностью реализован (см. [[Задачи · Новые модули]]): все эндпоинты
> из `HTTP API` в справочнике работают, права `CurriculumPermissions` зарегистрированы
> (`Subjects`/`Courses`/`Lessons`/`LessonMaterials`). Роутинг плоский, без сегмента
> `/curriculum` — как у People. Разделы курса (`CourseModule`) не имеют отдельного ресурса
> прав, их CRUD гейтится `Courses.Update` — учитывать в проверках `perm`/`anyPerm` на UI.

- [ ] `src/api/curriculum.ts` — обёртка над `apiFetch`: типы `SubjectDto`/`SubjectNodeDto`/
      `CourseDto`/`CourseDetailDto`/`CourseModuleDto`/`LessonDto`/`LessonMaterialDto`/
      `PagedResponse<T>` вручную по контрактам (см. [[Curriculum]] → «Контракты»); enum'ы
      `CourseLevel`/`CourseStatus`/`MaterialKind` — string union, сериализуются как строки;
      ключи TanStack Query на `subjects`/`courses`/`lessons`
- [ ] `/subjects` — дерево направлений (`GET /subjects/tree` → `SubjectNodeDto[]`, право
      `Subjects.View`), инлайн создание/переименование/удаление узла (`POST`/`PUT`/`DELETE
      /subjects/{id}`, право `Subjects.Create`/`Update`/`Delete`), перетаскивание для
      `PUT /subjects/order` (`ReorderSubjectsCommand` — принимает `parentId` и упорядоченный
      список id **только для одного уровня**, т.е. drag-n-drop работает в пределах родителя)
- [ ] `/courses` — список (`GET /courses`, право `Courses.View`), фильтры по направлению
      (`subjectId`), статусу (`Draft`/`Published`/`Archived`) и уровню (`CourseLevel`),
      пагинация и сортировка (`sortBy=title|createdAtUtc|durationHours`); отдельная вкладка/
      маршрут `/courses/trash` (`GET /courses/trash`, право `Courses.ViewTrash`) с кнопкой
      восстановления (`POST /courses/{id}/restore`, право `Courses.Restore`)
- [ ] `/courses/:id` — **конструктор курса**: карточка курса (редактирование `title`/
      `description`/`level`/`durationHours`/`subjectId`/`coverFileId` через `PUT /courses/{id}`,
      право `Courses.Update`) + дерево разделов и уроков ниже. `GET /courses/{id}` возвращает
      `CourseDetailDto` с готовым деревом `modules[].lessons[]` — отдельного запроса на дерево
      не нужно. Кнопки жизненного цикла: «Опубликовать» (`POST .../publish`, право
      `Courses.Publish`; сервер вернёт 409, если у курса нет ни одного раздела — показать
      причину, не глотать ошибку), «Архивировать» (`POST .../archive`, тоже `Courses.Publish`),
      «Дублировать» (`POST .../duplicate` → редирект на новый `id`, право `Courses.Create`),
      «Удалить» (`DELETE /courses/{id}` → в корзину, право `Courses.Delete`)
  - [ ] Дерево разделов: создание раздела (`POST /courses/{id}/modules`, право
        `Courses.Update`), инлайн-правка названия/описания (`PUT /modules/{id}`), удаление
        (`DELETE /modules/{id}` — предупредить, что каскадно удалит уроки и материалы раздела),
        перетаскивание (`PUT /courses/{id}/modules/reorder`)
  - [ ] Уроки внутри раздела: создание (`POST /modules/{id}/lessons`, право
        `Lessons.Create`), инлайн-правка title/objectives/content/durationMinutes
        (`PUT /lessons/{id}`, `Lessons.Update`), удаление (`DELETE /lessons/{id}`,
        `Lessons.Delete` — каскадно удаляет материалы урока), перетаскивание
        (`PUT /modules/{id}/lessons/reorder`); автосохранение по правилу 9 AGENTS.md —
        передавать `lessonId`/поля через `mutate(arg)`, не через состояние формы, которое
        замыкают колбэки
  - [ ] Материалы урока (панель на карточке урока): список (`GET /lessons/{id}/materials`,
        право `LessonMaterials.View`), добавление (`POST /lessons/{id}/materials`, право
        `LessonMaterials.Manage`) — форма переключает «файл» (через presigned-загрузку
        [[Files]], передаётся `fileId`) или «ссылка» (`url`), **ровно одно из двух** —
        валидировать на клиенте до отправки, сервер вернёт 400 при нарушении; переключатель
        `VisibleToStudents`; удаление (`DELETE /materials/{id}`); перетаскивание
        (`PUT /lessons/{id}/materials/reorder`)

### Этап 3 · StudyGroups

> [!note] ✅ Backend готов — можно начинать
> Модуль [[StudyGroups]] полностью реализован (см. [[Задачи · Новые модули]]): все эндпоинты
> из `HTTP API` в справочнике работают, права `StudyGroupsPermissions` зарегистрированы
> (`StudyGroups`: `View`/`ViewOwn`/`Create`/`Update`/`Delete`/`Archive`; `Enrollments`:
> `View`/`Create`/`Delete`/`Transfer`). Роутинг плоский, без сегмента `/study-groups` сверх имени
> ресурса — как у People/Curriculum. Расписание/посещаемость/оплаты из пункта ниже принадлежат
> Scheduling/Payments (Этапы 4–5) и появятся на `/study-groups/:id` только после тех модулей;
> здесь — только то, что закрывает сам StudyGroups.

- [ ] `src/api/study-groups.ts` — обёртка над `apiFetch`: типы `StudyGroupDto`/
      `StudyGroupDetailDto`/`GroupEnrollmentDto`/`GroupTeacherDto`/`PagedResponse<T>` вручную по
      контрактам (см. [[StudyGroups]] → «Контракты»); enum'ы `GroupFormat`/`StudyGroupStatus`/
      `EnrollmentStatus`/`TeacherRole` — string union, сериализуются как строки; ключи TanStack
      Query на `study-groups`/`enrollments`
- [ ] `/study-groups` — список (`GET /study-groups`, право `StudyGroups.View`), фильтры по курсу
      (`courseId`), преподавателю (`teacherId`), статусу (`StudyGroupStatus`) и формату
      (`GroupFormat`), поиск (`search`), пагинация и сортировка (`sortBy`/`sortDir`); кнопка
      создания гейтится `StudyGroups.Create`
- [ ] `/study-groups/:id` — **конструктор группы**: карточка (редактирование `name`/
      `primaryTeacherId`/`format`/`capacity`/`startDate`/`endDate`/`meetingUrl`/`roomId`/`notes`
      через `PUT /study-groups/{id}`, право `StudyGroups.Update`; `code` неизменяем после
      создания — не редактируется в форме) + состав ниже. `GET /study-groups/{id}` возвращает
      `StudyGroupDetailDto` с готовыми `enrollments[]`/`teachers[]` — отдельных запросов на
      состав не нужно. Кнопки жизненного цикла (право `StudyGroups.Archive` на все три):
      «Активировать» (`POST .../activate`; сервер вернёт 409, если нет ни одного зачисления —
      показать причину), «Завершить» (`POST .../finish`), «Отменить» (`POST .../cancel`, с
      полем причины). После `Finished`/`Cancelled` вся карточка и состав — read-only (сервер
      всё равно вернёт 409 на любую попытку изменения, но не отправлять запрос вхолостую).
      Удаление (`DELETE /study-groups/{id}`, право `StudyGroups.Delete`)
- [ ] Ростер преподавателей на `/study-groups/:id` — список `teachers[]` (роль
      `Primary`/`Assistant`/`Substitute`, `PrimaryTeacherId` самой группы показывать отдельной
      меткой — они не обязаны совпадать, см. [[StudyGroups]] → примечание о `PrimaryTeacherId`),
      добавление/удаление (`POST`/`DELETE .../teachers`, право `StudyGroups.Update`)
- [ ] Диалог **зачисления** — выбор одного или нескольких учеников (`POST
      /study-groups/{id}/enrollments`, тело — список `studentIds` + опционально
      `tariffId`/`discountPercent`, право `Enrollments.Create`); сервер вернёт 409 при
      превышении `Capacity` — показывать как «мест нет», не глотать
- [ ] Диалог **отчисления** — причина + дата (`DELETE
      /study-groups/{id}/enrollments/{enrollmentId}`, право `Enrollments.Delete`) — не удаляет
      строку из UI-списка сразу, а переводит в статус `Left` (список состава показывает ушедших,
      если фильтр не скрывает их явно)
- [ ] Диалог **перевода** — целевая группа + дата (`POST
      /enrollments/{enrollmentId}/transfer`, право `Enrollments.Transfer`); пауза/возобновление
      — отдельные быстрые действия в строке состава (`POST /enrollments/{id}/pause`|`/resume`,
      право `Enrollments.Create` — сервер гейтит оба под тем же правом, что и создание)
- [ ] `/study-groups/my` (право `StudyGroups.ViewOwn`) — «мои группы» для кабинета
      преподавателя/ученика (Этап 6), список без создания/редактирования
- [ ] `/students/:id` (в People, Этап 1) — вкладка «Группы» через `GET
      /students/{studentId}/enrollments` (право `Enrollments.View`) — все группы ученика,
      включая завершённые, не только активные

### Этап 4 · Scheduling

> [!note] ✅ Backend готов — можно начинать
> Модуль [[Scheduling]] полностью реализован (см. [[Задачи · Новые модули]], шаги 0–14):
> все эндпоинты из `HTTP API` в справочнике работают, права `SchedulingPermissions`
> зарегистрированы (`Sessions`: `View`/`ViewOwn`/`Create`/`Update`/`Cancel`/`Reschedule`/
> `Generate`; `Attendance`: `View`/`ViewOwn`/`Mark`/`MarkAny` — `MarkAny` зарегистрировано, но
> нигде не проверяется, см. ниже; `Rooms`: `View`/`Manage`; `ScheduleTemplates`: `View`/
> `Manage`, тем же правом гейтятся и нерабочие дни). Роутинг плоский, как у People/Curriculum/
> StudyGroups, с двумя исключениями, продиктованными самим справочником: список/создание
> шаблонов вложены под `/study-groups/:id/schedule-templates`, история посещаемости ученика —
> под `/students/:id/attendance`. Библиотека календаря — открытый вопрос
> ([[Открытые вопросы]] → «Библиотека календаря расписания»), решить перед стартом этого
> этапа, от неё зависит структура `/schedule`.

- [ ] `src/api/scheduling.ts` — обёртка над `apiFetch`: типы `SessionDto`/`SessionDetailDto`/
      `CalendarEntryDto`/`ScheduleTemplateDto`/`RoomDto`/`NonWorkingDayDto`/`AttendanceDto`/
      `AttendanceReportDto`/`GenerationPreviewDto`/`GenerationResultDto`/`SessionConflictDto`
      вручную по контрактам (см. [[Scheduling]] → «Контракты»/«DTO»); enum'ы `SessionStatus`
      (`Planned`/`Held`/`Cancelled`/`Rescheduled`), `AttendanceStatus` (`Present`/`Absent`/
      `Late`/`Excused`), `SessionConflictType`, `GenerationSkipReason` — string union; ключи
      TanStack Query на `sessions`/`schedule-templates`/`rooms`/`non-working-days`/`attendance`
- [ ] `/schedule` — **календарь** неделя/месяц через `GET /sessions/calendar` (фильтры
      `studyGroupId`/`teacherId`/`roomId`), drag-n-drop переноса → `POST /sessions/{id}/reschedule`
      (право `Sessions.Reschedule`; сервер вернёт `409` при конфликте с описанием — показать как
      диалог подтверждения с `force: true`, не глотать), цвета по группам/статусу занятия,
      часовой пояс школы для отображения (не для расчёта — сервер уже отдаёт `StartUtc`/`EndUtc`
      в UTC, конвертация в локальное время школы — на клиенте)
- [ ] `/sessions/:id` — карточка занятия. `GET /sessions/{id}` → `SessionDetailDto` с
      `ResolvedTopic` (уже посчитан на бэкенде — пусто в `Session.Topic` → подставлена
      `Lesson.Title`) и вложенным `Attendance[]`. Материалы урока **не** приходят в этом
      ответе (ADR-006) — если `LessonId` не пусто, отдельным запросом к Curriculum
      (`GET /lessons/{lessonId}/materials`, право `LessonMaterials.View`) подтянуть материалы.
      Кнопки жизненного цикла: «Провести» (`POST .../hold`, право `Sessions.Update` — отдельного
      права `Hold` нет; создаёт посещаемость на сервере, экран должен перезапросить `Attendance`
      после успеха), «Отменить» (`POST .../cancel`, право `Sessions.Cancel`, с полем причины),
      «Перенести» (`POST .../reschedule`, право `Sessions.Reschedule`, тот же диалог, что и в
      календаре). После `Held`/`Cancelled`/`Rescheduled` — не отправлять `PUT` на неизменяемое
      занятие вхолостую (сервер всё равно вернёт `409`, но UI должен блокировать кнопки заранее)
- [ ] `/study-groups/:id/schedule` — управление шаблонами группы. `GET .../schedule-templates`
      (право `ScheduleTemplates.View`) — список; создание/правка/удаление
      (`ScheduleTemplates.Manage`) — день недели, локальное время начала, длительность,
      аудитория/преподаватель (оба опциональны — пусто у преподавателя означает «берётся
      `PrimaryTeacherId` группы», это решает бэкенд, но в форме стоит показать подсказку).
      **Предпросмотр перед применением** — `POST /schedule-templates/{id}/preview` (право
      `Sessions.Generate`, `?horizonWeeks=` опционально, по умолчанию 8 недель) возвращает
      `GenerationPreviewDto` с `ToCreate[]` и `Skipped[]` (`Reason`: `NonWorkingDay` или
      `Conflict` — для `Conflict` показать `SessionConflictDto[]` с типом ресурса и на что
      наткнулись). Кнопка «Применить» → `POST /schedule-templates/{id}/generate`
      (право `Sessions.Generate`, тот же `horizonWeeks`) — массовая операция, отдельное право
      от `Sessions.Create` не просто так, гейтить кнопку отдельно
- [ ] `/attendance` — **таблица посещаемости**: сетка ученики × занятия. `GET
      /sessions/{id}/attendance` (право `Attendance.View`) для одного занятия; массовая отметка —
      `PUT /sessions/{id}/attendance` (право `Attendance.Mark`) с телом — массив
      `{studentId, status, comment}`, один запрос на всю сетку занятия, не по ученику. Дефолт
      новой строки на сервере — `Present`, поэтому типичный сценарий — отмечать только
      исключения (`Absent`/`Late`/`Excused`), не весь список. **Нет UI-различия для `MarkAny`** —
      право зарегистрировано, но сервер его не проверяет (Payments ещё нет), можно не закладывать
      отдельную ветку интерфейса сейчас
- [ ] `/students/:id/attendance` (в People, Этап 1) — история посещаемости ученика через `GET
      /students/{studentId}/attendance?from=&to=` (право `Attendance.View`), не под
      `/attendance` — отдельный сегмент, как в справочнике
- [ ] `/study-groups/:id` (в StudyGroups, Этап 3) — вкладка «Посещаемость» через `GET
      /study-groups/{id}/attendance-report?from=&to=` (право `Attendance.View`) —
      `AttendanceReportDto` со сводкой по каждому ученику (`Present`/`Absent`/`Late`/
      `Excused`/`Total`)
- [ ] Справочники: `/settings/rooms` (CRUD аудиторий, `Rooms.Manage`/`.View`, поле `IsVirtual` —
      исключает аудиторию из проверки конфликта, показать явной пометкой «онлайн») и
      `/settings/non-working-days` (нерабочие дни школы, гейтится `ScheduleTemplates.Manage`/
      `.View` — не своё право)
- [ ] `/sessions/my` (право `Sessions.ViewOwn`) — «моё расписание» для кабинета
      преподавателя/ученика/представителя (Этап 6), `GET /sessions/my?from=&to=`
- [ ] Подписка на SignalR — `AppHub`, событие `SessionScheduleChanged` в группе `tenant:{id}`
      (подключение к хабу и общая инфраструктура уже есть, см. `frontend/dashboard.md`),
      payload `SessionDto` — обновлять карточку/ячейку календаря по `Id` без перезагрузки
      страницы. Приходит на создание/правку/отмену/перенос/проведение отдельного занятия;
      **не приходит** при массовой генерации из шаблона — после применения предпросмотра
      экран должен сам перезапросить список занятий (`invalidateQueries`), SignalR не оповестит
      о каждом из сотни созданных занятий

### Этап 5 · Payments

- [ ] `/payments/invoices` — список, фильтры, **мастер массового выставления**:
      группа → период → предпросмотр сумм → выставление
- [ ] `/payments/invoices/:id` — строки, оплаты, PDF, подтверждение
- [ ] `/payments/tariffs` — CRUD
- [ ] `/payments/debtors` — отчёт

### Этап 6 · Кабинеты

- [ ] `/my/schedule` — преподаватель, ученик, представитель
- [ ] `/my/invoices` — ученик и представитель
- [ ] Стартовые страницы по ролям (разные маршруты, не разные приложения)
- [ ] Переключатель подопечных для представителя

### Этап 7 · Настройки и уборка

- [ ] `/settings/school` — часовой пояс, валюта, нумерация счетов, нерабочие дни
- [ ] `/` — обзор со школьными показателями, разный по роли
- [ ] `/system/trash` — убрать вкладки Catalog, добавить новые сущности
- [ ] `/audits` — читаемые названия сущностей
- [ ] `/identity/groups` — переименовать в «Группы доступа»
- [ ] `/tickets` — категории, привязка к ученику и счёту
- [ ] `/chat` — каналы учебных групп
- [ ] Развести в меню «Подписка» ([[Billing]]) и «Счета учеников» ([[Payments]])

---

## dashboard · навигация

- [ ] Перестроить `src/components/layout/nav-data.ts` под структуру меню
      из [[Dashboard (школа)]]
- [ ] Проверить, что гейт каждого пункта (`perm` / `anyPerm`) зеркалит право
      основного списочного эндпоинта страницы — иначе пользователь упрётся в 403
- [ ] Обновить `src/lib/trash-permissions.ts` под новые модули
- [ ] Команды новых разделов в командной палитре

---

## Тяжёлые компоненты

Требуют отдельной проработки, а не типовой формы:

| Компонент | Замечание |
|---|---|
| Календарь расписания | взять готовую библиотеку, не писать свою — [[Открытые вопросы]] |
| Конструктор курса | дерево с перетаскиванием и автосохранением |
| Таблица посещаемости | сетка с массовыми действиями |
| Мастер выставления счетов | многошаговый, с предпросмотром сумм |
| Предпросмотр генерации расписания | список будущих занятий + конфликты |

Остальное — типовые списки и формы по существующим образцам:
`pages/identity/users.tsx` (список), `pages/tenants/create.tsx` (форма).

---

## admin

- [ ] «Tenant» → «Школа» во всём UI: списки, формы, диалоги, тексты ошибок
- [ ] Карточка школы: активных учеников, преподавателей, групп, занятий за месяц,
      объём файлов — после доработки метрик [[Billing]]
- [ ] Планы: лимиты в терминах учеников и преподавателей
- [ ] Дашборд платформы: школы по планам, рост, приближающиеся к лимитам,
      истекающие подписки
- [ ] Каталог событий вебхуков — новые типы
- [ ] Демо-аккаунты (`pages/login.demo-accounts.ts`,
      `components/auth/demo-accounts-dialog.tsx`) под роли школы
- [ ] Брендирование `components/brand-mark.tsx`

> [!warning] Чего в admin делать нельзя
> Экрана «все ученики всех школ» для поддержки. Это ломает изоляцию
> ([[Мультитенантность]]). Правильный путь — имперсонация: журналируется,
> ограничена правами, отзываема.

---

## Тесты

Playwright с моками маршрутов. Обязательное покрытие — всё, что двигает деньги
или расписание:

- [ ] отметка посещаемости
- [ ] выставление счёта
- [ ] подтверждение оплаты
- [ ] зачисление и перевод между группами
- [ ] перенос и отмена занятия
- [ ] удалить спеки каталога ([[Задачи · Удаление Catalog]])

## Связанное

[[Карта экранов]] · [[Dashboard (школа)]] · [[Admin (операторская)]] · [[Бэклог]]
