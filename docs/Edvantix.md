---
tags: [moc, index]
aliases: [Главная, Home, Индекс]
created: 2026-08-09
---

# Edvantix — менеджмент онлайн-школ

> [!info] Что это за хранилище
> Внутренняя проектная документация (Obsidian vault) для системы **Edvantix**.
> Это **не** публичный docs-сайт (`github.com/fullstackhero/docs`, Astro) — тот описывает готовый
> продукт для пользователей. Здесь живут проектные решения, доменная модель, план миграции
> стартер-кита и договорённости команды.
>
> Технические конвенции кода остаются в [`AGENTS.md`](../AGENTS.md) и `.agents/rules/` —
> дублировать их здесь не нужно, только ссылаться.

## Что строим

Edvantix — SaaS-платформа управления онлайн-школами. Одна школа = один тенант
(см. [[ADR-001 Школа как тенант]]). Внутри школы: программа обучения, учебные группы,
расписание занятий, посещаемость, счета ученикам, преподаватели, менеджеры, коммуникации.

Основа — форк fullstackhero .NET Starter Kit: модульный монолит .NET 10 + два React 19
приложения. Каркас (Identity, Multitenancy, Auditing, Files, Webhooks, Chat, Tickets,
Notifications, Billing) переиспользуется почти целиком; предметная часть пишется с нуля.

---

## Навигация

### Обзор
- [[Продуктовое видение]] — зачем, для кого, границы MVP
- [[Глоссарий]] — единый язык (**читать до написания кода**)
- [[Роли и сценарии]] — кто что делает в системе

### Архитектура
- [[Обзор архитектуры]] — слои, стек, композиция
- [[Карта модулей]] — какие модули есть, какие появятся, порядок загрузки
- [[Мультитенантность]] — изоляция школ
- [[Модель прав доступа]] — гибкая ролевая система, каталог прав
- [[Интеграционные события]] — связи между модулями
- [[Регистрация модуля]] — чек-лист «четырёх мест»

### Модули — справочник

Что модуль представляет собой, его домен и контракты. Без планов работ:
они в разделе «Задачи».

**Новые (предметная область школы)**
- [[People]] — ученики, преподаватели, представители
- [[Curriculum]] — курсы, разделы, уроки, материалы
- [[StudyGroups]] — учебные группы и зачисления
- [[Scheduling]] — расписание, занятия, посещаемость
- [[Payments]] — тарифы, счета ученикам, подтверждение оплат

**Существующие (каркас)**
- [[Identity]] · [[Multitenancy]] · [[Billing]] · [[Auditing]] · [[Files]]
- [[Webhooks]] · [[Chat]] · [[Tickets]] · [[Notifications]]

**Удаляется**
- [[Catalog (удаляется)]] — демо-витрина e-commerce

### Frontend
- [[Admin (операторская)]] — управление платформой, SuperAdmin
- [[Dashboard (школа)]] — рабочее место школы
- [[Карта экранов]] — инвентарь экранов обоих приложений

### Задачи
- [[Бэклог]] — точка входа во все работы
- [[Задачи · Новые модули]] · [[Задачи · Доработки каркаса]] · [[Задачи · Frontend]]
- [[Задачи · Удаление Catalog]] — точные пути и строки
- [[Открытые вопросы]] — что ещё не решено
- [[Этапы внедрения]] — порядок и риски

### Решения (ADR)
- [[ADR-001 Школа как тенант]]
- [[ADR-002 Catalog заменяется на Curriculum]]
- [[ADR-003 People как отдельный модуль]]
- [[ADR-004 Payments отдельно от Billing]]
- [[ADR-005 Именование Group и StudyGroup]]
- [[ADR-006 Урок программы и занятие расписания]]

---

## Карта предметной области

```mermaid
flowchart TB
    subgraph Curriculum
        Course[Course<br/>курс]
        CModule[CourseModule<br/>раздел]
        Lesson[Lesson<br/>урок программы]
        Course --> CModule --> Lesson
    end

    subgraph People
        Student[Student<br/>ученик]
        Teacher[Teacher<br/>преподаватель]
        Guardian[Guardian<br/>представитель]
        Guardian -.опекает.-> Student
    end

    subgraph StudyGroups
        SG[StudyGroup<br/>учебная группа]
        Enr[GroupEnrollment<br/>зачисление]
    end

    subgraph Scheduling
        Session[Session<br/>занятие]
        Att[Attendance<br/>посещаемость]
        Session --> Att
    end

    subgraph Payments
        Inv[StudentInvoice<br/>счёт]
        Pay[PaymentConfirmation<br/>подтверждение]
        Inv --> Pay
    end

    Course --> SG
    Teacher --> SG
    Student --> Enr --> SG
    SG --> Session
    Lesson -.план занятия.-> Session
    Teacher --> Session
    Student --> Att
    Student --> Inv
    Guardian -.плательщик.-> Inv
    SG -.тарификация.-> Inv
```

## Статус

| Область | Состояние |
|---|---|
| Каркас (Identity, Multitenancy, Auditing, Files, Webhooks, Chat, Tickets, Notifications, Billing) | ✅ есть в коде |
| Catalog (демо e-commerce) | 🔴 удаляется |
| People, Curriculum, StudyGroups, Scheduling, Payments | 🟡 спроектированы, не реализованы |
| Frontend школы (учебные экраны) | 🟡 не начат |
