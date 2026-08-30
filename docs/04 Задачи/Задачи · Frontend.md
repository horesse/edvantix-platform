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

- [x] `src/api/people.ts` — students/teachers/guardians + scope, все эндпоинты People
- [x] `src/api/curriculum.ts`
- [x] `src/api/study-groups.ts`
- [x] `src/api/scheduling.ts`
- [x] `src/api/payments.ts`

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

- [x] `src/api/people.ts` — `clients/dashboard/src/api/people.ts`: типы `StudentDto`/
      `StudentDetailDto`/`TeacherDto`/`GuardianDto`/`StudentGuardianDto`/`StudentNoteDto`/
      `PeopleScope`/`PagedResponse<T>` + все команды/запросы, ключи TanStack Query
      `students`/`teachers`/`guardians` (+ `student`/`teacher`/`guardian` для карточек)
- [x] `/students` — список, фильтр по статусу, текстовый поиск, пагинация, создание.
      `GET /api/v1/students`, право `Students.View`. (фильтр по менеджеру — позже, нужен
      пикер пользователей)
- [x] `/students/:id` — профиль + редактирование (`PUT`), архивация/восстановление,
      привязка/отвязка учётки, вкладка «Представители» (`GET/POST/DELETE .../guardians`,
      `POST .../guardians/{gid}/primary-payer`), вкладка «Заметки» под `Students.ViewNotes`.
      Группы/посещаемость/счета/история — заглушки до [[StudyGroups]]/[[Scheduling]]/[[Payments]]
- [x] `/students/import` — загрузка CSV → `POST /students/import?dryRun=true` (таблица
      построчных результатов) → повторный вызов с `?dryRun=false` для записи
- [x] `/teachers` — список (`GET /teachers`, право `Teachers.View`), создание
- [x] `/teachers/:id` — профиль, специализации, ставка, био, деактивация/активация,
      привязка/отвязка учётки. Блок «нагрузка» — заглушка (эндпоинта нет)
- [x] `/guardians` — список, создание, привязка/отвязка учётки, редактирование.
      «Подопечные» на карточке — заглушка: в `People.Contracts` нет запроса
      «ученики представителя», связи задаются со стороны ученика
- [ ] `/guardians` — блок «подопечные»: нужен `GET /guardians/{id}/students` в People
      (сейчас связь только через `GET /students/{id}/guardians`)

### Этап 2 · Curriculum

> [!note] ✅ Backend готов — можно начинать
> Модуль [[Curriculum]] полностью реализован (см. [[Задачи · Новые модули]]): все эндпоинты
> из `HTTP API` в справочнике работают, права `CurriculumPermissions` зарегистрированы
> (`Subjects`/`Courses`/`Lessons`/`LessonMaterials`). Роутинг плоский, без сегмента
> `/curriculum` — как у People. Разделы курса (`CourseModule`) не имеют отдельного ресурса
> прав, их CRUD гейтится `Courses.Update` — учитывать в проверках `perm`/`anyPerm` на UI.

- [x] `src/api/curriculum.ts` — обёртка над `apiFetch`: типы `SubjectDto`/`SubjectNodeDto`/
      `CourseDto`/`CourseDetailDto`/`CourseModuleDto`/`LessonDto`/`LessonMaterialDto`/
      `PagedResponse<T>` вручную по контрактам (см. [[Curriculum]] → «Контракты»); enum'ы
      `CourseLevel`/`CourseStatus`/`MaterialKind` — string union, сериализуются как строки;
      ключи TanStack Query на `subjects`/`courses`/`lessons`
- [x] `/subjects` — дерево направлений (`GET /subjects/tree` → `SubjectNodeDto[]`, право
      `Subjects.View`), инлайн создание/переименование/удаление узла (`POST`/`PUT`/`DELETE
      /subjects/{id}`, право `Subjects.Create`/`Update`/`Delete`), перетаскивание для
      `PUT /subjects/order` (`ReorderSubjectsCommand` — принимает `parentId` и упорядоченный
      список id **только для одного уровня**, т.е. drag-n-drop работает в пределах родителя)
- [x] `/courses` — список (`GET /courses`, право `Courses.View`), фильтры по направлению
      (`subjectId`), статусу (`Draft`/`Published`/`Archived`) и уровню (`CourseLevel`),
      пагинация и сортировка (`sortBy=title|createdAtUtc|durationHours`); отдельная вкладка/
      маршрут `/courses/trash` (`GET /courses/trash`, право `Courses.ViewTrash`) с кнопкой
      восстановления (`POST /courses/{id}/restore`, право `Courses.Restore`)
- [x] `/courses/:id` — **конструктор курса**: карточка курса (редактирование `title`/
      `description`/`level`/`durationHours`/`subjectId`/`coverFileId` через `PUT /courses/{id}`,
      право `Courses.Update`) + дерево разделов и уроков ниже. `GET /courses/{id}` возвращает
      `CourseDetailDto` с готовым деревом `modules[].lessons[]` — отдельного запроса на дерево
      не нужно. Кнопки жизненного цикла: «Опубликовать» (`POST .../publish`, право
      `Courses.Publish`; сервер вернёт 409, если у курса нет ни одного раздела — показать
      причину, не глотать ошибку), «Архивировать» (`POST .../archive`, тоже `Courses.Publish`),
      «Дублировать» (`POST .../duplicate` → редирект на новый `id`, право `Courses.Create`),
      «Удалить» (`DELETE /courses/{id}` → в корзину, право `Courses.Delete`)
  - [x] Дерево разделов: создание раздела (`POST /courses/{id}/modules`, право
        `Courses.Update`), инлайн-правка названия/описания (`PUT /modules/{id}`), удаление
        (`DELETE /modules/{id}` — предупредить, что каскадно удалит уроки и материалы раздела),
        перетаскивание (`PUT /courses/{id}/modules/reorder`)
  - [x] Уроки внутри раздела: создание (`POST /modules/{id}/lessons`, право
        `Lessons.Create`), инлайн-правка title/objectives/content/durationMinutes
        (`PUT /lessons/{id}`, `Lessons.Update`), удаление (`DELETE /lessons/{id}`,
        `Lessons.Delete` — каскадно удаляет материалы урока), перетаскивание
        (`PUT /modules/{id}/lessons/reorder`); автосохранение по правилу 9 AGENTS.md —
        передавать `lessonId`/поля через `mutate(arg)`, не через состояние формы, которое
        замыкают колбэки
  - [x] Материалы урока (панель на карточке урока): список (`GET /lessons/{id}/materials`,
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

- [x] `src/api/study-groups.ts` — обёртка над `apiFetch`: типы `StudyGroupDto`/
      `StudyGroupDetailDto`/`GroupEnrollmentDto`/`GroupTeacherDto`/`PagedResponse<T>` вручную по
      контрактам (см. [[StudyGroups]] → «Контракты»); enum'ы `GroupFormat`/`StudyGroupStatus`/
      `EnrollmentStatus`/`TeacherRole` — string union, сериализуются как строки; ключи TanStack
      Query на `study-groups`/`enrollments`
- [x] `/study-groups` — список (`GET /study-groups`, право `StudyGroups.View`), фильтры по курсу
      (`courseId`), преподавателю (`teacherId`), статусу (`StudyGroupStatus`) и формату
      (`GroupFormat`), поиск (`search`), пагинация и сортировка (`sortBy`/`sortDir`); кнопка
      создания гейтится `StudyGroups.Create`
- [x] `/study-groups/:id` — **конструктор группы**: карточка (редактирование `name`/
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
- [x] Ростер преподавателей на `/study-groups/:id` — список `teachers[]` (роль
      `Primary`/`Assistant`/`Substitute`, `PrimaryTeacherId` самой группы показывать отдельной
      меткой — они не обязаны совпадать, см. [[StudyGroups]] → примечание о `PrimaryTeacherId`),
      добавление/удаление (`POST`/`DELETE .../teachers`, право `StudyGroups.Update`)
- [x] Диалог **зачисления** — выбор одного или нескольких учеников (`POST
      /study-groups/{id}/enrollments`, тело — список `studentIds` + опционально
      `tariffId`/`discountPercent`, право `Enrollments.Create`); сервер вернёт 409 при
      превышении `Capacity` — показывать как «мест нет», не глотать
- [x] Диалог **отчисления** — причина + дата (`DELETE
      /study-groups/{id}/enrollments/{enrollmentId}`, право `Enrollments.Delete`) — не удаляет
      строку из UI-списка сразу, а переводит в статус `Left` (список состава показывает ушедших,
      если фильтр не скрывает их явно)
- [x] Диалог **перевода** — целевая группа + дата (`POST
      /enrollments/{enrollmentId}/transfer`, право `Enrollments.Transfer`); пауза/возобновление
      — отдельные быстрые действия в строке состава (`POST /enrollments/{id}/pause`|`/resume`,
      право `Enrollments.Create` — сервер гейтит оба под тем же правом, что и создание)
- [x] `/study-groups/my` (право `StudyGroups.ViewOwn`) — «мои группы» для кабинета
      преподавателя/ученика (Этап 6), список без создания/редактирования
- [x] `/students/:id` (в People, Этап 1) — вкладка «Группы» через `GET
      /students/{studentId}/enrollments` (право `Enrollments.View`) — все группы ученика,
      включая завершённые, не только активные

> [!note] Замечания по реализации Этапа 3
> - `roomId` в формах создания/правки группы не выводится — справочник аудиторий
>   принадлежит Scheduling (Этап 4), пикера ещё нет; поле уедет в форму вместе с тем этапом.
> - Диалог отчисления шлёт только `reason` (query-параметр эндпоинта `DELETE
>   .../enrollments/{id}`); `LeftOn`/дату эндпоинт не биндит, поэтому поля даты в диалоге нет —
>   дату проставляет сервер.
> - `tariffId` в диалоге зачисления не выбирается (тарифы — Payments, Этап 5); доступна общая
>   скидка `discountPercent` на весь набор.
> - Пауза/возобновление зачисления — быстрые действия в строке состава под `Enrollments.Create`.
> - Проверено на Aspire: батч-зачисление при превышении `Capacity` может частично примениться
>   (первые ученики зачисляются, следующий даёт 409) — вопреки формулировке контракта об
>   атомарности. Диалог зачисления на 409 не только показывает «мест нет», но и инвалидирует
>   карточку группы, чтобы ростер отразил частичное зачисление.

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

- [x] `src/api/scheduling.ts` — обёртка над `apiFetch`: типы `SessionDto`/`SessionDetailDto`/
      `CalendarEntryDto`/`ScheduleTemplateDto`/`RoomDto`/`NonWorkingDayDto`/`AttendanceDto`/
      `AttendanceReportDto`/`GenerationPreviewDto`/`GenerationResultDto`/`SessionConflictDto`
      вручную по контрактам (см. [[Scheduling]] → «Контракты»/«DTO»); enum'ы `SessionStatus`
      (`Planned`/`Held`/`Cancelled`/`Rescheduled`), `AttendanceStatus` (`Present`/`Absent`/
      `Late`/`Excused`), `SessionConflictType`, `GenerationSkipReason` — string union; ключи
      TanStack Query на `sessions`/`schedule-templates`/`rooms`/`non-working-days`/`attendance`
- [x] `/schedule` — **календарь** неделя/месяц через `GET /sessions/calendar` (фильтры
      `studyGroupId`/`teacherId`/`roomId`), drag-n-drop переноса → `POST /sessions/{id}/reschedule`
      (право `Sessions.Reschedule`; сервер вернёт `409` при конфликте с описанием — показать как
      диалог подтверждения с `force: true`, не глотать), цвета по группам/статусу занятия,
      часовой пояс школы для отображения (не для расчёта — сервер уже отдаёт `StartUtc`/`EndUtc`
      в UTC, конвертация в локальное время школы — на клиенте)
- [x] `/sessions/:id` — карточка занятия. `GET /sessions/{id}` → `SessionDetailDto` с
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
- [x] `/study-groups/:id/schedule` — управление шаблонами группы. `GET .../schedule-templates`
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
- [x] `/attendance` — **таблица посещаемости**: сетка ученики × занятия. `GET
      /sessions/{id}/attendance` (право `Attendance.View`) для одного занятия; массовая отметка —
      `PUT /sessions/{id}/attendance` (право `Attendance.Mark`) с телом — массив
      `{studentId, status, comment}`, один запрос на всю сетку занятия, не по ученику. Дефолт
      новой строки на сервере — `Present`, поэтому типичный сценарий — отмечать только
      исключения (`Absent`/`Late`/`Excused`), не весь список. **Нет UI-различия для `MarkAny`** —
      право зарегистрировано, но сервер его не проверяет (Payments ещё нет), можно не закладывать
      отдельную ветку интерфейса сейчас
- [x] `/students/:id/attendance` (в People, Этап 1) — история посещаемости ученика через `GET
      /students/{studentId}/attendance?from=&to=` (право `Attendance.View`), не под
      `/attendance` — отдельный сегмент, как в справочнике
- [x] `/study-groups/:id` (в StudyGroups, Этап 3) — вкладка «Посещаемость» через `GET
      /study-groups/{id}/attendance-report?from=&to=` (право `Attendance.View`) —
      `AttendanceReportDto` со сводкой по каждому ученику (`Present`/`Absent`/`Late`/
      `Excused`/`Total`)
- [x] Справочники: `/settings/rooms` (CRUD аудиторий, `Rooms.Manage`/`.View`, поле `IsVirtual` —
      исключает аудиторию из проверки конфликта, показать явной пометкой «онлайн») и
      `/settings/non-working-days` (нерабочие дни школы, гейтится `ScheduleTemplates.Manage`/
      `.View` — не своё право)
- [x] `/sessions/my` (право `Sessions.ViewOwn`) — «моё расписание» для кабинета
      преподавателя/ученика/представителя (Этап 6), `GET /sessions/my?from=&to=`
- [x] Подписка на SignalR — `AppHub`, событие `SessionScheduleChanged` в группе `tenant:{id}`
      (подключение к хабу и общая инфраструктура уже есть, см. `frontend/dashboard.md`),
      payload `SessionDto` — обновлять карточку/ячейку календаря по `Id` без перезагрузки
      страницы. Приходит на создание/правку/отмену/перенос/проведение отдельного занятия;
      **не приходит** при массовой генерации из шаблона — после применения предпросмотра
      экран должен сам перезапросить список занятий (`invalidateQueries`), SignalR не оповестит
      о каждом из сотни созданных занятий

### Этап 5 · Payments

> [!note] ✅ Backend готов — можно начинать
> Модуль [[Payments]] полностью реализован (см. [[Задачи · Новые модули]]): все эндпоинты из
> `HTTP API` в справочнике работают, права `PaymentsPermissions` зарегистрированы (`Tariffs`:
> `View`/`Manage`; `StudentInvoices`: `View`/`ViewOwn`/`Create`/`Issue`/`Cancel`/`Export` — нет
> отдельного права на правку черновика, `PUT /student-invoices/{id}` гейтится тем же `Create`;
> `StudentPayments`: `View`/`Confirm`/`Revoke` — `Confirm` самое чувствительное право в
> системе, `Revoke` держать за `SchoolAdmin`). Роутинг плоский, без сегмента `/payments` сверх
> имени ресурса — как у People/Curriculum/StudyGroups. Два известных отклонения от справочника
> не видны на фронтенде (свой `IInvoicePdfRenderer`, дополнительное событие для напоминаний) —
> ничего в контрактах API они не меняют. Открытый пробел бэкенда: **остаток пакета для
> `PerPackage`** нигде не считается — не закладывать в UI баланса «осталось N занятий», такого
> поля в `StudentBalanceDto` нет.

- [x] `src/api/payments.ts` — обёртка над `apiFetch`: типы `TariffDto`/`StudentInvoiceDto`/
      `StudentInvoiceDetailDto`/`InvoiceLineDto`/`InvoiceLineInput`/`PaymentConfirmationDto`/
      `StudentBalanceDto`/`DebtorDto`/`RevenueReportDto`/`PagedResponse<T>` вручную по
      контрактам (см. [[Payments]] → «Контракты»); enum'ы `TariffKind` (`PerLesson`/`PerMonth`/
      `PerPackage`/`OneTime`), `InvoiceStatus` (`Draft`/`Issued`/`PartiallyPaid`/`Paid`/
      `Cancelled`), `PaymentMethod` (`Cash`/`BankTransfer`/`Card`/`Online`/`Other`) — string
      union; ключи TanStack Query на `tariffs`/`student-invoices`/`payments`
- [x] `/payments/tariffs` — список (`GET /tariffs`, право `Tariffs.View`, фильтр `isActive`),
      создание/правка (`POST`/`PUT /tariffs/{id}`, право `Tariffs.Manage`; `kind` и `currency`
      неизменяемы после создания — не показывать их в форме правки, только в создании),
      деактивация (`POST /tariffs/{id}/deactivate`, тот же `Tariffs.Manage`); поля
      `lessonsCount`/`validDays` актуальны только для `PerPackage` — скрывать/дизейблить для
      остальных `kind` в форме
- [x] `/payments/invoices` — список (`GET /student-invoices`, право `StudentInvoices.View`),
      фильтры (`studentId`/`studyGroupId`/`status`/`periodFrom`/`periodTo`/`hasDebt`/`search`
      по номеру), пагинация и сортировка (`sortBy`/`sortDir`); колонка `isOverdue` — уже
      посчитана на бэкенде (`StudentInvoiceDto.IsOverdue`), не пересчитывать на клиенте
  - [x] **Мастер массового выставления** (тяжёлый компонент) — группа → период
        (`periodFrom`/`periodTo`) → срок оплаты (`dueDate`) → `POST
        /student-invoices/bulk-generate` (право `StudentInvoices.Create`, идемпотентен —
        повторный запуск за тот же период возвращает существующие черновики, не дублирует;
        `issueImmediately: false` по умолчанию — отдельная явная кнопка «выставить сразу»,
        не чекбокс в мастере) → список созданных/уже существующих id, ссылка на каждый
  - [x] Массовое выставление отмеченных черновиков — `POST /student-invoices/bulk-issue`
        (право `StudentInvoices.Issue`), тело — массив id; best-effort — сервер молча
        пропускает не-`Draft` записи в выборке, не возвращает ошибку на всю пачку
- [x] `/payments/invoices/:id` — карточка счёта. `GET /student-invoices/{id}` →
      `StudentInvoiceDetailDto` со строками (`lines[]`) и оплатами (`payments[]`) сразу —
      отдельных запросов не нужно. Правка строк (`PUT /student-invoices/{id}`, право
      `StudentInvoices.Create`, только пока `status = Draft` — сервер вернёт 409 иначе, UI
      должен блокировать форму заранее) — построчный редактор (описание/тариф/количество/цена),
      сохранение отправляет **весь** набор строк разом (`ReplaceLines` на сервере — не
      построчный PATCH). Кнопки жизненного цикла: «Выставить» (`POST .../issue`, право
      `StudentInvoices.Issue`, требует хотя бы одну строку — сервер вернёт 409 на пустой
      черновик), «Отменить» (`POST .../cancel`, право `StudentInvoices.Cancel`, только при
      `paidAmount = 0` — иначе сначала сторнировать все оплаты, показать это в подсказке),
      «Скачать PDF» (`GET .../pdf`, право `StudentInvoices.View`, `application/pdf`)
  - [x] Блок оплат на карточке — список `payments[]`, подтверждение новой (`POST
        .../payments`, право `StudentPayments.Confirm` — гейтить кнопку отдельно от остальной
        карточки, самое чувствительное право модуля) с полями сумма/дата/способ
        (`PaymentMethod`)/референс/чек (`proofFileId` через presigned-загрузку [[Files]])/
        заметка; переплата разрешена явно — не блокировать сумму больше остатка долга, сервер
        примет. Сторнирование (`POST /payments/{paymentId}/reverse`, право
        `StudentPayments.Revoke` — держать за отдельной ролью в UI, не показывать рядом с
        обычным подтверждением) — с обязательной заметкой-причиной; сторно-строка появится в
        том же списке `payments[]` с отрицательной суммой и `reversesId`, не отдельной сущностью
- [x] `/payments/debtors` — отчёт (`GET /reports/debtors`, право `StudentInvoices.Export`,
      опциональный фильтр `studyGroupId`) — таблица `studentId`/`debt`/`overdueInvoiceCount`/
      `oldestDueDate`, ссылка на карточку ученика (People, Этап 1)
- [x] `/payments/revenue` — отчёт (`GET /reports/revenue`, право `StudentInvoices.Export`,
      обязательные `periodFrom`/`periodTo`) — сумма поступлений с разбивкой по
      `PaymentMethod` (`byMethod[]`); учитывает сторно автоматически (сумма со знаком), не
      нужно вычитать вручную
- [x] `/students/:id` (в People, Этап 1) — вкладка «Счета/Баланс»: `GET
      /students/{studentId}/balance` (право `StudentInvoices.View`) — `charged`/`paid`/`debt`/
      `advance` плюс список `overdueInvoices[]` со ссылками на карточки счетов; отдельно
      список всех счетов ученика через `GET /student-invoices?studentId=` для полной истории
      (баланс отдаёт только просроченные, не все)
- [x] `/student-invoices/my` (право `StudentInvoices.ViewOwn`) — «мои счета» для кабинета
      ученика/представителя (Этап 6), `GET /student-invoices/my` (опциональный `status`) —
      свои счета для ученика, счета всех подопечных для представителя, сервер сам резолвит
      через `PeopleScope`, отдельного переключателя подопечного на этом запросе не нужно

### Этап 6 · Кабинеты

- [ ] `/my/schedule` — преподаватель, ученик, представитель
- [ ] `/my/invoices` — ученик и представитель
- [ ] Стартовые страницы по ролям (разные маршруты, не разные приложения)
- [ ] Переключатель подопечных для представителя
- [ ] `/accept-invite` — форма установки пароля по ссылке из письма-приглашения
      (`email`, `token`, `tenant` в query). Отправляет `POST /reset-password`, тот же
      запрос, что и `/reset-password` — backend уже реализован, см. [[Identity]] →
      «Приглашение по e-mail» и [[Задачи · Доработки каркаса]]
- [ ] Убрать/не строить экран самостоятельной регистрации — приглашение стало
      единственным путём получить доступ представителю/ученику на практике;
      backend `/self-register` не удаляется, просто на него больше не ссылается UI

### Этап 7 · Настройки и уборка

- [ ] `/settings/school` — часовой пояс, валюта, нумерация счетов, нерабочие дни
- [ ] `/` — обзор со школьными показателями, разный по роли
- [ ] `/system/trash` — убрать вкладки Catalog, добавить новые сущности
- [ ] `/audits` — читаемые названия сущностей
- [ ] `/identity/groups` — переименовать в «Группы доступа»
- [ ] `/tickets` — категории, привязка к ученику и счёту
- [ ] `/chat` — каналы учебных групп
- [x] Развести в меню «Подписка» ([[Billing]]) и «Счета учеников» ([[Payments]]) —
      добавлен отдельный раздел «Оплаты» в `nav-data.ts` (Этап 5)

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

- [x] отметка посещаемости
- [x] выставление счёта (`tests/payments/invoice-issue.spec.ts` — 409 на пустом черновике,
      блокировка редактора для не-`Draft`, `ReplaceLines` одним PUT)
- [x] подтверждение оплаты (`tests/payments/payment-confirm.spec.ts` — гейт
      `StudentPayments.Confirm`, переплата не блокируется; `payment-reverse.spec.ts` —
      сторно с обязательной причиной, отрицательная строка + `reversesId`)
- [ ] зачисление и перевод между группами
- [x] перенос и отмена занятия
- [ ] удалить спеки каталога ([[Задачи · Удаление Catalog]])

## Связанное

[[Карта экранов]] · [[Dashboard (школа)]] · [[Admin (операторская)]] · [[Бэклог]]
