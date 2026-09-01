import {
  Activity,
  BookOpen,
  CalendarDays,
  CalendarRange,
  ClipboardCheck,
  CreditCard,
  FolderOpen,
  FolderTree,
  GraduationCap,
  HeartHandshake,
  HeartPulse,
  LayoutDashboard,
  MessageCircle,
  Receipt,
  ScrollText,
  Settings,
  ShieldCheck,
  Ticket,
  Trash2,
  TrendingUp,
  TriangleAlert,
  Users,
  UsersRound,
  Wallet,
  Wifi,
} from "lucide-react";
import { ALL_TRASH_PERMISSIONS } from "@/lib/trash-permissions";

export type NavSpec = {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  /**
   * Permission required to see this item. Items without a `perm` are visible to
   * every authenticated tenant user; gated items are hidden when the current
   * user (or impersonated user) lacks the permission, so they never land on a
   * page the API will reject with 403.
   */
  perm?: string;
  /**
   * Visible only if the user holds *at least one* of these permissions. Use for
   * an item that fronts several independently-gated sub-views (e.g. Trash, whose
   * tabs each require a different permission) — the entry should show as long as
   * the user can reach any one of them. Combined with `perm` via AND.
   */
  anyPerm?: readonly string[];
};

export type NavSection = {
  id: string;
  caption: string;
  /** Section-level icon used as a fallback when the sidebar is
   *  collapsed and the section is rendered as a stack of item icons. */
  icon: React.ComponentType<{ className?: string }>;
  items: NavSpec[];
};

// Top-level items live OUTSIDE any section. Overview opens the app;
// Settings is account-scoped and lives at the very bottom.
export const topNavTop: NavSpec[] = [
  { to: "/", label: "Обзор", icon: LayoutDashboard },
  // Each gate mirrors the permission the page's primary list endpoint enforces
  // server-side (Chat → channels list, Files → /files/mine). Same convention
  // as trash-permissions.ts: if the endpoint's permission changes, mirror it.
  { to: "/chat", label: "Чат", icon: MessageCircle, perm: "Permissions.Chat.Channels.View" },
  { to: "/files", label: "Файлы", icon: FolderOpen, perm: "Permissions.Files.Upload" },
];

export const topNavBottom: NavSpec[] = [
  { to: "/settings", label: "Настройки", icon: Settings },
];

// Section accordion. Single-select — only one section open at a time.
// Layout mirrors docs/03 Frontend/Dashboard (школа).md → «Навигация».
export const sections: NavSection[] = [
  {
    // Личный кабинет — «свои» экраны для преподавателя / ученика / представителя.
    // Каждый пункт гейтится правом *.ViewOwn: у менеджера их нет (у него полный
    // *.View), поэтому весь раздел скрыт для сотрудников школы.
    id: "cabinet",
    caption: "Кабинет",
    icon: LayoutDashboard,
    items: [
      {
        to: "/my/schedule",
        label: "Моё расписание",
        icon: CalendarRange,
        perm: "Permissions.Scheduling.Sessions.ViewOwn",
      },
      {
        to: "/my/groups",
        label: "Мои группы",
        icon: UsersRound,
        perm: "Permissions.StudyGroups.StudyGroups.ViewOwn",
      },
      {
        to: "/my/invoices",
        label: "Мои счета",
        icon: Receipt,
        perm: "Permissions.Payments.StudentInvoices.ViewOwn",
      },
    ],
  },
  {
    id: "people",
    caption: "Люди",
    icon: GraduationCap,
    items: [
      // Each gate mirrors the permission the page's primary list endpoint enforces
      // server-side (GET /students, /teachers, /guardians → *.View).
      { to: "/students", label: "Ученики", icon: GraduationCap, perm: "Permissions.People.Students.View" },
      { to: "/teachers", label: "Преподаватели", icon: Users, perm: "Permissions.People.Teachers.View" },
      { to: "/guardians", label: "Представители", icon: HeartHandshake, perm: "Permissions.People.Guardians.View" },
    ],
  },
  {
    id: "learning",
    caption: "Учебный процесс",
    icon: BookOpen,
    items: [
      // Each gate mirrors the primary list endpoint's permission:
      //   GET /subjects/tree            → Subjects.View
      //   GET /courses                  → Courses.View
      //   GET /study-groups             → StudyGroups.View
      //   GET /sessions/calendar        → Sessions.View
      //   GET /sessions/{id}/attendance → Attendance.View
      { to: "/subjects", label: "Направления", icon: FolderTree, perm: "Permissions.Curriculum.Subjects.View" },
      { to: "/courses", label: "Курсы", icon: BookOpen, perm: "Permissions.Curriculum.Courses.View" },
      {
        to: "/study-groups",
        label: "Группы",
        icon: UsersRound,
        perm: "Permissions.StudyGroups.StudyGroups.View",
      },
      {
        to: "/schedule",
        label: "Расписание",
        icon: CalendarDays,
        perm: "Permissions.Scheduling.Sessions.View",
      },
      {
        to: "/attendance",
        label: "Посещаемость",
        icon: ClipboardCheck,
        perm: "Permissions.Scheduling.Attendance.View",
      },
    ],
  },
  {
    id: "payments",
    caption: "Оплаты",
    icon: Wallet,
    items: [
      // Each gate mirrors the permission the page's primary list endpoint
      // enforces server-side (GET /tariffs → Tariffs.View, GET /student-invoices
      // → StudentInvoices.View, GET /reports/* → StudentInvoices.Export).
      // These are invoices for STUDENTS — distinct from /invoices (Billing
      // subscription invoices) under «Подписка».
      {
        to: "/payments/tariffs",
        label: "Тарифы",
        icon: Wallet,
        perm: "Permissions.Payments.Tariffs.View",
      },
      {
        to: "/payments/invoices",
        label: "Счета учеников",
        icon: Receipt,
        perm: "Permissions.Payments.StudentInvoices.View",
      },
      {
        to: "/payments/debtors",
        label: "Должники",
        icon: TriangleAlert,
        perm: "Permissions.Payments.StudentInvoices.Export",
      },
      {
        to: "/payments/revenue",
        label: "Выручка",
        icon: TrendingUp,
        perm: "Permissions.Payments.StudentInvoices.Export",
      },
    ],
  },
  {
    id: "helpdesk",
    caption: "Хелпдеск",
    icon: Ticket,
    items: [
      // GET /tickets → Tickets.View.
      { to: "/tickets", label: "Обращения", icon: Ticket, perm: "Permissions.Tickets.View" },
    ],
  },
  {
    id: "subscription",
    caption: "Подписка",
    icon: CreditCard,
    items: [
      // Billing — the school's own subscription to Edvantix. Kept separate from
      // «Оплаты» (student money). GET /subscriptions/me + GET /invoices →
      // Billing.View.
      { to: "/subscription", label: "Подписка", icon: CreditCard, perm: "Permissions.Billing.View" },
      { to: "/invoices", label: "Счета", icon: Receipt, perm: "Permissions.Billing.View" },
    ],
  },
  {
    id: "identity",
    caption: "Идентификация",
    icon: Users,
    items: [
      // Gate the identity-management pages on a manage permission (not View): View Users/Roles/Groups
      // are IsBasic so every member holds them (the chat/user picker relies on Users.View), but only
      // managers should see these admin pages. Basic lacks the *.Update perms, so the items hide for them.
      { to: "/identity/users", label: "Пользователи", icon: Users, perm: "Permissions.Users.Update" },
      { to: "/identity/roles", label: "Роли", icon: ShieldCheck, perm: "Permissions.Roles.Update" },
      // Route + endpoints stay `/identity/groups`; only the UI wording changed.
      { to: "/identity/groups", label: "Группы доступа", icon: UsersRound, perm: "Permissions.Groups.Update" },
    ],
  },
  {
    id: "system",
    caption: "Система",
    icon: HeartPulse,
    items: [
      // Live activity is SSE-backed; the stream is auth-only (no permission), so no gate.
      { to: "/activity", label: "Активность", icon: Activity },
      // Health hits the anonymous /health/ready probe — visible to everyone.
      { to: "/system/health", label: "Здоровье", icon: HeartPulse },
      { to: "/system/audits", label: "Аудит", icon: ScrollText, perm: "Permissions.AuditTrails.View" },
      { to: "/system/sessions", label: "Сессии", icon: Wifi, perm: "Permissions.Sessions.ViewAll" },
      // Trash fronts several tabs, each gated on a different resource's restore /
      // view-trash permission. Show the entry if the user can reach any tab; the
      // page hides the individual tabs they can't (see trash-permissions.ts).
      { to: "/system/trash", label: "Корзина", icon: Trash2, anyPerm: ALL_TRASH_PERMISSIONS },
    ],
  },
];

/** True when the user satisfies the item's gates: the single `perm` (if any)
 *  AND at least one of `anyPerm` (if any). Ungated items are always visible. */
function isNavItemVisible(item: NavSpec, permissions: readonly string[]): boolean {
  if (item.perm && !permissions.includes(item.perm)) return false;
  if (item.anyPerm && !item.anyPerm.some((p) => permissions.includes(p))) return false;
  return true;
}

/** Drop items the user can't access, then drop any section left empty. */
export function visibleSections(permissions: readonly string[]): NavSection[] {
  return sections
    .map((s) => ({ ...s, items: s.items.filter((i) => isNavItemVisible(i, permissions)) }))
    .filter((s) => s.items.length > 0);
}

/** Filter a flat nav list (top/bottom) by permission. */
export function visibleItems(items: NavSpec[], permissions: readonly string[]): NavSpec[] {
  return items.filter((i) => isNavItemVisible(i, permissions));
}

/** Find the section whose items contain the given path (best prefix match). */
export function findSectionForPath(pathname: string): string | null {
  let bestId: string | null = null;
  let bestLen = 0;
  for (const s of sections) {
    for (const item of s.items) {
      if (
        (item.to === "/" && pathname === "/") ||
        (item.to !== "/" && pathname.startsWith(item.to))
      ) {
        if (item.to.length > bestLen) {
          bestLen = item.to.length;
          bestId = s.id;
        }
      }
    }
  }
  return bestId;
}
