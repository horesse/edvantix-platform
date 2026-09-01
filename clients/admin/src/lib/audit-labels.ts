/** Russian labels for Auditing enums (cross-tenant audit trail). */

export const AUDIT_TYPE_RU: Record<string, string> = {
  Security: "Безопасность",
  Exception: "Исключение",
  EntityChange: "Изменение сущности",
  Activity: "Активность",
};

export const AUDIT_SEVERITY_RU: Record<string, string> = {
  Trace: "Трассировка",
  Debug: "Отладка",
  Information: "Информация",
  Warning: "Предупреждение",
  Error: "Ошибка",
  Critical: "Критично",
};
