---
tags: [задачи, frontend]
---

# Задачи · Frontend

← [[Бэклог]] · справочники: [[Dashboard (школа)]] · [[Admin (операторская)]] · [[Карта экранов]]

Полный инвентарь экранов обоих приложений с состоянием — [[Карта экранов]].

> [!success] Этапы 1–7 сделаны
> **dashboard** — People, Curriculum, StudyGroups, Scheduling, Payments, Кабинеты
> (`/my/*`, переключатель подопечных, `/accept-invite`), настройки школы, перестройка
> навигации, уборка Catalog. **admin** — «## admin»: блок метрик школы, лимиты планов
> в терминах учеников/преподавателей/групп, дашборд платформы, каталог событий вебхуков,
> полная RU-локализация + ребренд Edvantix. Слито в `main`: PR #14, #16, #18, #19, #21,
> #22, #23, #24. Playwright зелёный (233 dashboard + 113 admin).

> [!danger] Правило 9 из `AGENTS.md` — живая конвенция, не задача
> Данные для конкретного вызова передавать через `mutate(arg)`, **никогда** через
> состояние, которое замыкают колбэки мутации — гонка на момент выполнения.
> Проверять на ревью каждой формы.

## Открытые пункты

- [x] `/guardians` — блок «подопечные»: добавлен `GET /guardians/{id}/students` в People
      (обработчик + DTO `GuardianStudentDto`, право `Permissions.People.Students.View`),
      карточка представителя показывает список подопечных с ролью, статусом ученика
      и меткой плательщика, ссылки ведут на `/students/:id`.
- [ ] `/students/:id` — вкладка «История» (аудит по ученику). Бэкенд готов
      (`GET /api/v1/audits/by-entity/{entityName}/{entityId}`, PR #15), но привязка к
      карточке ученика во frontend-этапах не значилась — подтвердить, нужна ли она в MVP
      или это осознанно вне скоупа.
- [x] **Привязка тарифа к зачислению в группу.** В `EnrollDialog` на
      `/study-groups/:id` добавлен селектор «Тариф» (список из `GET /api/v1/tariffs`,
      только активные, лейбл `имя · сумма · вид`, подсказка про фолбэк на тариф
      курса при массовой генерации счетов); `tariffId` проходит через `mutate(arg)`
      (правило 9). В ростере «Состав группы» у строки зачисления показывается имя
      привязанного тарифа. Запрос тарифов и оба места гейтятся на
      `Permissions.Payments.Tariffs.View`, чтобы роль без Payments не ловила 403.
      Файлы: `clients/dashboard/src/pages/study-groups/study-group-detail.tsx`,
      тесты `clients/dashboard/tests/study-groups/enrollments.spec.ts`.
      **Смена тарифа у существующего зачисления не сделана** — в
      `Modules.StudyGroups` нет write-эндпоинта, меняющего `GroupEnrollment.TariffId`
      без переоформления (есть только Enroll / Transfer / Pause / Resume / Unenroll).
      Заведено бэкенд-пунктом в [[Задачи · Новые модули]] (StudyGroups).
- [ ] **Вынести настройки школы из личных настроек отдельным пунктом.** Сейчас
      «Школа» (часовой пояс, валюта, нумерация счетов) — это вкладка внутри
      `/settings`, вперемешку с личными вкладками Profile / Security / Appearance /
      Notifications / API keys (`clients/dashboard/src/pages/settings/settings-layout.tsx`,
      `ALL_TABS`). Настройки школы — тенант-скоуп (право
      `Permissions.SchoolSettings.Manage`), а не «настройки профиля», и логически не
      должны жить в одном списке с личными.
      Нужно: отдельный пункт/раздел верхнего уровня в сайдбаре
      (`clients/dashboard/src/components/layout/nav-data.ts`) — напр. «Школа» или
      «Настройки школы» рядом с «Системой»/«Идентификацией», гейт на
      `Permissions.SchoolSettings.Manage`. Под него же завести уже существующие,
      но не представленные в навигации экраны: **аудитории**
      (`pages/scheduling/rooms-settings.tsx`, роут `/settings/rooms`,
      `Permissions.Scheduling.Rooms.Manage`) и **нерабочие дни**
      (`pages/scheduling/non-working-days-settings.tsx`, роут
      `/settings/non-working-days`) — сейчас доступны только ссылками со страницы
      `/settings/school`. Личный `/settings` оставить только с личными вкладками
      (заодно доперевести их подписи на RU — «Profile»/«Security»/… ещё англ.).
      Роуты школьных экранов можно как оставить под `/settings/*`, так и перенести
      под общий префикс (напр. `/school/*`) — решить при реализации.

## Связанное

[[Карта экранов]] · [[Dashboard (школа)]] · [[Admin (операторская)]] · [[Бэклог]]
