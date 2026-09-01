/**
 * Permission strings + catalog mirrored from the server's *Permissions.cs
 * registries. Kept here so:
 *   1. Route guards stay typo-proof (`IdentityPermissions.Users.View`).
 *   2. The Role editor can render every assignable permission grouped
 *      by category without an extra round-trip.
 *
 * If the server registry adds permissions, mirror them here — there is no
 * runtime fetch (no /permissions catalog endpoint exists).
 * Convention follows the server: `Permissions.{Resource}.{Action}`.
 */

export const IdentityPermissions = Object.freeze({
  Users: {
    View: "Permissions.Users.View",
    Search: "Permissions.Users.Search",
    Create: "Permissions.Users.Create",
    Update: "Permissions.Users.Update",
    Delete: "Permissions.Users.Delete",
    Export: "Permissions.Users.Export",
    ManageRoles: "Permissions.Users.ManageRoles",
    Impersonate: "Permissions.Users.Impersonate",
  },
  UserRoles: {
    View: "Permissions.UserRoles.View",
    Update: "Permissions.UserRoles.Update",
  },
  Roles: {
    View: "Permissions.Roles.View",
    Create: "Permissions.Roles.Create",
    Update: "Permissions.Roles.Update",
    Delete: "Permissions.Roles.Delete",
  },
  RoleClaims: {
    View: "Permissions.RoleClaims.View",
    Update: "Permissions.RoleClaims.Update",
  },
  Sessions: {
    View: "Permissions.Sessions.View",
    Revoke: "Permissions.Sessions.Revoke",
    ViewAll: "Permissions.Sessions.ViewAll",
    RevokeAll: "Permissions.Sessions.RevokeAll",
  },
  Impersonation: {
    View: "Permissions.Impersonation.View",
    Revoke: "Permissions.Impersonation.Revoke",
  },
} as const);

export const MultitenancyPermissions = Object.freeze({
  Tenants: {
    View: "Permissions.Tenants.View",
    Create: "Permissions.Tenants.Create",
    Update: "Permissions.Tenants.Update",
    UpgradeSubscription: "Permissions.Tenants.UpgradeSubscription",
  },
} as const);

export const BillingPermissions = Object.freeze({
  View: "Permissions.Billing.View",
  Manage: "Permissions.Billing.Manage",
} as const);

export const AuditingPermissions = Object.freeze({
  AuditTrails: {
    View: "Permissions.AuditTrails.View",
    ViewCrossTenant: "Permissions.AuditTrails.ViewCrossTenant",
  },
} as const);

export const WebhooksPermissions = Object.freeze({
  Subscriptions: {
    View: "Permissions.Webhooks.View",
    Create: "Permissions.Webhooks.Create",
    Delete: "Permissions.Webhooks.Delete",
    Test: "Permissions.Webhooks.Test",
  },
} as const);

// ─── Catalog (drives the Role editor) ───────────────────────────────────

export type PermissionEntry = {
  name: string;
  description: string;
  /** Only assignable on root-tenant (cross-tenant) roles. */
  root?: boolean;
  /** Granted by default to authenticated users via the basic role. */
  basic?: boolean;
};

export type PermissionGroup = {
  /** UI-facing category name. */
  category: string;
  /** Section blurb shown under the heading. */
  blurb: string;
  entries: PermissionEntry[];
};

export const PERMISSION_CATALOG: readonly PermissionGroup[] = [
  {
    category: "Школы",
    blurb: "Создание и управление школами. Только для оператора корневой школы.",
    entries: [
      { name: MultitenancyPermissions.Tenants.View, description: "Просмотр школ", root: true },
      { name: MultitenancyPermissions.Tenants.Create, description: "Создание школ", root: true },
      { name: MultitenancyPermissions.Tenants.Update, description: "Изменение школ", root: true },
      { name: MultitenancyPermissions.Tenants.UpgradeSubscription, description: "Изменение подписки школы", root: true },
    ],
  },
  {
    category: "Пользователи",
    blurb: "Управление учётками пользователей школы и их ролями.",
    entries: [
      { name: IdentityPermissions.Users.View, description: "Просмотр пользователей", basic: true },
      { name: IdentityPermissions.Users.Search, description: "Поиск пользователей" },
      { name: IdentityPermissions.Users.Create, description: "Создание пользователей" },
      { name: IdentityPermissions.Users.Update, description: "Изменение пользователей" },
      { name: IdentityPermissions.Users.Delete, description: "Удаление пользователей" },
      { name: IdentityPermissions.Users.Export, description: "Экспорт пользователей" },
      { name: IdentityPermissions.Users.ManageRoles, description: "Назначение ролей пользователям" },
      { name: IdentityPermissions.Users.Impersonate, description: "Имперсонация другого пользователя" },
    ],
  },
  {
    category: "Роли",
    blurb: "Управление ролями и их набором прав.",
    entries: [
      { name: IdentityPermissions.Roles.View, description: "Просмотр ролей", basic: true },
      { name: IdentityPermissions.Roles.Create, description: "Создание ролей" },
      { name: IdentityPermissions.Roles.Update, description: "Изменение ролей" },
      { name: IdentityPermissions.Roles.Delete, description: "Удаление ролей" },
      { name: IdentityPermissions.RoleClaims.View, description: "Просмотр claim'ов роли", basic: true },
      { name: IdentityPermissions.RoleClaims.Update, description: "Изменение claim'ов роли" },
      { name: IdentityPermissions.UserRoles.View, description: "Просмотр назначений ролей", basic: true },
      { name: IdentityPermissions.UserRoles.Update, description: "Изменение назначений ролей" },
    ],
  },
  {
    category: "Сессии",
    blurb: "Просмотр и отзыв активных сессий.",
    entries: [
      { name: IdentityPermissions.Sessions.View, description: "Просмотр своих сессий", basic: true },
      { name: IdentityPermissions.Sessions.Revoke, description: "Отзыв своих сессий", basic: true },
      { name: IdentityPermissions.Sessions.ViewAll, description: "Просмотр всех сессий школы" },
      { name: IdentityPermissions.Sessions.RevokeAll, description: "Отзыв любой сессии" },
    ],
  },
  {
    category: "Биллинг",
    blurb: "Просмотр и управление подписками и счетами школ.",
    entries: [
      { name: BillingPermissions.View, description: "Просмотр биллинга", basic: true },
      { name: BillingPermissions.Manage, description: "Управление биллингом — тарифы, подписки, счета" },
    ],
  },
  {
    category: "Журналы аудита",
    blurb: "Просмотр событий безопасности и изменений сущностей.",
    entries: [
      { name: AuditingPermissions.AuditTrails.View, description: "Просмотр журнала аудита", basic: true },
      {
        name: AuditingPermissions.AuditTrails.ViewCrossTenant,
        description: "Просмотр журнала аудита по всем школам",
        root: true,
      },
    ],
  },
  {
    category: "Имперсонация",
    blurb: "Просмотр и отзыв активных сессий имперсонации. Отзыв немедленно аннулирует выданный токен.",
    entries: [
      { name: IdentityPermissions.Impersonation.View, description: "Просмотр разрешений имперсонации" },
      { name: IdentityPermissions.Impersonation.Revoke, description: "Отзыв активных разрешений имперсонации" },
    ],
  },
  {
    category: "Вебхуки",
    blurb: "Управление исходящими подписками вебхуков и просмотр их доставок.",
    entries: [
      { name: WebhooksPermissions.Subscriptions.View, description: "Просмотр подписок и доставок вебхуков", basic: true },
      { name: WebhooksPermissions.Subscriptions.Create, description: "Создание подписок вебхуков" },
      { name: WebhooksPermissions.Subscriptions.Delete, description: "Удаление подписок вебхуков" },
      { name: WebhooksPermissions.Subscriptions.Test, description: "Отправка тестовых доставок вебхуков" },
    ],
  },
];

export const ALL_PERMISSION_NAMES: readonly string[] = PERMISSION_CATALOG.flatMap((g) =>
  g.entries.map((e) => e.name),
);
