import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  BadgeCheck,
  Ban,
  CalendarClock,
  CalendarDays,
  ChevronRight,
  Clock,
  Link2,
  Link2Off,
  Mail,
  Pencil,
  Phone,
  Trash2,
  Users,
  UsersRound,
} from "lucide-react";
import { toast } from "sonner";
import {
  activateTeacher,
  deactivateTeacher,
  deleteTeacher,
  getTeacherById,
  linkTeacherUser,
  unlinkTeacherUser,
  updateTeacher,
  type TeacherDto,
  type TeacherStatus,
  type UpdateTeacherInput,
} from "@/api/people";
import { getTeacherWorkload } from "@/api/scheduling";
import { searchStudyGroups } from "@/api/study-groups";
import {
  STATUS_LABEL as GROUP_STATUS_LABEL,
  STATUS_TONE as GROUP_STATUS_TONE,
} from "@/pages/study-groups/study-groups-ui";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  EntityDetailAvatar,
  EntityDetailBack,
  EntityDetailHero,
  EntityDetailMeta,
  EntityDetailSection,
  EntityDetailStat,
  EntityStatusBadge,
  Field,
  type EntityStatusTone,
} from "@/components/list";
import { describe, formatDate, pad2 } from "@/lib/list-helpers";
import { ConfirmDeleteDialog, LinkUserDialog } from "./student-detail";

const STATUS_TONE: Record<TeacherStatus, EntityStatusTone> = {
  Active: "success",
  Inactive: "default",
};
const STATUS_LABEL: Record<TeacherStatus, string> = {
  Active: "Активен",
  Inactive: "Неактивен",
};

export function TeacherDetailPage() {
  const { teacherId = "" } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const perms = useAuth().user?.permissions ?? [];
  const canUpdate = perms.includes("Permissions.People.Teachers.Update");
  const canDelete = perms.includes("Permissions.People.Teachers.Delete");
  const canViewSchedule = perms.includes("Permissions.Scheduling.Sessions.View");

  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [linkOpen, setLinkOpen] = useState(false);

  const query = useQuery({
    queryKey: ["teacher", teacherId],
    queryFn: () => getTeacherById(teacherId),
    enabled: Boolean(teacherId),
  });

  const teacher = query.data;

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["teacher", teacherId] });
    void queryClient.invalidateQueries({ queryKey: ["teachers"] });
  };

  const deactivateMut = useMutation({
    mutationFn: (id: string) => deactivateTeacher(id),
    onSuccess: () => {
      toast.success("Преподаватель деактивирован");
      invalidate();
    },
    onError: (e) => toast.error("Не удалось деактивировать", { description: describe(e) }),
  });

  const activateMut = useMutation({
    mutationFn: (id: string) => activateTeacher(id),
    onSuccess: () => {
      toast.success("Преподаватель активирован");
      invalidate();
    },
    onError: (e) => toast.error("Не удалось активировать", { description: describe(e) }),
  });

  const unlinkMut = useMutation({
    mutationFn: (id: string) => unlinkTeacherUser(id),
    onSuccess: () => {
      toast.success("Учётная запись отвязана");
      invalidate();
    },
    onError: (e) => toast.error("Не удалось отвязать", { description: describe(e) }),
  });

  if (query.isLoading) {
    return (
      <div>
        <EntityDetailBack to="/teachers" label="К списку преподавателей" />
        <div className="h-40 animate-pulse rounded-xl bg-[var(--color-muted)]" />
      </div>
    );
  }

  if (query.isError || !teacher) {
    return (
      <div>
        <EntityDetailBack to="/teachers" label="К списку преподавателей" />
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {query.error ? describe(query.error) : "Преподаватель не найден"}
        </div>
      </div>
    );
  }

  const isInactive = teacher.status === "Inactive";

  return (
    <div>
      <EntityDetailBack to="/teachers" label="К списку преподавателей" />

      <EntityDetailHero
        avatar={<EntityDetailAvatar name={teacher.displayName} icon={Users} />}
        title={teacher.displayName}
        badges={
          <EntityStatusBadge tone={STATUS_TONE[teacher.status]}>
            {STATUS_LABEL[teacher.status]}
          </EntityStatusBadge>
        }
        subtitle={teacher.specializations.join(", ") || "Специализации не указаны"}
        actions={
          canUpdate ? (
            <>
              <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setEditOpen(true)}>
                <Pencil className="size-3.5" />
                Изменить
              </Button>
              {isInactive ? (
                <Button
                  variant="outline"
                  size="sm"
                  className="gap-1.5"
                  disabled={activateMut.isPending}
                  onClick={() => activateMut.mutate(teacher.id)}
                >
                  <BadgeCheck className="size-3.5" />
                  Активировать
                </Button>
              ) : (
                <Button
                  variant="outline"
                  size="sm"
                  className="gap-1.5"
                  disabled={deactivateMut.isPending}
                  onClick={() => deactivateMut.mutate(teacher.id)}
                >
                  <Ban className="size-3.5" />
                  Деактивировать
                </Button>
              )}
              {canDelete && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="gap-1.5 text-[var(--color-destructive)]"
                  onClick={() => setDeleteOpen(true)}
                >
                  <Trash2 className="size-3.5" />
                </Button>
              )}
            </>
          ) : undefined
        }
        stats={
          teacher.hourlyRate != null ? (
            <EntityDetailStat value={teacher.hourlyRate} label="ставка / час" tone="primary" />
          ) : undefined
        }
        meta={
          <>
            <EntityDetailMeta icon={Phone}>{teacher.phone || "—"}</EntityDetailMeta>
            <EntityDetailMeta icon={Mail}>{teacher.email || "—"}</EntityDetailMeta>
          </>
        }
      />

      <div className="space-y-4">
        {teacher.bio && (
          <EntityDetailSection title="О преподавателе">
            <p className="whitespace-pre-wrap text-[13px] text-[var(--color-foreground)]">{teacher.bio}</p>
          </EntityDetailSection>
        )}

        <EntityDetailSection title="Учётная запись" icon={Link2}>
          {teacher.userId ? (
            <div className="flex items-center justify-between gap-3">
              <div className="min-w-0 text-[13px]">
                <p className="text-[var(--color-foreground)]">Привязана учётная запись</p>
                <code className="text-[12px] text-[var(--color-muted-foreground)]">{teacher.userId}</code>
              </div>
              {canUpdate && (
                <Button
                  variant="outline"
                  size="sm"
                  className="gap-1.5"
                  disabled={unlinkMut.isPending}
                  onClick={() => unlinkMut.mutate(teacher.id)}
                >
                  <Link2Off className="size-3.5" />
                  Отвязать
                </Button>
              )}
            </div>
          ) : (
            <div className="flex items-center justify-between gap-3">
              <p className="text-[13px] text-[var(--color-muted-foreground)]">
                Преподаватель не привязан к учётной записи.
              </p>
              {canUpdate && (
                <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setLinkOpen(true)}>
                  <Link2 className="size-3.5" />
                  Привязать
                </Button>
              )}
            </div>
          )}
        </EntityDetailSection>

        <TeacherWorkloadSection teacherId={teacher.id} canView={canViewSchedule} />

        <TeacherGroupsSection teacherId={teacher.id} />
      </div>

      <EditTeacherDialog open={editOpen} onClose={() => setEditOpen(false)} teacher={teacher} onSaved={invalidate} />
      <LinkUserDialog
        open={linkOpen}
        onClose={() => setLinkOpen(false)}
        onSubmit={(userId) => linkTeacherUser(teacher.id, userId)}
        onSaved={invalidate}
      />
      <ConfirmDeleteDialog
        open={deleteOpen}
        onClose={() => setDeleteOpen(false)}
        name={teacher.displayName}
        onConfirm={async () => {
          await deleteTeacher(teacher.id);
          toast.success("Преподаватель удалён");
          void queryClient.invalidateQueries({ queryKey: ["teachers"] });
          navigate("/teachers");
        }}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Нагрузка — GET /teachers/{id}/workload (Scheduling, Sessions.View).
//  Active groups / sessions / hours over a 7 / 14 / 30-day window ahead.
// ───────────────────────────────────────────────────────────────────────

const WORKLOAD_WINDOWS = [7, 14, 30] as const;
type WorkloadWindow = (typeof WORKLOAD_WINDOWS)[number];

function isoDate(d: Date): string {
  return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
}

function formatHours(hours: number): string {
  return Number.isInteger(hours) ? String(hours) : hours.toFixed(1).replace(".", ",");
}

function TeacherWorkloadSection({ teacherId, canView }: { teacherId: string; canView: boolean }) {
  const [windowDays, setWindowDays] = useState<WorkloadWindow>(7);

  const { from, to } = useMemo(() => {
    const start = new Date();
    const end = new Date();
    end.setDate(end.getDate() + windowDays);
    return { from: isoDate(start), to: isoDate(end) };
  }, [windowDays]);

  const query = useQuery({
    queryKey: ["teacher-workload", teacherId, { from, to }],
    queryFn: () => getTeacherWorkload(teacherId, { from, to }),
    enabled: canView,
  });

  if (!canView) {
    return (
      <EntityDetailSection title="Нагрузка" icon={CalendarClock}>
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          Недостаточно прав для просмотра нагрузки преподавателя.
        </p>
      </EntityDetailSection>
    );
  }

  return (
    <EntityDetailSection
      title="Нагрузка"
      icon={CalendarClock}
      description={`Активные группы, занятия и часы с ${formatDate(from)} по ${formatDate(to)}.`}
      action={
        <div className="flex gap-1">
          {WORKLOAD_WINDOWS.map((d) => (
            <Button
              key={d}
              variant={d === windowDays ? "default" : "outline"}
              size="sm"
              onClick={() => setWindowDays(d)}
            >
              {d} дн.
            </Button>
          ))}
        </div>
      }
    >
      {query.isLoading ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : query.isError ? (
        <p className="text-[13px] text-[var(--color-destructive)]">{describe(query.error)}</p>
      ) : query.data ? (
        <div className="flex flex-wrap gap-2">
          <EntityDetailStat
            icon={UsersRound}
            value={query.data.activeGroupsCount}
            label="активных групп"
            tone="primary"
          />
          <EntityDetailStat
            icon={CalendarDays}
            value={query.data.sessionsCount}
            label="занятий за период"
          />
          <EntityDetailStat icon={Clock} value={formatHours(query.data.totalHours)} label="часов" />
        </div>
      ) : null}
    </EntityDetailSection>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Группы преподавателя — GET /study-groups?teacherId= (groups where he is
//  the lead), plus a shortcut into the schedule filtered to this teacher.
// ───────────────────────────────────────────────────────────────────────

function TeacherGroupsSection({ teacherId }: { teacherId: string }) {
  const query = useQuery({
    queryKey: ["study-groups", { teacherId, for: "teacher-groups" }],
    queryFn: () => searchStudyGroups({ teacherId, pageSize: 100 }),
  });
  const groups = query.data?.items ?? [];

  return (
    <EntityDetailSection
      title="Группы преподавателя"
      icon={UsersRound}
      description="Учебные группы, где преподаватель — ведущий."
      action={
        <Button asChild variant="outline" size="sm" className="gap-1.5">
          <Link to={`/schedule?teacherId=${encodeURIComponent(teacherId)}`}>
            <CalendarDays className="size-3.5" />
            Расписание преподавателя
          </Link>
        </Button>
      }
    >
      {query.isLoading ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : query.isError ? (
        <p className="text-[13px] text-[var(--color-destructive)]">{describe(query.error)}</p>
      ) : groups.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          Преподаватель не ведёт ни одной группы.
        </p>
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
          {groups.map((g) => (
            <li key={g.id} className="py-3 first:pt-0 last:pb-0">
              <Link
                to={`/study-groups/${g.id}`}
                className="flex items-center justify-between gap-3"
              >
                <div className="min-w-0">
                  <p className="truncate text-[13px] font-medium text-[var(--color-foreground)]">
                    {g.name}
                    <span className="ml-2 font-mono text-[11px] text-[var(--color-muted-foreground)]">
                      {g.code}
                    </span>
                  </p>
                  <p className="mt-0.5 flex flex-wrap items-center gap-2 text-[11.5px] text-[var(--color-muted-foreground)]">
                    <EntityStatusBadge tone={GROUP_STATUS_TONE[g.status]}>
                      {GROUP_STATUS_LABEL[g.status]}
                    </EntityStatusBadge>
                    <span>
                      {g.activeEnrollmentCount}/{g.capacity} учеников · с {formatDate(g.startDate)}
                    </span>
                  </p>
                </div>
                <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </EntityDetailSection>
  );
}

function EditTeacherDialog({
  open,
  onClose,
  teacher,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  teacher: TeacherDto;
  onSaved: () => void;
}) {
  const [lastName, setLastName] = useState(teacher.lastName);
  const [firstName, setFirstName] = useState(teacher.firstName);
  const [middleName, setMiddleName] = useState(teacher.middleName ?? "");
  const [phone, setPhone] = useState(teacher.phone);
  const [email, setEmail] = useState(teacher.email);
  const [bio, setBio] = useState(teacher.bio ?? "");
  const [specializations, setSpecializations] = useState(teacher.specializations.join(", "));
  const [hourlyRate, setHourlyRate] = useState(teacher.hourlyRate != null ? String(teacher.hourlyRate) : "");

  useEffect(() => {
    if (open) {
      setLastName(teacher.lastName);
      setFirstName(teacher.firstName);
      setMiddleName(teacher.middleName ?? "");
      setPhone(teacher.phone);
      setEmail(teacher.email);
      setBio(teacher.bio ?? "");
      setSpecializations(teacher.specializations.join(", "));
      setHourlyRate(teacher.hourlyRate != null ? String(teacher.hourlyRate) : "");
    }
  }, [open, teacher]);

  const mutation = useMutation({
    mutationFn: (input: UpdateTeacherInput) => updateTeacher(input),
    onSuccess: () => {
      toast.success("Изменения сохранены");
      onSaved();
      onClose();
    },
    onError: (e) => toast.error("Не удалось сохранить", { description: describe(e) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const specs = specializations
      .split(",")
      .map((s) => s.trim())
      .filter(Boolean);
    const rate = hourlyRate.trim() ? Number(hourlyRate) : null;
    mutation.mutate({
      teacherId: teacher.id,
      lastName: lastName.trim(),
      firstName: firstName.trim(),
      middleName: middleName.trim() || null,
      phone: phone.trim(),
      email: email.trim(),
      bio: bio.trim() || null,
      specializations: specs.length > 0 ? specs : null,
      hourlyRate: rate != null && !Number.isNaN(rate) ? rate : null,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Изменить преподавателя</DialogTitle>
            <DialogDescription>Статус меняется отдельными действиями.</DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="et-last" label="Фамилия" required>
                <Input id="et-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
              </Field>
              <Field id="et-first" label="Имя" required>
                <Input id="et-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
              </Field>
            </div>
            <Field id="et-middle" label="Отчество">
              <Input id="et-middle" value={middleName} onChange={(e) => setMiddleName(e.target.value)} />
            </Field>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="et-phone" label="Телефон" required>
                <Input id="et-phone" value={phone} onChange={(e) => setPhone(e.target.value)} required />
              </Field>
              <Field id="et-email" label="E-mail" required>
                <Input id="et-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
              </Field>
            </div>
            <Field id="et-specs" label="Специализации" hint="Через запятую">
              <Input id="et-specs" value={specializations} onChange={(e) => setSpecializations(e.target.value)} />
            </Field>
            <Field id="et-rate" label="Часовая ставка">
              <Input id="et-rate" type="number" min="0" step="0.01" value={hourlyRate} onChange={(e) => setHourlyRate(e.target.value)} />
            </Field>
            <Field id="et-bio" label="Биография">
              <textarea
                id="et-bio"
                value={bio}
                onChange={(e) => setBio(e.target.value)}
                rows={3}
                className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] px-3 py-2 text-[13px] text-[var(--color-foreground)] outline-none placeholder:text-[var(--color-muted-foreground)] focus:border-[oklch(from_var(--color-ring)_l_c_h_/_0.30)] focus:ring-2 focus:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.10)]"
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Сохранение…" : "Сохранить"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
