---
key: EDX-026
aliases: [EDX-026]
tags: [задача, frontend]
status: blocked
area: dashboard
priority: p1
blocked-by: ["[[EDX-023 Привязка получателя к Telegram]]", "[[EDX-024 Канал доставки Telegram]]"]
blocks: []
related: ["[[EDX-021 Telegram-уведомления]]", "[[Карта экранов]]"]
created: 2026-09-08
closed:
---

# EDX-026 · Блок Telegram в настройках уведомлений

## Контекст

Пользователь с учётной записью (менеджер, преподаватель, ученик или родитель с логином)
подключает Telegram сам. Экран — `clients/dashboard/src/pages/settings/notifications.tsx`
(`/settings/notifications`), там уже таблица типов с тумблерами «Приложение» / «Почта».
API — из [[EDX-023 Привязка получателя к Telegram]] и [[EDX-024 Канал доставки Telegram]].

## Что сделать

**API-модуль** `src/api/notifications.ts`
- [ ] `getTelegramStatus()`, `issueTelegramLinkToken()`, `unlinkTelegram()`; тип
      `NotificationPreference` — поле `telegram`, ответ preferences — `telegramLinked`.

**Блок «Telegram»** над таблицей подписок
- [ ] Не подключён: текст «Получайте напоминания о занятиях и счета в Telegram», кнопка
      **«Подключить Telegram»**. Нажатие → `issueTelegramLinkToken` (через `mutate`, правило 9)
      → показать ссылку `t.me/…` кнопкой «Открыть Telegram» (`target=_blank`) и QR-код
      (зависимость `qrcode` уже в `package.json`) для случая «сижу за компьютером, телефон
      в руке». Подпись: «Ссылка действует 30 дней».
- [ ] Ожидание привязки: после выпуска ссылки — поллинг `getTelegramStatus` каждые 3 с,
      пока открыт экран (TanStack `refetchInterval`, отключается, когда `isLinked`).
      Как подключилось — плашка «Подключено: @username, {дата}» без перезагрузки.
- [ ] Подключён: «Подключено как @username · с {дата}», кнопка «Отключить» с
      подтверждением. `DeactivationReason = Blocked` → «Бот заблокирован в Telegram.
      Разблокируйте его и подключите снова» + кнопка «Подключить снова».
- [ ] Ошибки API — тостом с текстом, что делать; без «Something went wrong».

**Таблица подписок**
- [ ] Третья колонка «Telegram» с тумблером на каждом типе; пока `telegramLinked = false` —
      колонка приглушена, тумблеры `disabled`, подсказка «Сначала подключите Telegram».
      Дефолты приходят с сервера, на клиенте не дублируются.

**Кабинет**
- [ ] На `/my` (`RoleLanding` → `CabinetLandingPage`) — неблокирующая плашка «Подключите
      Telegram, чтобы не пропускать занятия» с кнопкой на `/settings/notifications`.
      Показывается, если не подключён; «Скрыть» прячет на 30 дней (`localStorage`,
      в `try/catch`). Менеджерам не показывать — им важнее лента.

**Тесты и документация**
- [ ] Playwright (route-mocked): не подключён → выпуск ссылки → QR и ссылка видны;
      статус переключился → плашка «Подключено»; отключение; колонка Telegram неактивна
      без привязки; плашка в кабинете и её скрытие.
- [ ] `docs/03 Frontend/Карта экранов.md` — строка «Настройки» и «Кабинет»,
      `.agents/rules/frontend/dashboard.md`, если появился новый паттерн (поллинг статуса).

## Зависимости

- Блокируется: [[EDX-023 Привязка получателя к Telegram]], [[EDX-024 Канал доставки Telegram]]

## Проверка

- `cd clients/dashboard && npm run lint && npm run test` зелёные.
- Aspire-смоук: подключить свой Telegram по QR со стенда в режиме polling, выключить
  «Счёт выставлен → Telegram», выставить себе счёт — в Telegram не пришло, в ленту пришло.
