import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { Command } from "cmdk";
import {
  Activity,
  BookOpen,
  CalendarDays,
  ClipboardCheck,
  Folder,
  FolderTree,
  GraduationCap,
  HeartPulse,
  KeyRound,
  Landmark,
  LayoutDashboard,
  LifeBuoy,
  LogOut,
  MessageSquare,
  Monitor,
  Moon,
  Palette,
  Plus,
  Receipt,
  ScrollText,
  Search,
  Settings as SettingsIcon,
  Shield,
  ShieldCheck,
  Sparkles,
  Sun,
  TrendingUp,
  TriangleAlert,
  Users,
  UsersRound,
  UserRound,
  Wallet,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog";
import { useAuth } from "@/auth/use-auth";
import { useTheme } from "@/components/theme/theme-provider";
import { accents } from "@/components/theme/appearance-options";
import { ALL_TRASH_PERMISSIONS } from "@/lib/trash-permissions";
import { cn } from "@/lib/cn";

/**
 * Command palette dialog — separated from the provider so cmdk + the full
 * action graph (lucide icons, accent options, navigate logic) are
 * code-split into their own chunk. The provider in command-palette.tsx
 * lazy-imports this module on first ⌘K, keeping the main shell shipping
 * a smaller bundle for cold start.
 */

type ActionItem = {
  id: string;
  label: string;
  hint?: string;
  Icon: React.ComponentType<{ className?: string }>;
  /** Free-form keywords for fuzzy matching. */
  keywords?: string[];
  shortcut?: string;
  perform: () => void;
  /**
   * Permission gates — same semantics as NavSpec in layout/nav-data.ts: the item
   * is hidden unless the user holds `perm` AND at least one of `anyPerm`. Each
   * value mirrors what the destination page's API (or the create action's
   * endpoint) enforces server-side, so the palette never offers a guaranteed 403.
   */
  perm?: string;
  anyPerm?: readonly string[];
};

type ActionGroup = {
  heading: string;
  items: ActionItem[];
};

export function CommandPaletteDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (next: boolean) => void;
}) {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const { setMode, setAccent } = useTheme();
  const permissions = useMemo(() => user?.permissions ?? [], [user]);

  // Build the action set fresh each time the palette opens. The ones
  // that navigate close the palette; the ones that mutate appearance
  // don't, so the user can preview multiple choices.
  const groups = useMemo<ActionGroup[]>(() => {
    const close = () => onOpenChange(false);
    const go = (path: string) => () => {
      navigate(path);
      close();
    };
    // Mirrors isNavItemVisible in layout/nav-data.ts.
    const visible = (item: ActionItem) => {
      if (item.perm && !permissions.includes(item.perm)) return false;
      if (item.anyPerm && !item.anyPerm.some((p) => permissions.includes(p))) return false;
      return true;
    };
    const allGroups: ActionGroup[] = [
      {
        heading: "Navigate",
        items: [
          {
            id: "nav-overview",
            label: "Обзор",
            hint: "Показатели школы",
            Icon: LayoutDashboard,
            keywords: ["overview", "home", "dashboard", "обзор", "главная"],
            perform: go("/"),
          },
          {
            id: "nav-activity",
            label: "Активность",
            hint: "Живая лента событий",
            Icon: Activity,
            keywords: ["activity", "events", "sse", "log", "лента"],
            perform: go("/activity"),
          },
          {
            id: "nav-chat",
            label: "Чат",
            hint: "Каналы и личные сообщения",
            Icon: MessageSquare,
            keywords: ["chat", "messages", "dm", "channel", "чат", "сообщения"],
            perform: go("/chat"),
            perm: "Permissions.Chat.Channels.View",
          },
          {
            id: "nav-files",
            label: "Файлы",
            hint: "Мои загруженные файлы",
            Icon: Folder,
            keywords: ["files", "storage", "uploads", "файлы", "документы"],
            perform: go("/files"),
            perm: "Permissions.Files.Upload",
          },
          {
            id: "nav-students",
            label: "Ученики",
            hint: "Список учеников школы",
            Icon: GraduationCap,
            keywords: ["students", "ученики", "people", "люди"],
            perform: go("/students"),
            perm: "Permissions.People.Students.View",
          },
          {
            id: "nav-teachers",
            label: "Преподаватели",
            hint: "Список преподавателей",
            Icon: Users,
            keywords: ["teachers", "преподаватели", "people"],
            perform: go("/teachers"),
            perm: "Permissions.People.Teachers.View",
          },
          {
            id: "nav-guardians",
            label: "Представители",
            hint: "Родители и опекуны",
            Icon: Users,
            keywords: ["guardians", "представители", "родители"],
            perform: go("/guardians"),
            perm: "Permissions.People.Guardians.View",
          },
          {
            id: "nav-subjects",
            label: "Направления",
            hint: "Дерево направлений",
            Icon: FolderTree,
            keywords: ["subjects", "направления", "curriculum", "программа"],
            perform: go("/subjects"),
            perm: "Permissions.Curriculum.Subjects.View",
          },
          {
            id: "nav-courses",
            label: "Курсы",
            hint: "Программы обучения",
            Icon: BookOpen,
            keywords: ["courses", "курсы", "curriculum", "программа"],
            perform: go("/courses"),
            perm: "Permissions.Curriculum.Courses.View",
          },
          {
            id: "nav-study-groups",
            label: "Группы",
            hint: "Учебные группы",
            Icon: UsersRound,
            keywords: ["study groups", "группы", "learning"],
            perform: go("/study-groups"),
            perm: "Permissions.StudyGroups.StudyGroups.View",
          },
          {
            id: "nav-schedule",
            label: "Расписание",
            hint: "Календарь занятий",
            Icon: CalendarDays,
            keywords: ["schedule", "calendar", "расписание", "календарь", "занятия"],
            perform: go("/schedule"),
            perm: "Permissions.Scheduling.Sessions.View",
          },
          {
            id: "nav-attendance",
            label: "Посещаемость",
            hint: "Таблица посещаемости",
            Icon: ClipboardCheck,
            keywords: ["attendance", "посещаемость"],
            perform: go("/attendance"),
            perm: "Permissions.Scheduling.Attendance.View",
          },
          {
            id: "nav-tariffs",
            label: "Тарифы",
            hint: "Тарифы оплаты",
            Icon: Wallet,
            keywords: ["tariffs", "тарифы", "payments", "оплаты"],
            perform: go("/payments/tariffs"),
            perm: "Permissions.Payments.Tariffs.View",
          },
          {
            id: "nav-student-invoices",
            label: "Счета учеников",
            hint: "Счета за обучение",
            Icon: Receipt,
            keywords: ["student invoices", "счета", "оплаты", "payments"],
            perform: go("/payments/invoices"),
            perm: "Permissions.Payments.StudentInvoices.View",
          },
          {
            id: "nav-debtors",
            label: "Должники",
            hint: "Отчёт по задолженностям",
            Icon: TriangleAlert,
            keywords: ["debtors", "должники", "долг", "debt"],
            perform: go("/payments/debtors"),
            perm: "Permissions.Payments.StudentInvoices.Export",
          },
          {
            id: "nav-revenue",
            label: "Выручка",
            hint: "Отчёт по поступлениям",
            Icon: TrendingUp,
            keywords: ["revenue", "выручка", "поступления", "отчёт"],
            perform: go("/payments/revenue"),
            perm: "Permissions.Payments.StudentInvoices.Export",
          },
          {
            id: "nav-users",
            label: "Пользователи",
            hint: "Каталог пользователей",
            Icon: Users,
            keywords: ["users", "пользователи", "identity", "members"],
            perform: go("/identity/users"),
            perm: "Permissions.Users.Update",
          },
          {
            id: "nav-roles",
            label: "Роли",
            hint: "Права и назначение ролей",
            Icon: ShieldCheck,
            keywords: ["roles", "роли", "permissions", "rbac"],
            perform: go("/identity/roles"),
            perm: "Permissions.Roles.Update",
          },
          {
            id: "nav-groups",
            label: "Группы доступа",
            hint: "Группы пользователей и роли",
            Icon: UsersRound,
            keywords: ["access groups", "группы доступа", "identity", "teams"],
            perform: go("/identity/groups"),
            perm: "Permissions.Groups.Update",
          },
          {
            id: "nav-tickets",
            label: "Обращения",
            hint: "Заявки в поддержку",
            Icon: LifeBuoy,
            keywords: ["tickets", "обращения", "support", "helpdesk", "заявки"],
            perform: go("/tickets"),
            perm: "Permissions.Tickets.View",
          },
          {
            id: "nav-subscription",
            label: "Подписка",
            hint: "Подписка школы на Edvantix",
            Icon: Receipt,
            keywords: ["subscription", "подписка", "billing"],
            perform: go("/subscription"),
            perm: "Permissions.Billing.View",
          },
          {
            id: "nav-invoices",
            label: "Счета подписки",
            hint: "История счетов Billing",
            Icon: Receipt,
            keywords: ["invoices", "счета", "billing", "подписка"],
            perform: go("/invoices"),
            perm: "Permissions.Billing.View",
          },
          {
            id: "nav-school-settings",
            label: "Настройки школы",
            hint: "Часовой пояс, валюта",
            Icon: Landmark,
            keywords: ["school settings", "настройки школы", "timezone", "currency", "валюта"],
            perform: go("/settings/school"),
            perm: "Permissions.SchoolSettings.Manage",
          },
          {
            id: "nav-health",
            label: "Здоровье",
            hint: "Проба готовности и зависимости",
            Icon: HeartPulse,
            keywords: ["health", "status", "uptime", "здоровье", "redis", "postgres"],
            perform: go("/system/health"),
          },
          {
            id: "nav-audits",
            label: "Аудит",
            hint: "Действия, безопасность, изменения",
            Icon: ScrollText,
            keywords: ["audit", "аудит", "log", "журнал", "security", "trace"],
            perform: go("/system/audits"),
            perm: "Permissions.AuditTrails.View",
          },
          {
            id: "nav-trash",
            label: "Корзина",
            hint: "Удалённые записи",
            Icon: ScrollText,
            keywords: ["trash", "корзина", "recycle", "deleted", "restore", "восстановить"],
            perform: go("/system/trash"),
            anyPerm: ALL_TRASH_PERMISSIONS,
          },
          {
            id: "nav-sessions",
            label: "Сессии",
            hint: "Активные сессии пользователей",
            Icon: Shield,
            keywords: ["sessions", "сессии", "devices", "logins"],
            perform: go("/system/sessions"),
            perm: "Permissions.Sessions.ViewAll",
          },
          {
            id: "nav-settings",
            label: "Настройки",
            Icon: SettingsIcon,
            keywords: ["settings", "настройки", "preferences", "config"],
            perform: go("/settings"),
          },
        ],
      },
      {
        heading: "Create",
        items: [
          {
            id: "create-user",
            label: "Create user",
            hint: "Register a new account",
            Icon: Plus,
            keywords: ["new", "invite", "register", "identity"],
            perform: go("/identity/users?action=create"),
            perm: "Permissions.Users.Create",
          },
          {
            id: "create-role",
            label: "Create role",
            hint: "Define a new permission set",
            Icon: Plus,
            keywords: ["new", "permissions", "rbac"],
            perform: go("/identity/roles?action=create"),
            perm: "Permissions.Roles.Create",
          },
          {
            id: "create-group",
            label: "Create group",
            hint: "Organize members",
            Icon: Plus,
            keywords: ["new", "team", "org"],
            perform: go("/identity/groups?action=create"),
            perm: "Permissions.Groups.Create",
          },
          {
            id: "create-ticket",
            label: "Create ticket",
            hint: "File a support request",
            Icon: Plus,
            keywords: ["new", "support", "issue"],
            perform: go("/tickets?action=create"),
            perm: "Permissions.Tickets.Create",
          },
          {
            id: "create-channel",
            label: "Create chat channel",
            hint: "Start a new conversation space",
            Icon: Plus,
            keywords: ["new", "chat", "channel"],
            perform: go("/chat?action=create-channel"),
            perm: "Permissions.Chat.Channels.Create",
          },
          {
            id: "create-file",
            label: "Upload file",
            hint: "Add to your storage",
            Icon: Plus,
            keywords: ["new", "upload", "attach"],
            perform: go("/files?action=upload"),
            perm: "Permissions.Files.Upload",
          },
        ],
      },
      {
        heading: "Account",
        items: [
          {
            id: "acc-profile",
            label: "Profile",
            hint: "Name, email, contact",
            Icon: UserRound,
            perform: go("/settings/profile"),
          },
          {
            id: "acc-security",
            label: "Security",
            hint: "Password, 2FA, sessions",
            Icon: Shield,
            keywords: ["password", "2fa", "sessions"],
            perform: go("/settings/security"),
          },
          {
            id: "acc-keys",
            label: "API keys",
            hint: "Generate & rotate",
            Icon: KeyRound,
            keywords: ["token", "credentials"],
            perform: go("/settings/api-keys"),
          },
          {
            id: "acc-notifications",
            label: "Notifications",
            hint: "Email preferences",
            Icon: Sparkles,
            perform: go("/settings/notifications"),
          },
          {
            id: "acc-appearance",
            label: "Appearance",
            hint: "Theme, accent, font, density",
            Icon: Palette,
            keywords: ["theme", "font", "density", "dark", "light"],
            perform: go("/settings/appearance"),
          },
        ],
      },
      {
        heading: "Theme",
        items: [
          {
            id: "theme-light",
            label: "Switch to light",
            Icon: Sun,
            keywords: ["bright", "day"],
            perform: () => setMode("light"),
          },
          {
            id: "theme-dark",
            label: "Switch to dark",
            Icon: Moon,
            keywords: ["night", "oled"],
            perform: () => setMode("dark"),
          },
          {
            id: "theme-system",
            label: "Follow system theme",
            Icon: Monitor,
            keywords: ["auto"],
            perform: () => setMode("system"),
          },
        ],
      },
      {
        heading: "Accent",
        items: accents.map((a) => ({
          id: `accent-${a.id}`,
          label: `Set accent: ${a.label}`,
          hint: a.description,
          Icon: Palette,
          keywords: ["color", "brand", a.id],
          perform: () => setAccent(a.id),
        })),
      },
      {
        heading: "Session",
        items: [
          {
            id: "sess-logout",
            label: "Sign out",
            hint: "End this session",
            Icon: LogOut,
            keywords: ["logout", "exit", "quit"],
            perform: () => {
              close();
              logout();
            },
          },
        ],
      },
    ];
    // Drop items the user can't access, then drop any group left empty —
    // same shape as visibleSections() in layout/nav-data.ts.
    return allGroups
      .map((g) => ({ ...g, items: g.items.filter(visible) }))
      .filter((g) => g.items.length > 0);
  }, [navigate, onOpenChange, setMode, setAccent, logout, permissions]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className={cn(
          "max-w-[640px] p-0 sm:max-w-[640px]",
          "bg-[var(--color-popover)]",
        )}
      >
        <DialogTitle className="sr-only">Command palette</DialogTitle>
        <DialogDescription className="sr-only">
          Search across pages, account actions, theme and accent. Use arrow keys to navigate; Enter to select.
        </DialogDescription>

        <Command
          loop
          className="flex flex-col"
          // cmdk sets [cmdk-...] data attrs we hook into with selectors below.
        >
          {/* Search row — mirrors EntitySearch shape (rounded-xl, soft icon left). */}
          <div className="flex items-center gap-2.5 border-b border-border px-4 py-3">
            <Search className="h-[18px] w-[18px] shrink-0 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" aria-hidden />
            <Command.Input
              placeholder="Type a command or search…"
              aria-label="Search commands"
              className={cn(
                "h-7 flex-1 bg-transparent text-[14px] tracking-tight placeholder:text-[var(--color-muted-foreground)]",
                "focus:outline-none focus-visible:outline-none focus-visible:shadow-none",
              )}
              autoFocus
            />
            <kbd className="rounded border border-border bg-[var(--color-muted)] px-1.5 py-px text-[10px] tracking-tight text-[var(--color-muted-foreground)]">
              Esc
            </kbd>
          </div>

          {/* Results */}
          <Command.List className="max-h-[420px] overflow-y-auto px-2 py-2">
            <Command.Empty className="px-4 py-12 text-center">
              <p className="text-sm font-medium tracking-tight">No matches</p>
              <p className="mt-1 text-xs text-[var(--color-muted-foreground)]">
                Try a different keyword — page name, entity, theme, accent, or sign-out.
              </p>
            </Command.Empty>

            {groups.map((group) => (
              <Command.Group
                key={group.heading}
                heading={group.heading}
                className={cn(
                  // Heading text styling via cmdk's nested rendering.
                  "[&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:pb-1 [&_[cmdk-group-heading]]:pt-3",
                  "[&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:font-semibold",
                  "[&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-wider",
                  "[&_[cmdk-group-heading]]:text-[var(--color-muted-foreground)]",
                )}
              >
                {group.items.map((item) => (
                  <CommandRow key={item.id} item={item} />
                ))}
              </Command.Group>
            ))}
          </Command.List>

          {/* Footer */}
          <div className="flex items-center justify-between border-t border-border px-4 py-2.5">
            <div className="flex items-center gap-3 text-[11px] text-[var(--color-muted-foreground)]">
              <span className="flex items-center gap-1">
                <kbd className="rounded border border-border bg-[var(--color-muted)] px-1 py-px text-[9px]">↑</kbd>
                <kbd className="rounded border border-border bg-[var(--color-muted)] px-1 py-px text-[9px]">↓</kbd>
                navigate
              </span>
              <span className="flex items-center gap-1">
                <kbd className="rounded border border-border bg-[var(--color-muted)] px-1 py-px text-[9px]">↵</kbd>
                select
              </span>
            </div>
            <span className="text-[11px] text-[var(--color-muted-foreground)]">
              v0.1
            </span>
          </div>
        </Command>
      </DialogContent>
    </Dialog>
  );
}

function CommandRow({ item }: { item: ActionItem }) {
  const { Icon, label, hint, keywords, perform } = item;
  return (
    <Command.Item
      value={[label, hint, ...(keywords ?? [])].filter(Boolean).join(" ")}
      onSelect={perform}
      className={cn(
        "group/cmd flex cursor-default select-none items-center gap-3 rounded-md px-2.5 py-2 text-sm",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        "outline-none focus:outline-none focus-visible:outline-none focus-visible:shadow-none",
        "hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)]",
        "data-[selected=true]:bg-[var(--color-primary-soft)] data-[selected=true]:text-[var(--color-foreground)]",
      )}
    >
      <span
        aria-hidden
        className={cn(
          "grid h-7 w-7 shrink-0 place-items-center rounded-md",
          "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
          "transition-colors group-data-[selected=true]/cmd:bg-[var(--color-primary-soft)] group-data-[selected=true]/cmd:text-[var(--color-primary)]",
        )}
      >
        <Icon className="h-3.5 w-3.5" />
      </span>
      <span className="flex min-w-0 flex-1 flex-col">
        <span className="truncate font-medium tracking-tight">{label}</span>
        {hint && (
          <span className="truncate text-[11px] text-[var(--color-muted-foreground)]">
            {hint}
          </span>
        )}
      </span>
    </Command.Item>
  );
}
