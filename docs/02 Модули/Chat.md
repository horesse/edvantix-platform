---
tags: [модуль, каркас, chat]
статус: реализован
порядок: 800
схема: chat
---

# Chat

← [[Edvantix]] · [[Карта модулей]] · задачи: [[Бэклог]]

> ✅ Реализован · порядок `800` · схема `chat`

## Назначение

Каналы и сообщения: именованные каналы, личные диалоги, групповые диалоги.
Закрывает требование «чат»; для Edvantix нужна интеграция с учебными группами.

## Домен

| Сущность | Назначение |
|---|---|
| `ChatChannel` | канал: `Channel` / `DirectMessage` / `GroupMessage`. `SourceStudyGroupId` (nullable) — канал учебной группы; `IsLocked` — «только чтение» (история видна, писать нельзя), отдельно от `IsDeleted` |
| `ChannelMember` | участник с ролью и отметкой прочтения |
| `Message` | сообщение, поддерживает ответы (треды) |
| `MessageAttachment` | вложение → [[Files]] |
| `MessageMention` | упоминание пользователя |
| `MessageReaction` | реакция |

Особенности реализации:

- `DirectKey` — отсортированный ключ `"{userA}:{userB}"` с частичным UNIQUE-индексом:
  поиск существующего личного диалога за O(1) без гонки.
- `Archive()` вместо `Remove()` — EF-удаление агрегата каскадом пометило бы
  `ChannelMember` как `Deleted`, и восстановление потеряло бы участников.
  Флаг переключается явно, восстановление получается без потерь.
- `ChannelMember.UserId` требует существующего пользователя.

Доменные события: `ChannelCreated`, `ChannelMemberAdded`, `ChannelMemberRemoved`,
`MessageCreated`, `MessageEdited`, `MessageDeleted`, `MessagePinned`, `MessageUnpinned`.

## Контракты

`Modules.Chat.Contracts`

### Команды

| Область | Команды |
|---|---|
| Каналы | `CreateChannelCommand` `UpdateChannelCommand` `ArchiveChannelCommand` `RestoreChannelCommand` `FindOrCreateDmCommand` |
| Участники | `AddChannelMembersCommand` `RemoveChannelMemberCommand` `MarkChannelReadCommand` |
| Сообщения | `SendMessageCommand` `EditMessageCommand` `DeleteMessageCommand` `PinMessageCommand` |
| Реакции | `AddReactionCommand` `RemoveReactionCommand` |

### Запросы

`ListMyChannelsQuery` (фильтр `Kind`: `study-group` — только каналы учебных групп,
`standalone` — только остальные) · `DiscoverChannelsQuery` · `GetChannelByIdQuery` ·
`ListChannelMessagesQuery` · `ListMessageRepliesQuery` · `GetPinnedMessagesQuery` ·
`SearchMessagesQuery`

### DTO

`ChannelDto` (`SourceStudyGroupId` — nullable id учебной группы, если канал её обслуживает;
позволяет SPA отличать каналы групп и делать deep-link) · `ChannelType` ·
`ChannelMemberDto` · `ChannelMemberRole` · `MessageDto` · `MessageAttachmentDto` ·
`MessageReactionDto`

### Публикуемые события

`MentionedInChannelIntegrationEvent` → [[Notifications]]
`StudyGroupChannelLinkedIntegrationEvent` (`StudyGroupId`, `ChannelId`) → [[StudyGroups]]
— обратная связь после провижининга канала группы (Chat не может звать StudyGroups напрямую).
Публикуется прямо через `IEventBus`, тем же приёмом, что и `MentionedInChannelIntegrationEvent`.

### Реальное время

SignalR-хаб: новые сообщения, редактирование, реакции, индикатор набора.

## Права

`ChatPermissions`, ресурсы `Chat.Channels` и `Chat.Messages`:

| Ресурс | Действие | Basic |
|---|---|---|
| `Chat.Channels` | `View` | ✔ |
| | `Create` | ✔ |
| | `ManageAll` | |
| `Chat.Messages` | `Send` | ✔ |
| | `EditOwn` | ✔ |
| | `DeleteOwn` | ✔ |
| | `DeleteAny` | |

## HTTP API

```
GET    /api/v1/chat/channels/my            ?kind=study-group|standalone — фильтр по типу канала
GET    /api/v1/chat/channels/discover
POST   /api/v1/chat/channels
GET    /api/v1/chat/channels/{id}
PUT    /api/v1/chat/channels/{id}
POST   /api/v1/chat/channels/{id}/archive
POST   /api/v1/chat/channels/{id}/restore
POST   /api/v1/chat/dm                          найти или создать диалог (гейтится IChatDmPolicy)
GET    /api/v1/chat/dm-settings
PUT    /api/v1/chat/dm-settings
POST   /api/v1/chat/channels/{id}/members
DELETE /api/v1/chat/channels/{id}/members/{uid}
POST   /api/v1/chat/channels/{id}/read
GET    /api/v1/chat/channels/{id}/messages
POST   /api/v1/chat/channels/{id}/messages
PUT    /api/v1/chat/messages/{id}
DELETE /api/v1/chat/messages/{id}
GET    /api/v1/chat/messages/{id}/replies
POST   /api/v1/chat/messages/{id}/pin
GET    /api/v1/chat/channels/{id}/pinned
POST   /api/v1/chat/messages/{id}/reactions
GET    /api/v1/chat/search
```

## Применение в Edvantix

Каждая учебная группа получает приватный канал (`IntegrationEventHandlers/`,
`StudyGroupChannelSync`):

| Событie [[StudyGroups]] | Что делает Chat |
|---|---|
| `StudyGroupCreated` | создаёт `ChatChannel` с `SourceStudyGroupId`, сидит преподавателя (если у него есть учётка), публикует `StudyGroupChannelLinkedIntegrationEvent` — StudyGroups кладёт id в `StudyGroup.ChatChannelId`. Идемпотентно: повтор находит канал и просто перепубликует связь. |
| `StudentEnrolled` | добавляет ученика в канал |
| `StudentUnenrolled` | убирает ученика — **но не**, если его чат-аккаунт всё ещё представляет другого активного ученика группы (общий опекун-плательщик на двоих детей) |
| `StudyGroupFinished` | `channel.Lock()` — история остаётся, `SendMessage` в заблокированный канал отдаёт `409` |

Канал ищется по `SourceStudyGroupId` (частичный индекс) — прямой ссылки
StudyGroups → Chat в рантайме нет. Тот же признак вынесен в `ChannelDto` и в фильтр
`GET /channels/my?kind=study-group`, чтобы SPA показывала каналы групп отдельным разделом
([[EDX-011 Раздел каналов учебных групп в чате]]). Миграция `StudyGroupChannelBackfill`
(EDX-010) — разовый бэкофилл `SourceStudyGroupId` по `StudyGroup.ChatChannelId` для каналов,
созданных до появления признака (защищён `to_regclass`, no-op без модуля StudyGroups).

### Ограничение личных сообщений

`IChatDmPolicy` (`Features/v1/Channels/DmPolicy/`) гейтит `FindOrCreateDmCommand`
(существующие диалоги не закрываются задним числом):

| Кто | Кому | Можно |
|---|---|---|
| менеджер / школьный админ / платформенный админ | кому угодно | да (и к нему — тоже) |
| преподаватель | преподаватель | да |
| ученик / представитель | преподавателю своих групп (у представителя — групп подопечных) | да, в обе стороны |
| ученик | ученик | по настройке школы `ChatDmSettings.AllowStudentToStudentDm` (по умолчанию **нет**) |
| остальное (представитель ↔ представитель, ученик ↔ чужой преподаватель) | | нет |

Роли резолвятся через `IUserService.GetUserRolesAsync` (менеджер/админ) и
`IPeopleScopeResolver` (`PeopleScope`); «преподаватель моих групп» — пересечение
`IStudyGroupQueryService.GetActiveGroupIdsForTeacherAsync` и
`GetActiveStudyGroupIdsForStudentAsync`. Настройка — `GET`/`PUT /api/v1/chat/dm-settings`
(права `SchoolSettings.View`/`.Manage`). Модуль получил ссылку на `Multitenancy.Contracts`.

> [!note] Ученик без учётной записи
> `StudyGroupChannelSync.ResolveChatUserId`: свой `UserId` ученика → иначе `UserId`
> опекуна-плательщика → иначе любой опекун с учёткой. Если ни у кого в семье учётки
> нет — ученик просто не добавляется (обработчик не падает). E-mail-канал уведомлений
> при этом работает (People хранит `Email`), in-app — нет.

## Зависимости

**Ссылается на:** `Identity.Contracts`, `Multitenancy.Contracts` (`SchoolSettings`-права),
`Files.Contracts`, `People.Contracts` (`IPeopleScopeResolver` + резолв контактов),
`StudyGroups.Contracts` (события + `IStudyGroupQueryService`).

**Подписан на события:** [[StudyGroups]] (`StudyGroupCreated`, `StudentEnrolled/Unenrolled`,
`StudyGroupFinished`).
**Подписаны на его события:** [[Notifications]] (`MentionedInChannel`), [[StudyGroups]]
(`StudyGroupChannelLinked`).

## Связанное

[[StudyGroups]] · [[People]] · `.agents/rules/modules/chat.md` · `.agents/rules/realtime.md`
