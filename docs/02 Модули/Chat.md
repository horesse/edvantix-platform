---
tags: [модуль, каркас, chat]
статус: реализован
порядок: 800
схема: chat
---

# Chat

← [[Edvantix]] · [[Карта модулей]] · задачи: [[Задачи · Доработки каркаса]]

> ✅ Реализован · порядок `800` · схема `chat`

## Назначение

Каналы и сообщения: именованные каналы, личные диалоги, групповые диалоги.
Закрывает требование «чат»; для Edvantix нужна интеграция с учебными группами.

## Домен

| Сущность | Назначение |
|---|---|
| `ChatChannel` | канал: `Channel` / `DirectMessage` / `GroupMessage` |
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

`ListMyChannelsQuery` · `DiscoverChannelsQuery` · `GetChannelByIdQuery` ·
`ListChannelMessagesQuery` · `ListMessageRepliesQuery` · `GetPinnedMessagesQuery` ·
`SearchMessagesQuery`

### DTO

`ChannelDto` · `ChannelType` · `ChannelMemberDto` · `ChannelMemberRole` ·
`MessageDto` · `MessageAttachmentDto` · `MessageReactionDto`

### Публикуемые события

`MentionedInChannelIntegrationEvent` → [[Notifications]]

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
GET    /api/v1/chat/channels/my
GET    /api/v1/chat/channels/discover
POST   /api/v1/chat/channels
GET    /api/v1/chat/channels/{id}
PUT    /api/v1/chat/channels/{id}
POST   /api/v1/chat/channels/{id}/archive
POST   /api/v1/chat/channels/{id}/restore
POST   /api/v1/chat/dm                          найти или создать диалог
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

Каждая учебная группа получает приватный канал: `StudyGroup.ChatChannelId`
заполняется по событию `StudyGroupCreated`, состав синхронизируется по
`StudentEnrolled` / `StudentUnenrolled`.

> [!warning] Ученик без учётной записи в канал не попадает
> `ChannelMember.UserId` требует `FshUser`, а у ученика `UserId` может быть `null`
> ([[People]]). Обработчик события должен подставлять представителя-плательщика,
> а не падать.

## Зависимости

**Ссылается на:** `Identity.Contracts`, `Multitenancy.Contracts`, `Files.Contracts`.

**Подписан на события:** [[StudyGroups]].
**Подписаны на его события:** [[Notifications]].

## Связанное

[[StudyGroups]] · [[People]] · `.agents/rules/modules/chat.md` · `.agents/rules/realtime.md`
