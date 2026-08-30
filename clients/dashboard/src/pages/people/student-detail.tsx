import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Archive,
  ArchiveRestore,
  CalendarDays,
  ChevronRight,
  ClipboardCheck,
  GraduationCap,
  Link2,
  Link2Off,
  Mail,
  NotebookPen,
  Pencil,
  Phone,
  Plus,
  Star,
  Trash2,
  Users,
  UsersRound,
  X,
} from "lucide-react";
import { toast } from "sonner";
import {
  addStudentGuardian,
  addStudentNote,
  archiveStudent,
  deleteStudent,
  deleteStudentNote,
  getStudentById,
  getStudentGuardians,
  getStudentNotes,
  linkStudentUser,
  removeStudentGuardian,
  restoreStudent,
  searchGuardians,
  setPrimaryPayer,
  unlinkStudentUser,
  updateStudent,
  type StudentDetailDto,
  type StudentStatus,
  type UpdateStudentInput,
} from "@/api/people";
import { searchUsers, type UserDto } from "@/api/identity";
import {
  getStudentEnrollments,
  searchStudyGroups,
  type EnrollmentStatus,
} from "@/api/study-groups";
import { getStudentAttendance } from "@/api/scheduling";
import {
  ATTENDANCE_STATUS_LABEL,
  ATTENDANCE_STATUS_TONE,
} from "@/pages/scheduling/scheduling-ui";
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
  Combobox,
  EntityDetailAvatar,
  EntityDetailBack,
  EntityDetailHero,
  EntityDetailMeta,
  EntityDetailSection,
  EntityDetailStat,
  EntityInitialsAvatar,
  EntityStatusBadge,
  Field,
  type EntityStatusTone,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe, formatDate, formatDateTimeMono } from "@/lib/list-helpers";

const STATUS_TONE: Record<StudentStatus, EntityStatusTone> = {
  Lead: "info",
  Active: "success",
  Paused: "warning",
  Archived: "default",
};
const STATUS_LABEL: Record<StudentStatus, string> = {
  Lead: "Лид",
  Active: "Активен",
  Paused: "Пауза",
  Archived: "Архив",
};

const ENROLLMENT_STATUS_LABEL: Record<EnrollmentStatus, string> = {
  Active: "Активен",
  Paused: "Пауза",
  Left: "Ушёл",
  Completed: "Завершил",
};
const ENROLLMENT_STATUS_TONE: Record<EnrollmentStatus, EntityStatusTone> = {
  Active: "success",
  Paused: "warning",
  Left: "default",
  Completed: "info",
};

type Tab = "profile" | "guardians" | "notes" | "groups" | "attendance";

export function StudentDetailPage() {
  const { studentId = "" } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const perms = useAuth().user?.permissions ?? [];
  const canUpdate = perms.includes("Permissions.People.Students.Update");
  const canDelete = perms.includes("Permissions.People.Students.Delete");
  const canViewNotes = perms.includes("Permissions.People.Students.ViewNotes");
  const canViewGroups = perms.includes("Permissions.StudyGroups.Enrollments.View");
  const canViewAttendance = perms.includes("Permissions.Scheduling.Attendance.View");

  const [tab, setTab] = useState<Tab>("profile");
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [linkOpen, setLinkOpen] = useState(false);

  const query = useQuery({
    queryKey: ["student", studentId],
    queryFn: () => getStudentById(studentId),
    enabled: Boolean(studentId),
  });

  const student = query.data;

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["student", studentId] });
    void queryClient.invalidateQueries({ queryKey: ["students"] });
  };

  const archiveMut = useMutation({
    mutationFn: (id: string) => archiveStudent(id),
    onSuccess: () => {
      toast.success("Ученик архивирован");
      invalidate();
    },
    onError: (e) => toast.error("Не удалось архивировать", { description: describe(e) }),
  });

  const restoreMut = useMutation({
    mutationFn: (id: string) => restoreStudent(id),
    onSuccess: () => {
      toast.success("Ученик восстановлен");
      invalidate();
    },
    onError: (e) => toast.error("Не удалось восстановить", { description: describe(e) }),
  });

  const unlinkMut = useMutation({
    mutationFn: (id: string) => unlinkStudentUser(id),
    onSuccess: () => {
      toast.success("Учётная запись отвязана");
      invalidate();
    },
    onError: (e) => toast.error("Не удалось отвязать", { description: describe(e) }),
  });

  if (query.isLoading) {
    return (
      <div>
        <EntityDetailBack to="/students" label="К списку учеников" />
        <div className="h-40 animate-pulse rounded-xl bg-[var(--color-muted)]" />
      </div>
    );
  }

  if (query.isError || !student) {
    return (
      <div>
        <EntityDetailBack to="/students" label="К списку учеников" />
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {query.error ? describe(query.error) : "Ученик не найден"}
        </div>
      </div>
    );
  }

  const isArchived = student.status === "Archived";

  return (
    <div>
      <EntityDetailBack to="/students" label="К списку учеников" />

      <EntityDetailHero
        avatar={<EntityDetailAvatar name={student.displayName} icon={GraduationCap} />}
        title={student.displayName}
        badges={
          <EntityStatusBadge tone={STATUS_TONE[student.status]}>
            {STATUS_LABEL[student.status]}
          </EntityStatusBadge>
        }
        subtitle={`Зачислен ${formatDate(student.enrolledAtUtc)} · источник: ${student.source || "—"}`}
        actions={
          canUpdate ? (
            <>
              <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setEditOpen(true)}>
                <Pencil className="size-3.5" />
                Изменить
              </Button>
              {isArchived ? (
                <Button
                  variant="outline"
                  size="sm"
                  className="gap-1.5"
                  disabled={restoreMut.isPending}
                  onClick={() => restoreMut.mutate(student.id)}
                >
                  <ArchiveRestore className="size-3.5" />
                  Восстановить
                </Button>
              ) : (
                <Button
                  variant="outline"
                  size="sm"
                  className="gap-1.5"
                  disabled={archiveMut.isPending}
                  onClick={() => archiveMut.mutate(student.id)}
                >
                  <Archive className="size-3.5" />
                  В архив
                </Button>
              )}
              {canDelete && (
                <Button variant="ghost" size="sm" className="gap-1.5 text-[var(--color-destructive)]" onClick={() => setDeleteOpen(true)}>
                  <Trash2 className="size-3.5" />
                </Button>
              )}
            </>
          ) : undefined
        }
        stats={
          <>
            <EntityDetailStat icon={Users} value={student.guardianCount} label="представителей" />
            <EntityDetailStat icon={NotebookPen} value={student.noteCount} label="заметок" />
          </>
        }
        meta={
          <>
            <EntityDetailMeta icon={Phone}>{student.phone || "—"}</EntityDetailMeta>
            <EntityDetailMeta icon={Mail}>{student.email || "—"}</EntityDetailMeta>
            <EntityDetailMeta icon={CalendarDays}>
              Рождён {formatDate(student.birthDate)}
            </EntityDetailMeta>
          </>
        }
      />

      <nav aria-label="Разделы ученика" className="mb-5 flex flex-wrap items-center gap-2">
        <TabPill active={tab === "profile"} onClick={() => setTab("profile")} icon={GraduationCap}>
          Профиль
        </TabPill>
        <TabPill active={tab === "guardians"} onClick={() => setTab("guardians")} icon={Users}>
          Представители
        </TabPill>
        {canViewGroups && (
          <TabPill active={tab === "groups"} onClick={() => setTab("groups")} icon={UsersRound}>
            Группы
          </TabPill>
        )}
        {canViewAttendance && (
          <TabPill
            active={tab === "attendance"}
            onClick={() => setTab("attendance")}
            icon={ClipboardCheck}
          >
            Посещаемость
          </TabPill>
        )}
        {canViewNotes && (
          <TabPill active={tab === "notes"} onClick={() => setTab("notes")} icon={NotebookPen}>
            Заметки
          </TabPill>
        )}
      </nav>

      {tab === "profile" && (
        <div className="space-y-4">
          <EntityDetailSection title="Учётная запись" icon={Link2}>
            {student.userId ? (
              <div className="flex items-center justify-between gap-3">
                <div className="min-w-0 text-[13px]">
                  <p className="text-[var(--color-foreground)]">Привязана учётная запись</p>
                  <code className="text-[12px] text-[var(--color-muted-foreground)]">{student.userId}</code>
                </div>
                {canUpdate && (
                  <Button
                    variant="outline"
                    size="sm"
                    className="gap-1.5"
                    disabled={unlinkMut.isPending}
                    onClick={() => unlinkMut.mutate(student.id)}
                  >
                    <Link2Off className="size-3.5" />
                    Отвязать
                  </Button>
                )}
              </div>
            ) : (
              <div className="flex items-center justify-between gap-3">
                <p className="text-[13px] text-[var(--color-muted-foreground)]">
                  Ученик не привязан к учётной записи — вход в систему невозможен.
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

          <EntityDetailSection title="Дальнейшие разделы" icon={CalendarDays}>
            <p className="text-[13px] text-[var(--color-muted-foreground)]">
              Группы, посещаемость и счета появятся здесь после подключения модулей
              StudyGroups, Scheduling и Payments.
            </p>
          </EntityDetailSection>
        </div>
      )}

      {tab === "guardians" && (
        <GuardiansTab studentId={student.id} canUpdate={canUpdate} onChanged={invalidate} />
      )}

      {tab === "groups" && canViewGroups && <GroupsTab studentId={student.id} />}

      {tab === "attendance" && canViewAttendance && (
        <AttendanceTab studentId={student.id} />
      )}

      {tab === "notes" && canViewNotes && (
        <NotesTab studentId={student.id} onChanged={invalidate} />
      )}

      <EditStudentDialog open={editOpen} onClose={() => setEditOpen(false)} student={student} onSaved={invalidate} />
      <LinkUserDialog
        open={linkOpen}
        onClose={() => setLinkOpen(false)}
        onSubmit={(userId) => linkStudentUser(student.id, userId)}
        onSaved={invalidate}
      />
      <ConfirmDeleteDialog
        open={deleteOpen}
        onClose={() => setDeleteOpen(false)}
        name={student.displayName}
        onConfirm={async () => {
          await deleteStudent(student.id);
          toast.success("Ученик удалён");
          void queryClient.invalidateQueries({ queryKey: ["students"] });
          navigate("/students");
        }}
      />
    </div>
  );
}

function TabPill({
  active,
  onClick,
  icon: Icon,
  children,
}: {
  active: boolean;
  onClick: () => void;
  icon: React.ComponentType<{ className?: string }>;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={cn(
        "inline-flex h-8 cursor-pointer items-center gap-1.5 rounded-full border px-3 text-[12px] font-medium transition-colors duration-[var(--duration-fast)]",
        active
          ? "border-transparent bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
          : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
      )}
    >
      <Icon className="size-3.5" aria-hidden />
      {children}
    </button>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Guardians tab
// ───────────────────────────────────────────────────────────────────────

function GuardiansTab({
  studentId,
  canUpdate,
  onChanged,
}: {
  studentId: string;
  canUpdate: boolean;
  onChanged: () => void;
}) {
  const queryClient = useQueryClient();
  const [addOpen, setAddOpen] = useState(false);

  const query = useQuery({
    queryKey: ["student", studentId, "guardians"],
    queryFn: () => getStudentGuardians(studentId),
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["student", studentId, "guardians"] });
    onChanged();
  };

  const removeMut = useMutation({
    mutationFn: (guardianId: string) => removeStudentGuardian(studentId, guardianId),
    onSuccess: () => {
      toast.success("Представитель отвязан");
      invalidate();
    },
    onError: (e) => toast.error("Не удалось отвязать", { description: describe(e) }),
  });

  const primaryMut = useMutation({
    mutationFn: (guardianId: string) => setPrimaryPayer(studentId, guardianId),
    onSuccess: () => {
      toast.success("Плательщик назначен");
      invalidate();
    },
    onError: (e) => toast.error("Не удалось назначить плательщика", { description: describe(e) }),
  });

  const items = query.data ?? [];

  return (
    <EntityDetailSection
      title="Представители"
      icon={Users}
      action={
        canUpdate ? (
          <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setAddOpen(true)}>
            <Plus className="size-3.5" />
            Привязать
          </Button>
        ) : undefined
      }
    >
      {query.isLoading ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : query.isError ? (
        <p className="text-[13px] text-[var(--color-destructive)]">{describe(query.error)}</p>
      ) : items.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          У ученика пока нет представителей.
        </p>
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
          {items.map((link) => (
            <li key={link.id} className="flex items-center justify-between gap-3 py-3 first:pt-0 last:pb-0">
              <div className="flex min-w-0 items-center gap-3">
                <EntityInitialsAvatar name={link.guardian.displayName} size={36} />
                <div className="min-w-0">
                  <p className="truncate text-[13px] font-medium text-[var(--color-foreground)]">
                    {link.guardian.displayName}
                    {link.isPrimaryPayer && (
                      <EntityStatusBadge tone="success" className="ml-2">
                        Плательщик
                      </EntityStatusBadge>
                    )}
                  </p>
                  <p className="truncate text-[11.5px] text-[var(--color-muted-foreground)]">
                    {link.relation || "—"} · {link.guardian.phone || link.guardian.email || "нет контактов"}
                  </p>
                </div>
              </div>
              {canUpdate && (
                <div className="flex shrink-0 items-center gap-1">
                  {!link.isPrimaryPayer && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="gap-1.5"
                      disabled={primaryMut.isPending}
                      onClick={() => primaryMut.mutate(link.guardianId)}
                      title="Сделать плательщиком"
                    >
                      <Star className="size-3.5" />
                    </Button>
                  )}
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-[var(--color-destructive)]"
                    disabled={removeMut.isPending}
                    onClick={() => removeMut.mutate(link.guardianId)}
                    title="Отвязать"
                  >
                    <X className="size-3.5" />
                  </Button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      <AddGuardianDialog
        open={addOpen}
        onClose={() => setAddOpen(false)}
        studentId={studentId}
        existingGuardianIds={items.map((l) => l.guardianId)}
        onSaved={invalidate}
      />
    </EntityDetailSection>
  );
}

function AddGuardianDialog({
  open,
  onClose,
  studentId,
  existingGuardianIds,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  studentId: string;
  existingGuardianIds: string[];
  onSaved: () => void;
}) {
  const [guardianId, setGuardianId] = useState<string | null>(null);
  const [relation, setRelation] = useState("");
  const [isPrimaryPayer, setIsPrimaryPayer] = useState(false);

  useEffect(() => {
    if (!open) {
      setGuardianId(null);
      setRelation("");
      setIsPrimaryPayer(false);
    }
  }, [open]);

  const guardiansQuery = useQuery({
    queryKey: ["guardians", { pageSize: 100 }],
    queryFn: () => searchGuardians({ pageSize: 100 }),
    enabled: open,
  });

  const options = useMemo(
    () =>
      (guardiansQuery.data?.items ?? [])
        .filter((g) => !existingGuardianIds.includes(g.id))
        .map((g) => ({ value: g.id, label: `${g.displayName} · ${g.phone || g.email}` })),
    [guardiansQuery.data, existingGuardianIds],
  );

  const mutation = useMutation({
    mutationFn: (vars: { guardianId: string; relation: string; isPrimaryPayer: boolean }) =>
      addStudentGuardian({ studentId, ...vars }),
    onSuccess: () => {
      toast.success("Представитель привязан");
      onSaved();
      onClose();
    },
    onError: (e) => toast.error("Не удалось привязать", { description: describe(e) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!guardianId) return;
    mutation.mutate({ guardianId, relation: relation.trim(), isPrimaryPayer });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Привязать представителя</DialogTitle>
            <DialogDescription>
              Выберите представителя из справочника и укажите степень родства.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="ag-guardian" label="Представитель" required>
              <Combobox
                label="Представитель"
                value={guardianId}
                onChange={setGuardianId}
                options={options}
                placeholder={guardiansQuery.isLoading ? "Загрузка…" : "Выберите представителя"}
                searchable
                clearable
              />
            </Field>
            <Field id="ag-relation" label="Степень родства" required hint="Например: мать, отец, опекун">
              <Input id="ag-relation" value={relation} onChange={(e) => setRelation(e.target.value)} required />
            </Field>
            <label className="flex items-center gap-2 text-[13px] text-[var(--color-foreground)]">
              <input
                type="checkbox"
                checked={isPrimaryPayer}
                onChange={(e) => setIsPrimaryPayer(e.target.checked)}
              />
              Назначить плательщиком по умолчанию
            </label>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !guardianId}>
              {mutation.isPending ? "Сохранение…" : "Привязать"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Groups tab — the student's full enrollment history (all groups,
//  including finished/left), via GET /students/{id}/enrollments.
// ───────────────────────────────────────────────────────────────────────

function GroupsTab({ studentId }: { studentId: string }) {
  const query = useQuery({
    queryKey: ["student-enrollments", studentId],
    queryFn: () => getStudentEnrollments(studentId),
  });

  const groupsQuery = useQuery({
    queryKey: ["study-groups", { pageSize: 100, for: "student-groups" }],
    queryFn: () => searchStudyGroups({ pageSize: 100 }),
    staleTime: 60_000,
  });
  const groupById = useMemo(() => {
    const m = new Map<string, { code: string; name: string }>();
    for (const g of groupsQuery.data?.items ?? []) m.set(g.id, { code: g.code, name: g.name });
    return m;
  }, [groupsQuery.data]);

  const items = useMemo(() => {
    const list = [...(query.data ?? [])];
    list.sort((a, b) => b.enrolledOn.localeCompare(a.enrolledOn));
    return list;
  }, [query.data]);

  return (
    <EntityDetailSection
      title="Учебные группы"
      icon={UsersRound}
      description="Все группы ученика, включая завершённые и покинутые."
    >
      {query.isLoading ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : query.isError ? (
        <p className="text-[13px] text-[var(--color-destructive)]">{describe(query.error)}</p>
      ) : items.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          Ученик пока не состоял ни в одной группе.
        </p>
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
          {items.map((e) => {
            const g = groupById.get(e.studyGroupId);
            return (
              <li key={e.id} className="py-3 first:pt-0 last:pb-0">
                <Link
                  to={`/study-groups/${e.studyGroupId}`}
                  className="flex items-center justify-between gap-3"
                >
                  <div className="flex min-w-0 items-center gap-3">
                    <EntityInitialsAvatar name={g?.name ?? "Группа"} size={36} />
                    <div className="min-w-0">
                      <p className="truncate text-[13px] font-medium text-[var(--color-foreground)]">
                        {g?.name ?? "Группа"}
                        {g?.code ? (
                          <span className="ml-2 font-mono text-[11px] text-[var(--color-muted-foreground)]">
                            {g.code}
                          </span>
                        ) : null}
                      </p>
                      <p className="truncate text-[11.5px] text-[var(--color-muted-foreground)]">
                        <EntityStatusBadge tone={ENROLLMENT_STATUS_TONE[e.status]}>
                          {ENROLLMENT_STATUS_LABEL[e.status]}
                        </EntityStatusBadge>{" "}
                        · с {formatDate(e.enrolledOn)}
                        {e.leftOn ? ` · до ${formatDate(e.leftOn)}` : ""}
                        {e.leaveReason ? ` · ${e.leaveReason}` : ""}
                      </p>
                    </div>
                  </div>
                  <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </EntityDetailSection>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Attendance tab — the student's attendance history over a period, via
//  GET /students/{id}/attendance?from=&to= (Scheduling, Attendance.View).
// ───────────────────────────────────────────────────────────────────────

function AttendanceTab({ studentId }: { studentId: string }) {
  const [from, setFrom] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().slice(0, 10);
  });
  const [to, setTo] = useState(() => new Date().toISOString().slice(0, 10));

  const query = useQuery({
    queryKey: ["student-attendance", studentId, { from, to }],
    queryFn: () => getStudentAttendance(studentId, from, to),
  });

  const rows = useMemo(
    () =>
      [...(query.data ?? [])].sort((a, b) =>
        b.markedAtUtc.localeCompare(a.markedAtUtc),
      ),
    [query.data],
  );
  const summary = useMemo(() => {
    const s = { Present: 0, Absent: 0, Late: 0, Excused: 0 };
    for (const r of query.data ?? []) s[r.status] += 1;
    return s;
  }, [query.data]);

  return (
    <EntityDetailSection
      title="Посещаемость"
      icon={ClipboardCheck}
      description="История отметок за выбранный период."
      action={
        <div className="flex items-center gap-2">
          <Input
            type="date"
            aria-label="С даты"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            className="h-8 text-[12px]"
          />
          <span className="text-[12px] text-[var(--color-muted-foreground)]">–</span>
          <Input
            type="date"
            aria-label="По дату"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            className="h-8 text-[12px]"
          />
        </div>
      }
    >
      <div className="mb-3 flex flex-wrap gap-2">
        {(["Present", "Absent", "Late", "Excused"] as const).map((k) => (
          <span
            key={k}
            className="inline-flex items-center gap-1.5 rounded-md bg-[var(--color-muted)] px-2 py-1 text-[11.5px] text-[var(--color-foreground)]"
          >
            {ATTENDANCE_STATUS_LABEL[k]}: <span className="tabular-nums">{summary[k]}</span>
          </span>
        ))}
      </div>

      {query.isLoading ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : query.isError ? (
        <p className="text-[13px] text-[var(--color-destructive)]">{describe(query.error)}</p>
      ) : rows.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          За выбранный период отметок нет.
        </p>
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
          {rows.map((a) => (
            <li
              key={a.id}
              className="flex items-center justify-between gap-3 py-2.5 first:pt-0 last:pb-0"
            >
              <div className="min-w-0">
                <Link
                  to={`/sessions/${a.sessionId}`}
                  className="text-[13px] text-[var(--color-foreground)] hover:underline"
                >
                  {formatDate(a.markedAtUtc)}
                </Link>
                {a.comment && (
                  <p className="truncate text-[11.5px] text-[var(--color-muted-foreground)]">
                    {a.comment}
                  </p>
                )}
              </div>
              <EntityStatusBadge tone={ATTENDANCE_STATUS_TONE[a.status]}>
                {ATTENDANCE_STATUS_LABEL[a.status]}
              </EntityStatusBadge>
            </li>
          ))}
        </ul>
      )}
    </EntityDetailSection>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Notes tab
// ───────────────────────────────────────────────────────────────────────

function NotesTab({ studentId, onChanged }: { studentId: string; onChanged: () => void }) {
  const queryClient = useQueryClient();
  const [text, setText] = useState("");

  const query = useQuery({
    queryKey: ["student", studentId, "notes"],
    queryFn: () => getStudentNotes(studentId),
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["student", studentId, "notes"] });
    onChanged();
  };

  const addMut = useMutation({
    mutationFn: (noteText: string) => addStudentNote(studentId, noteText),
    onSuccess: () => {
      setText("");
      invalidate();
    },
    onError: (e) => toast.error("Не удалось добавить заметку", { description: describe(e) }),
  });

  const deleteMut = useMutation({
    mutationFn: (noteId: string) => deleteStudentNote(studentId, noteId),
    onSuccess: () => invalidate(),
    onError: (e) => toast.error("Не удалось удалить заметку", { description: describe(e) }),
  });

  const items = query.data ?? [];

  return (
    <EntityDetailSection title="Внутренние заметки" icon={NotebookPen} description="Видны только пользователям с правом «Просмотр заметок».">
      <form
        onSubmit={(e) => {
          e.preventDefault();
          const trimmed = text.trim();
          if (trimmed) addMut.mutate(trimmed);
        }}
        className="mb-4 space-y-2"
      >
        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          rows={3}
          placeholder="Новая заметка…"
          className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] px-3 py-2 text-[13px] text-[var(--color-foreground)] outline-none placeholder:text-[var(--color-muted-foreground)] focus:border-[oklch(from_var(--color-ring)_l_c_h_/_0.30)] focus:ring-2 focus:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.10)]"
        />
        <div className="flex justify-end">
          <Button type="submit" size="sm" disabled={addMut.isPending || !text.trim()} className="gap-1.5">
            <Plus className="size-3.5" />
            {addMut.isPending ? "Добавление…" : "Добавить"}
          </Button>
        </div>
      </form>

      {query.isLoading ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : query.isError ? (
        <p className="text-[13px] text-[var(--color-destructive)]">{describe(query.error)}</p>
      ) : items.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">Заметок пока нет.</p>
      ) : (
        <ul className="space-y-3">
          {items.map((note) => (
            <li key={note.id} className="rounded-lg border border-[oklch(from_var(--color-border)_l_c_h_/_0.6)] px-3 py-2.5">
              <div className="flex items-start justify-between gap-3">
                <p className="whitespace-pre-wrap text-[13px] text-[var(--color-foreground)]">{note.text}</p>
                <Button
                  variant="ghost"
                  size="sm"
                  className="shrink-0 text-[var(--color-destructive)]"
                  disabled={deleteMut.isPending}
                  onClick={() => deleteMut.mutate(note.id)}
                  title="Удалить"
                >
                  <X className="size-3.5" />
                </Button>
              </div>
              <p className="mt-1 text-[11px] text-[var(--color-muted-foreground)]">
                {formatDateTimeMono(note.createdAtUtc)}
              </p>
            </li>
          ))}
        </ul>
      )}
    </EntityDetailSection>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Edit / link / delete dialogs
// ───────────────────────────────────────────────────────────────────────

function EditStudentDialog({
  open,
  onClose,
  student,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  student: StudentDetailDto;
  onSaved: () => void;
}) {
  const [lastName, setLastName] = useState(student.lastName);
  const [firstName, setFirstName] = useState(student.firstName);
  const [middleName, setMiddleName] = useState(student.middleName ?? "");
  const [birthDate, setBirthDate] = useState(student.birthDate);
  const [phone, setPhone] = useState(student.phone);
  const [email, setEmail] = useState(student.email);
  const [managerUserId, setManagerUserId] = useState(student.managerUserId);
  const [source, setSource] = useState(student.source ?? "");

  useEffect(() => {
    if (open) {
      setLastName(student.lastName);
      setFirstName(student.firstName);
      setMiddleName(student.middleName ?? "");
      setBirthDate(student.birthDate);
      setPhone(student.phone);
      setEmail(student.email);
      setManagerUserId(student.managerUserId);
      setSource(student.source ?? "");
    }
  }, [open, student]);

  const usersQuery = useQuery({
    queryKey: ["identity", "users", { pageSize: 100, sort: "userName asc" }],
    queryFn: () => searchUsers({ pageSize: 100, sort: "userName asc" }),
    enabled: open,
    staleTime: 60_000,
  });
  const managerOptions = useMemo(() => {
    const opts = (usersQuery.data?.items ?? [])
      .filter((u): u is UserDto & { id: string } => Boolean(u.id))
      .map((u) => ({
        value: u.id,
        label: [u.firstName, u.lastName].filter(Boolean).join(" ") || u.userName || u.email || u.id,
      }));
    // Keep the current manager selectable even if outside the first 100 users.
    if (managerUserId && !opts.some((o) => o.value === managerUserId)) {
      opts.unshift({ value: managerUserId, label: managerUserId });
    }
    return opts;
  }, [usersQuery.data, managerUserId]);

  const mutation = useMutation({
    mutationFn: (input: UpdateStudentInput) => updateStudent(input),
    onSuccess: () => {
      toast.success("Изменения сохранены");
      onSaved();
      onClose();
    },
    onError: (e) => toast.error("Не удалось сохранить", { description: describe(e) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate({
      studentId: student.id,
      lastName: lastName.trim(),
      firstName: firstName.trim(),
      middleName: middleName.trim() || null,
      birthDate,
      phone: phone.trim(),
      email: email.trim(),
      managerUserId: managerUserId.trim(),
      source: source.trim() || null,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Изменить ученика</DialogTitle>
            <DialogDescription>Обновите профиль. Статус меняется отдельными действиями.</DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="es-last" label="Фамилия" required>
                <Input id="es-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
              </Field>
              <Field id="es-first" label="Имя" required>
                <Input id="es-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
              </Field>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="es-middle" label="Отчество">
                <Input id="es-middle" value={middleName} onChange={(e) => setMiddleName(e.target.value)} />
              </Field>
              <Field id="es-birth" label="Дата рождения" required>
                <Input id="es-birth" type="date" value={birthDate} onChange={(e) => setBirthDate(e.target.value)} required />
              </Field>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="es-phone" label="Телефон" required>
                <Input id="es-phone" value={phone} onChange={(e) => setPhone(e.target.value)} required />
              </Field>
              <Field id="es-email" label="E-mail" required>
                <Input id="es-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
              </Field>
            </div>
            <Field id="es-manager" label="Ответственный менеджер" required>
              <Combobox
                label="Ответственный менеджер"
                value={managerUserId || null}
                onChange={(v) => setManagerUserId(v ?? "")}
                options={managerOptions}
                placeholder={usersQuery.isLoading ? "Загрузка…" : "Выберите менеджера"}
                searchable
              />
            </Field>
            <Field id="es-source" label="Источник">
              <Input id="es-source" value={source} onChange={(e) => setSource(e.target.value)} />
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

export function LinkUserDialog({
  open,
  onClose,
  onSubmit,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  onSubmit: (userId: string) => Promise<unknown>;
  onSaved: () => void;
}) {
  const [userId, setUserId] = useState("");

  useEffect(() => {
    if (!open) setUserId("");
  }, [open]);

  const mutation = useMutation({
    mutationFn: (id: string) => onSubmit(id),
    onSuccess: () => {
      toast.success("Учётная запись привязана");
      onSaved();
      onClose();
    },
    onError: (e) => toast.error("Не удалось привязать", { description: describe(e) }),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            const trimmed = userId.trim();
            if (trimmed) mutation.mutate(trimmed);
          }}
        >
          <DialogHeader>
            <DialogTitle>Привязать учётную запись</DialogTitle>
            <DialogDescription>
              Укажите User ID существующей учётной записи из модуля Identity.
            </DialogDescription>
          </DialogHeader>
          <DialogBody>
            <Field id="link-user-id" label="User ID" required>
              <Input id="link-user-id" value={userId} onChange={(e) => setUserId(e.target.value)} required autoFocus />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !userId.trim()}>
              {mutation.isPending ? "Привязка…" : "Привязать"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export function ConfirmDeleteDialog({
  open,
  onClose,
  name,
  onConfirm,
}: {
  open: boolean;
  onClose: () => void;
  name: string;
  onConfirm: () => Promise<void>;
}) {
  const mutation = useMutation({
    mutationFn: () => onConfirm(),
    onSuccess: () => onClose(),
    onError: (e) => toast.error("Не удалось удалить", { description: describe(e) }),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <DialogHeader>
          <DialogTitle>Удалить «{name}»?</DialogTitle>
          <DialogDescription>
            Запись переместится в корзину. Это действие можно отменить восстановлением из корзины.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={mutation.isPending}>
              Отмена
            </Button>
          </DialogClose>
          <Button
            type="button"
            variant="destructive"
            disabled={mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? "Удаление…" : "Удалить"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
