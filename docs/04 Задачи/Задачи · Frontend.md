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

- [ ] `/study-groups` — список, фильтры
- [ ] `/study-groups/:id` — состав, расписание, посещаемость, чат, оплаты
- [ ] Диалоги зачисления, отчисления, перевода

### Этап 4 · Scheduling

- [ ] `/schedule` — **календарь** неделя/месяц, drag-n-drop переноса, цвета по
      группам, часовой пояс
- [ ] `/sessions/:id` — тема, материалы урока, посещаемость, перенос, отмена
- [ ] `/study-groups/:id/schedule` — шаблон, предпросмотр генерации, конфликты
- [ ] `/attendance` — **таблица посещаемости**: сетка ученики × занятия,
      отметка кликом, массовые действия
- [ ] Подписка на SignalR — обновление календаря без перезагрузки

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
