# Module: Curriculum

Учебная программа: направления → курсы → разделы → уроки → материалы — всё шаблоны вне времени
и людей. Даты и люди — в Scheduling/StudyGroups (см. `ADR-006`). Module `Order = 600` — слот,
освободившийся после удаления Catalog (`ADR-002`). Справочник: `docs/02 Модули/Curriculum.md`.

**Entities / DbContext:** `Subject` (самоссылающееся дерево), `Course` (единственный
`ISoftDeletable`), `CourseModule`, `Lesson`, `LessonMaterial`. `CurriculumDbContext`, схема
`curriculum`.

**Areas:** Subjects (CRUD, дерево, перестановка), Courses (CRUD, поиск, публикация/архивация/
дублирование/корзина), CourseModules и Lessons (CRUD + перестановка на каждом уровне),
LessonMaterials (добавление/удаление/перестановка, файл или ссылка).

## Gotchas / patterns to copy

- **Плоская персистентность, не вложенный агрегат в три уровня.** `Course`, `CourseModule`,
  `Lesson`, `LessonMaterial` — четыре независимых `AggregateRoot` со своим `DbSet` и FK на
  родителя, а не `Course`, владеющий всем деревом через EF owned-коллекции (в отличие от
  `Product.Images` в Catalog — там всего один уровень). `GetCourseByIdQuery` собирает
  `CourseDetailDto` явным join'ом трёх `DbSet` в хендлере. Причина: конструктор курса
  автосохраняет один урок/материал за запрос — перечитывать и сохранять весь агрегат на каждую
  мелкую правку не тот путь.
- **Мягкое удаление — только у `Course`.** `Subject`/`CourseModule`/`Lesson`/`LessonMaterial` не
  реализуют `ISoftDeletable` — их `Delete`/`Remove` обычное жёсткое удаление (в контрактах нет
  команд восстановления для них). Не путать с Catalog, где `Category`/`Product` оба
  soft-deletable.
- **Публикация требует хотя бы один раздел.** `PublishCourseCommandHandler` проверяет
  `CourseModules.Any(CourseId=...)` перед `course.Publish()` и бросает `CustomException` 409 —
  инвариант "курс без разделов недопустим" из справочника. Нет авто-создания скрытого раздела
  «Основной» — уроки всегда создаются в явно созданный раздел.
- **`DuplicateCourseCommand`** — глубокое копирование: новый `Course` в `Draft`
  (`PublishedAtUtc = null`), заголовок с суффиксом «(копия {8 hex-символов guid})» для
  уникальности слага, затем клонируются все `CourseModule` → `Lesson` → `LessonMaterial` с
  новыми `Id`, сохраняя `SortOrder`.
- **`LessonMaterial`** — CHECK-ограничение в БД (`CK_LessonMaterials_FileXorUrl`) поверх
  проверки в домене и валидаторе: ровно одно из `FileId`/`Url`. Плюс правило «вид → источник»
  (домен + `AddLessonMaterialCommandValidator`): `Video`/`Link` → только `Url`,
  `File`/`Presentation` → только `FileId`, `Homework` — любое. `Video` дополнительно требует
  хост из `CurriculumOptions.VideoMaterialAllowedHosts` (`Curriculum` секция конфига;
  YouTube/Vimeo/RuTube/VK/Дзен по умолчанию, сравнение по хосту и поддоменам) — прямой
  загрузки видео нет ни здесь, ни через [[Files]] (нет `.mp4/.mov/…` в категории
  `LessonMaterial`). `LessonMaterialAccessPolicy`
  (`IFileAccessPolicy`, `OwnerType = "LessonMaterial"`, `OwnerId = LessonId`) — по образцу
  Catalog's `ProductFileAccessPolicy`; видимость ученику решает `VisibleToStudents` на самом
  материале, не файловая политика.
- **Не звать `AddEventingCore()` в `CurriculumModule`** — `IdentityModule` уже регистрирует его;
  модуль вызывает только `AddEventingForDbContext<CurriculumDbContext>()`, как People.
  `CurriculumDbContext` объявляет `DbSet<OutboxMessage>`/`DbSet<InboxMessage>` **с самого
  начала** (не второй миграцией, как пришлось чинить People — см. `Задачи · Новые модули.md`).
- **`ICourseQueryService`** — без кэша (в отличие от `IPeopleScopeResolver`): StudyGroups/
  Scheduling вызывают его на команду, не на каждый элемент горячего списка. Пересмотреть, если
  профилирование после появления StudyGroups скажет иначе.
- **Модули (разделы) не имеют отдельного ресурса прав.** Их CRUD и перестановка гейтятся
  `Courses.Update` — в таблице прав `docs/01 Архитектура/Модель прав доступа.md` нет отдельного
  `CourseModules`.
- **Роутинг плоский**, без сегмента `/curriculum` — как у People, не как у Catalog
  (`/api/v1/subjects`, `/api/v1/courses`, `/api/v1/modules`, `/api/v1/lessons`,
  `/api/v1/materials`).
