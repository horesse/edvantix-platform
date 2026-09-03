---
key: EDX-010
aliases: [EDX-010]
tags: [задача, backend]
status: done
area: backend
priority: p3
blocked-by: []
blocks: [EDX-011]
related: []
created: 2026-09-03
closed: 2026-09-03
---

# EDX-010 · Признак «канал учебной группы» в Chat

> [!success] Готово · 2026-09-03 · PR #37

## Контекст

Chat создаёт приватный канал на `StudyGroupCreated` и синхронизирует участников на
enroll/unenroll, блокирует канал на `StudyGroupFinished` (см. `.agents/rules/modules/chat.md`).
Но у канала нет типа/признака «study-group» — во frontend невозможно отфильтровать
каналы групп от обычных. Карта экранов помечает `/chat` как 🔧 по этой причине.

## Что сделать

- [x] Привязка канала к контексту: `SourceStudyGroupId` (nullable) на `ChatChannel`,
      проставляется подписчиком `StudyGroupCreated` (`ChatChannel.CreateForStudyGroup`).
      Колонка + частичный индекс уже были (миграция `StudyGroupChannels`) — аддитивно.
- [x] Признак вынесен в `ChannelDto.SourceStudyGroupId` (маппер).
- [x] Фильтр в `ListMyChannelsQuery` — параметр `Kind` (`study-group` / `standalone`),
      query-string `GET /api/v1/chat/channels/my?kind=…`, значения из `ChannelKindFilter`,
      провалидированы (`ListMyChannelsQueryValidator`).
- [x] Миграция `20260903164116_StudyGroupChannelBackfill` — разовый бэкофилл
      `SourceStudyGroupId` по `StudyGroup.ChatChannelId` для каналов, созданных до
      появления признака. Кросс-схемный `UPDATE … FROM study_groups."StudyGroups"`,
      защищён `to_regclass` (no-op, если модуль StudyGroups не развёрнут). Data-only,
      снапшот не тронут.
- [x] Интеграционный тест `StudyGroupChannelKindFilterTests` — создать группу → канал
      помечен, `?kind=study-group` его отдаёт, `?kind=standalone` исключает; неизвестный
      `kind` → 400; изоляция тенантов (tenant B не видит канал группы tenant A ни в
      фильтре, ни по id).
- [x] Обновлены `docs/02 Модули/Chat.md` и `docs/03 Frontend/Карта экранов.md`.

## Зависимости

- Блокируется: —
- Блокирует: [[EDX-011 Раздел каналов учебных групп в чате]] (бэкенд готов, разблокирована)

## Проверка

- Интеграционный тест: создать группу → канал помечен, попадает в отфильтрованный список. ✅
  (`src/Tests/Integration.Tests/Tests/Chat/StudyGroupChannelKindFilterTests.cs`)
