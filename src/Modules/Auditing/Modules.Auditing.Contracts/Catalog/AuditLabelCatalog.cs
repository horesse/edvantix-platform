namespace FSH.Modules.Auditing.Contracts.Catalog;

/// <summary>
/// Human-readable Russian labels for the CLR type names and property names that appear in
/// <c>EntityChangeEventPayload</c>. The audit pipeline records <c>entityType.ClrType.Name</c> and
/// raw property names; the UI showed those verbatim ("StudentInvoice", "PayerGuardianId"). This
/// catalog is the single place a friendly label is resolved, served by
/// <c>GET /api/v1/audits/entity-labels</c> so the front-end has one source of truth instead of a
/// hard-coded map.
///
/// A discovery surface, not an allow-list: an unlabelled type/field simply falls back to its raw
/// name in the UI. Keys are matched case-insensitively.
/// </summary>
public static class AuditLabelCatalog
{
    /// <summary>Simple CLR type name → label. Covers the school-domain aggregates a user opens a history for.</summary>
    public static IReadOnlyDictionary<string, string> Entities { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // People
            ["Student"] = "Ученик",
            ["Teacher"] = "Преподаватель",
            ["Guardian"] = "Представитель",
            ["StudentGuardian"] = "Связь ученик — представитель",
            ["StudentNote"] = "Заметка об ученике",

            // Curriculum
            ["Subject"] = "Направление",
            ["Course"] = "Курс",
            ["CourseModule"] = "Раздел курса",
            ["Lesson"] = "Урок программы",
            ["LessonMaterial"] = "Материал урока",

            // StudyGroups
            ["StudyGroup"] = "Группа",
            ["GroupEnrollment"] = "Зачисление в группу",
            ["GroupTeacher"] = "Преподаватель группы",

            // Scheduling
            ["ScheduleTemplate"] = "Шаблон расписания",
            ["Session"] = "Занятие",
            ["Attendance"] = "Посещаемость",
            ["Room"] = "Кабинет",
            ["NonWorkingDay"] = "Нерабочий день",

            // Payments
            ["Tariff"] = "Тариф",
            ["StudentInvoice"] = "Счёт ученику",
            ["InvoiceLine"] = "Строка счёта",
            ["PaymentConfirmation"] = "Подтверждение оплаты",

            // Support / platform
            ["Ticket"] = "Обращение",
            ["TicketComment"] = "Комментарий к обращению",
            ["WebhookSubscription"] = "Подписка на вебхуки",
            ["ChatChannel"] = "Канал чата",
            ["FshUser"] = "Пользователь",
            ["FshRole"] = "Роль",
        };

    /// <summary>
    /// Common property names → label. Deliberately small — only fields shared across many entities
    /// or otherwise opaque. Entity-specific fields fall back to their raw name.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Fields { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Status"] = "Статус",
            ["Name"] = "Название",
            ["Title"] = "Заголовок",
            ["FirstName"] = "Имя",
            ["LastName"] = "Фамилия",
            ["MiddleName"] = "Отчество",
            ["Email"] = "E-mail",
            ["Phone"] = "Телефон",
            ["Notes"] = "Примечания",
            ["IsActive"] = "Активен",
            ["CreatedAtUtc"] = "Создано",
            ["UpdatedAtUtc"] = "Изменено",
            ["StartsAtUtc"] = "Начало",
            ["EndsAtUtc"] = "Окончание",
            ["Amount"] = "Сумма",
            ["Currency"] = "Валюта",
            ["DueDate"] = "Срок оплаты",
            ["Number"] = "Номер",
            ["TeacherId"] = "Преподаватель",
            ["StudentId"] = "Ученик",
            ["CourseId"] = "Курс",
            ["StudyGroupId"] = "Группа",
            ["PayerGuardianId"] = "Плательщик",
        };

    /// <summary>Label for a CLR type name, or the raw name when it is not catalogued.</summary>
    public static string EntityLabel(string clrTypeName) =>
        Entities.TryGetValue(clrTypeName, out var label) ? label : clrTypeName;

    /// <summary>Label for a property name, or the raw name when it is not catalogued.</summary>
    public static string FieldLabel(string propertyName) =>
        Fields.TryGetValue(propertyName, out var label) ? label : propertyName;
}
