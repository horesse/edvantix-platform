import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeftRight,
  BookOpen,
  CalendarClock,
  CalendarDays,
  CheckCircle2,
  ClipboardCheck,
  Pause,
  Pencil,
  Play,
  Plus,
  RefreshCw,
  Rocket,
  Trash2,
  UserMinus,
  UserPlus,
  Users,
  UsersRound,
  X,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";
import {
  activateStudyGroup,
  addGroupTeacher,
  cancelStudyGroup,
  deleteStudyGroup,
  enrollStudents,
  finishStudyGroup,
  getStudyGroupById,
  GROUP_FORMATS,
  pauseEnrollment,
  removeGroupTeacher,
  resumeEnrollment,
  searchStudyGroups,
  TEACHER_ROLES,
  transferEnrollment,
  unenrollStudent,
  updateStudyGroup,
  type GroupEnrollmentDto,
  type GroupFormat,
  type StudyGroupDetailDto,
  type TeacherRole,
  type UpdateStudyGroupInput,
} from "@/api/study-groups";
import { searchCourses } from "@/api/curriculum";
import { searchStudents, searchTeachers } from "@/api/people";
import {
  getGroupAttendanceReport,
  type StudentAttendanceSummaryDto,
} from "@/api/scheduling";
import { useAuth } from "@/auth/use-auth";
import { ApiRequestError } from "@/lib/api-client";
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
  ErrorBand,
  Field,
  type ComboboxOption,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe, formatDate } from "@/lib/list-helpers";
import {
  ENROLLMENT_STATUS_LABEL,
  ENROLLMENT_STATUS_TONE,
  FORMAT_LABEL,
  isFrozen,
  STATUS_LABEL,
  STATUS_TONE,
  TEACHER_ROLE_LABEL,
} from "./study-groups-ui";

const TEXTAREA_CLS = cn(
  "flex w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm",
  "placeholder:text-[var(--color-muted-foreground)]",
  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
);

const ACTIVE_STATUSES = new Set(["Active", "Paused"]);

function short(id: string) {
  return id.slice(0, 8);
}

export function StudyGroupBuilderPage() {
  const { studyGroupId = "" } = useParams<{ studyGroupId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const perms = useAuth().user?.permissions ?? [];
  const canUpdate = perms.includes("Permissions.StudyGroups.StudyGroups.Update");
  const canArchive = perms.includes("Permissions.StudyGroups.StudyGroups.Archive");
  const canDelete = perms.includes("Permissions.StudyGroups.StudyGroups.Delete");
  const canEnrollView = perms.includes("Permissions.StudyGroups.Enrollments.View");
  const canEnrollCreate = perms.includes("Permissions.StudyGroups.Enrollments.Create");
  const canEnrollDelete = perms.includes("Permissions.StudyGroups.Enrollments.Delete");
  const canEnrollTransfer = perms.includes("Permissions.StudyGroups.Enrollments.Transfer");
  const canViewTemplates = perms.includes(
    "Permissions.Scheduling.ScheduleTemplates.View",
  );
  const canViewAttendance = perms.includes("Permissions.Scheduling.Attendance.View");

  const groupKey = ["study-group", studyGroupId] as const;
  const query = useQuery({
    queryKey: groupKey,
    queryFn: () => getStudyGroupById(studyGroupId),
    enabled: !!studyGroupId,
  });
  const group = query.data;

  const invalidateGroup = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: groupKey });
    void queryClient.invalidateQueries({ queryKey: ["study-groups"] });
  }, [queryClient, studyGroupId]); // eslint-disable-line react-hooks/exhaustive-deps

  // Name lookups for the roster / enrollment tables.
  const studentsQuery = useQuery({
    queryKey: ["students", { pageSize: 200, for: "study-group" }],
    queryFn: () => searchStudents({ pageSize: 200 }),
    staleTime: 60_000,
  });
  const teachersQuery = useQuery({
    queryKey: ["teachers", { pageSize: 200, for: "study-group" }],
    queryFn: () => searchTeachers({ pageSize: 200 }),
    staleTime: 60_000,
  });
  const coursesQuery = useQuery({
    queryKey: ["courses", { pageSize: 100, for: "study-group" }],
    queryFn: () => searchCourses({ pageSize: 100 }),
    staleTime: 60_000,
  });

  const studentName = useMemo(() => {
    const m = new Map<string, string>();
    for (const s of studentsQuery.data?.items ?? []) m.set(s.id, s.displayName);
    return m;
  }, [studentsQuery.data]);
  const teacherName = useMemo(() => {
    const m = new Map<string, string>();
    for (const t of teachersQuery.data?.items ?? []) m.set(t.id, t.displayName);
    return m;
  }, [teachersQuery.data]);
  const courseName = useMemo(() => {
    const m = new Map<string, string>();
    for (const c of coursesQuery.data?.items ?? []) m.set(c.id, c.title);
    return m;
  }, [coursesQuery.data]);
  const teacherOptions = useMemo(
    () =>
      (teachersQuery.data?.items ?? []).map((t) => ({ value: t.id, label: t.displayName })),
    [teachersQuery.data],
  );

  const [editOpen, setEditOpen] = useState(false);
  const [cancelOpen, setCancelOpen] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [addTeacherOpen, setAddTeacherOpen] = useState(false);
  const [enrollOpen, setEnrollOpen] = useState(false);
  const [transferTarget, setTransferTarget] = useState<GroupEnrollmentDto | null>(null);
  const [unenrollTarget, setUnenrollTarget] = useState<GroupEnrollmentDto | null>(null);
  const [lifecycleError, setLifecycleError] = useState<string | null>(null);

  const activateMutation = useMutation({
    mutationFn: () => activateStudyGroup(studyGroupId),
    onSuccess: () => {
      toast.success("Группа активирована");
      setLifecycleError(null);
      invalidateGroup();
    },
    onError: (err) => {
      const msg =
        err instanceof ApiRequestError && err.status === 409
          ? err.problem?.detail ??
            "В группе нет ни одного зачисления — активировать нельзя."
          : describe(err);
      setLifecycleError(msg);
      toast.error("Не удалось активировать группу", { description: msg });
    },
  });

  const finishMutation = useMutation({
    mutationFn: () => finishStudyGroup(studyGroupId),
    onSuccess: () => {
      toast.success("Группа завершена");
      setLifecycleError(null);
      invalidateGroup();
    },
    onError: (err) =>
      toast.error("Не удалось завершить группу", { description: describe(err) }),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteStudyGroup(studyGroupId),
    onSuccess: () => {
      toast.success("Группа удалена");
      void queryClient.invalidateQueries({ queryKey: ["study-groups"] });
      navigate("/study-groups");
    },
    onError: (err) =>
      toast.error("Не удалось удалить группу", { description: describe(err) }),
  });

  return (
    <div className="pb-12">
      <EntityDetailBack to="/study-groups" label="К списку групп" />

      {query.isError && (
        <div className="mb-5">
          <ErrorBand message={describe(query.error)} />
        </div>
      )}

      {query.isLoading ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка группы…</p>
      ) : group ? (
        <>
          {(() => {
            const frozen = isFrozen(group.status);
            return (
              <>
                <EntityDetailHero
                  avatar={<EntityDetailAvatar name={group.name} icon={UsersRound} />}
                  title={group.name}
                  badges={
                    <EntityStatusBadge tone={STATUS_TONE[group.status]}>
                      {STATUS_LABEL[group.status]}
                    </EntityStatusBadge>
                  }
                  subtitle={<span className="font-mono text-[11px]">{group.code}</span>}
                  actions={
                    <>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => void query.refetch()}
                        disabled={query.isFetching}
                        className="gap-1.5"
                      >
                        <RefreshCw
                          className={cn("h-3.5 w-3.5", query.isFetching && "animate-spin")}
                        />
                        <span className="hidden sm:inline">Обновить</span>
                      </Button>
                      {canViewTemplates && (
                        <Button
                          variant="outline"
                          size="sm"
                          asChild
                          className="gap-1.5"
                        >
                          <Link to={`/study-groups/${studyGroupId}/schedule`}>
                            <CalendarClock className="h-3.5 w-3.5" />
                            <span className="hidden sm:inline">Расписание</span>
                          </Link>
                        </Button>
                      )}
                      {canUpdate && !frozen && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setEditOpen(true)}
                          className="gap-1.5"
                        >
                          <Pencil className="h-3.5 w-3.5" />
                          <span className="hidden sm:inline">Изменить</span>
                        </Button>
                      )}
                      {canArchive && group.status === "Forming" && (
                        <Button
                          size="sm"
                          onClick={() => activateMutation.mutate()}
                          disabled={activateMutation.isPending}
                          className="gap-1.5"
                        >
                          <Rocket className="h-3.5 w-3.5" />
                          Активировать
                        </Button>
                      )}
                      {canArchive && group.status === "Active" && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => finishMutation.mutate()}
                          disabled={finishMutation.isPending}
                          className="gap-1.5"
                        >
                          <CheckCircle2 className="h-3.5 w-3.5" />
                          Завершить
                        </Button>
                      )}
                      {canArchive && !frozen && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setCancelOpen(true)}
                          className="gap-1.5 hover:!border-[var(--color-destructive)] hover:!text-[var(--color-destructive)]"
                        >
                          <XCircle className="h-3.5 w-3.5" />
                          <span className="hidden sm:inline">Отменить</span>
                        </Button>
                      )}
                      {canDelete && (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setConfirmDelete(true)}
                          className="gap-1.5 text-[var(--color-destructive)]"
                          aria-label="Удалить группу"
                          title="Удалить группу"
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      )}
                    </>
                  }
                  stats={
                    <>
                      <EntityDetailStat
                        icon={Users}
                        value={`${group.activeEnrollmentCount}/${group.capacity}`}
                        label="состав"
                      />
                      <EntityDetailStat
                        icon={UsersRound}
                        value={FORMAT_LABEL[group.format]}
                        label="формат"
                      />
                      <EntityDetailStat
                        icon={CalendarDays}
                        value={formatDate(group.startDate)}
                        label="старт"
                      />
                      {group.endDate && (
                        <EntityDetailStat
                          icon={CalendarDays}
                          value={formatDate(group.endDate)}
                          label="финиш"
                        />
                      )}
                    </>
                  }
                  meta={
                    <>
                      {courseName.get(group.courseId) && (
                        <EntityDetailMeta icon={BookOpen}>
                          <Link
                            to={`/courses/${group.courseId}`}
                            className="hover:text-[var(--color-foreground)]"
                          >
                            {courseName.get(group.courseId)}
                          </Link>
                        </EntityDetailMeta>
                      )}
                      <EntityDetailMeta icon={Users}>
                        Основной преподаватель:{" "}
                        {teacherName.get(group.primaryTeacherId) ??
                          short(group.primaryTeacherId)}
                      </EntityDetailMeta>
                      {group.meetingUrl && (
                        <EntityDetailMeta icon={UsersRound} hideOnMobile>
                          <a
                            href={group.meetingUrl}
                            target="_blank"
                            rel="noreferrer"
                            className="hover:text-[var(--color-foreground)]"
                          >
                            Ссылка на встречу
                          </a>
                        </EntityDetailMeta>
                      )}
                    </>
                  }
                />

                {lifecycleError && (
                  <div className="mb-5">
                    <ErrorBand message={lifecycleError} />
                  </div>
                )}

                {frozen && (
                  <div className="mb-5 rounded-lg border border-[var(--color-border)] bg-[oklch(from_var(--color-muted)_l_c_h_/_0.3)] px-3 py-2 text-[12px] text-[var(--color-muted-foreground)]">
                    Группа {group.status === "Finished" ? "завершена" : "отменена"} —
                    карточка и состав доступны только для чтения.
                  </div>
                )}

                {group.notes && (
                  <EntityDetailSection title="Заметки" icon={Pencil}>
                    <p className="whitespace-pre-wrap text-[13px] text-[var(--color-foreground)]">
                      {group.notes}
                    </p>
                  </EntityDetailSection>
                )}

                <div className="mt-4 space-y-4">
                  <TeachersSection
                    group={group}
                    teacherName={teacherName}
                    teacherOptions={teacherOptions}
                    canManage={canUpdate && !frozen}
                    addOpen={addTeacherOpen}
                    setAddOpen={setAddTeacherOpen}
                    onChanged={invalidateGroup}
                  />

                  {canEnrollView && (
                    <EnrollmentsSection
                      group={group}
                      frozen={frozen}
                      studentName={studentName}
                      canCreate={canEnrollCreate}
                      canDelete={canEnrollDelete}
                      canTransfer={canEnrollTransfer}
                      onEnroll={() => setEnrollOpen(true)}
                      onTransfer={setTransferTarget}
                      onUnenroll={setUnenrollTarget}
                      onChanged={invalidateGroup}
                    />
                  )}

                  {canViewAttendance && (
                    <AttendanceReportSection
                      studyGroupId={group.id}
                      studentName={studentName}
                    />
                  )}
                </div>

                <StudyGroupEditDialog
                  open={editOpen}
                  group={group}
                  teacherOptions={teacherOptions}
                  onClose={() => setEditOpen(false)}
                  onSaved={invalidateGroup}
                />

                <CancelGroupDialog
                  open={cancelOpen}
                  studyGroupId={studyGroupId}
                  onClose={() => setCancelOpen(false)}
                  onDone={invalidateGroup}
                />

                <Dialog
                  open={confirmDelete}
                  onOpenChange={(o) => !o && setConfirmDelete(false)}
                >
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle>Удалить группу?</DialogTitle>
                      <DialogDescription>
                        Группа «{group.name}» ({group.code}) будет удалена вместе с
                        составом. Действие необратимо.
                      </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                      <DialogClose asChild>
                        <Button
                          type="button"
                          variant="outline"
                          disabled={deleteMutation.isPending}
                        >
                          Отмена
                        </Button>
                      </DialogClose>
                      <Button
                        variant="destructive"
                        onClick={() => deleteMutation.mutate()}
                        disabled={deleteMutation.isPending}
                      >
                        {deleteMutation.isPending ? "Удаление…" : "Удалить"}
                      </Button>
                    </DialogFooter>
                  </DialogContent>
                </Dialog>

                <EnrollDialog
                  open={enrollOpen}
                  group={group}
                  onClose={() => setEnrollOpen(false)}
                  onDone={invalidateGroup}
                />

                {transferTarget && (
                  <TransferDialog
                    enrollment={transferTarget}
                    currentGroupId={group.id}
                    onClose={() => setTransferTarget(null)}
                    onDone={invalidateGroup}
                  />
                )}

                {unenrollTarget && (
                  <UnenrollDialog
                    enrollment={unenrollTarget}
                    studyGroupId={group.id}
                    onClose={() => setUnenrollTarget(null)}
                    onDone={invalidateGroup}
                  />
                )}
              </>
            );
          })()}
        </>
      ) : (
        <p className="text-sm text-[var(--color-muted-foreground)]">Группа не найдена.</p>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Teacher roster
// ───────────────────────────────────────────────────────────────────────

function TeachersSection({
  group,
  teacherName,
  teacherOptions,
  canManage,
  addOpen,
  setAddOpen,
  onChanged,
}: {
  group: StudyGroupDetailDto;
  teacherName: Map<string, string>;
  teacherOptions: ComboboxOption[];
  canManage: boolean;
  addOpen: boolean;
  setAddOpen: (v: boolean) => void;
  onChanged: () => void;
}) {
  const roster = [...group.teachers].sort((a, b) => a.role.localeCompare(b.role));

  const removeMutation = useMutation({
    mutationFn: (teacherId: string) => removeGroupTeacher(group.id, teacherId),
    onSuccess: () => {
      toast.success("Преподаватель убран из ростера");
      onChanged();
    },
    onError: (err) =>
      toast.error("Не удалось убрать преподавателя", { description: describe(err) }),
  });

  return (
    <EntityDetailSection
      title="Ростер преподавателей"
      icon={Users}
      description="Роли ростера не обязаны совпадать с основным преподавателем группы."
      action={
        canManage ? (
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5"
            onClick={() => setAddOpen(true)}
          >
            <Plus className="size-3.5" />
            Добавить
          </Button>
        ) : undefined
      }
    >
      <div className="mb-3 rounded-lg bg-[oklch(from_var(--color-primary)_l_c_h_/_0.06)] px-3 py-2 text-[12px] text-[var(--color-foreground)]">
        Основной преподаватель группы:{" "}
        <span className="font-medium">
          {teacherName.get(group.primaryTeacherId) ?? short(group.primaryTeacherId)}
        </span>
      </div>

      {roster.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          В ростере пока нет преподавателей.
        </p>
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
          {roster.map((t) => (
            <li
              key={t.id}
              className="flex items-center justify-between gap-3 py-3 first:pt-0 last:pb-0"
            >
              <div className="flex min-w-0 items-center gap-3">
                <EntityInitialsAvatar
                  name={teacherName.get(t.teacherId) ?? short(t.teacherId)}
                  size={36}
                />
                <div className="min-w-0">
                  <p className="truncate text-[13px] font-medium text-[var(--color-foreground)]">
                    {teacherName.get(t.teacherId) ?? short(t.teacherId)}
                  </p>
                  <EntityStatusBadge tone="info">
                    {TEACHER_ROLE_LABEL[t.role]}
                  </EntityStatusBadge>
                </div>
              </div>
              {canManage && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="shrink-0 text-[var(--color-destructive)]"
                  disabled={removeMutation.isPending}
                  onClick={() => removeMutation.mutate(t.teacherId)}
                  title="Убрать из ростера"
                >
                  <X className="size-3.5" />
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}

      <AddTeacherDialog
        open={addOpen}
        studyGroupId={group.id}
        teacherOptions={teacherOptions}
        existingTeacherIds={group.teachers.map((t) => t.teacherId)}
        onClose={() => setAddOpen(false)}
        onDone={onChanged}
      />
    </EntityDetailSection>
  );
}

function AddTeacherDialog({
  open,
  studyGroupId,
  teacherOptions,
  existingTeacherIds,
  onClose,
  onDone,
}: {
  open: boolean;
  studyGroupId: string;
  teacherOptions: ComboboxOption[];
  existingTeacherIds: string[];
  onClose: () => void;
  onDone: () => void;
}) {
  const [teacherId, setTeacherId] = useState<string | null>(null);
  const [role, setRole] = useState<TeacherRole>("Assistant");

  useEffect(() => {
    if (!open) {
      setTeacherId(null);
      setRole("Assistant");
    }
  }, [open]);

  const options = useMemo(
    () => teacherOptions.filter((o) => !existingTeacherIds.includes(o.value)),
    [teacherOptions, existingTeacherIds],
  );

  const mutation = useMutation({
    mutationFn: (vars: { teacherId: string; role: TeacherRole }) =>
      addGroupTeacher({ studyGroupId, ...vars }),
    onSuccess: () => {
      toast.success("Преподаватель добавлен в ростер");
      onDone();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось добавить преподавателя", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!teacherId) return;
    mutation.mutate({ teacherId, role });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Добавить преподавателя</DialogTitle>
            <DialogDescription>
              Выберите преподавателя и его роль в этой группе.
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <Field id="at-teacher" label="Преподаватель" required>
              <Combobox
                id="at-teacher"
                label="Преподаватель"
                value={teacherId}
                onChange={setTeacherId}
                options={options}
                searchable
                clearable
              />
            </Field>
            <Field id="at-role" label="Роль" required>
              <Combobox
                id="at-role"
                label="Роль"
                value={role}
                onChange={(v) => setRole((v as TeacherRole) ?? "Assistant")}
                options={TEACHER_ROLES.map((r) => ({
                  value: r,
                  label: TEACHER_ROLE_LABEL[r],
                }))}
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !teacherId}>
              {mutation.isPending ? "Добавление…" : "Добавить"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Enrollments roster
// ───────────────────────────────────────────────────────────────────────

function EnrollmentsSection({
  group,
  frozen,
  studentName,
  canCreate,
  canDelete,
  canTransfer,
  onEnroll,
  onTransfer,
  onUnenroll,
  onChanged,
}: {
  group: StudyGroupDetailDto;
  frozen: boolean;
  studentName: Map<string, string>;
  canCreate: boolean;
  canDelete: boolean;
  canTransfer: boolean;
  onEnroll: () => void;
  onTransfer: (e: GroupEnrollmentDto) => void;
  onUnenroll: (e: GroupEnrollmentDto) => void;
  onChanged: () => void;
}) {
  const [hideLeft, setHideLeft] = useState(false);

  const rows = useMemo(() => {
    const list = [...group.enrollments];
    list.sort((a, b) => {
      // Active/paused first, then by student name.
      const aActive = ACTIVE_STATUSES.has(a.status) ? 0 : 1;
      const bActive = ACTIVE_STATUSES.has(b.status) ? 0 : 1;
      if (aActive !== bActive) return aActive - bActive;
      return (studentName.get(a.studentId) ?? a.studentId).localeCompare(
        studentName.get(b.studentId) ?? b.studentId,
      );
    });
    return hideLeft
      ? list.filter((e) => e.status !== "Left" && e.status !== "Completed")
      : list;
  }, [group.enrollments, studentName, hideLeft]);

  const pauseMutation = useMutation({
    mutationFn: (enrollmentId: string) => pauseEnrollment(enrollmentId),
    onSuccess: () => {
      toast.success("Зачисление приостановлено");
      onChanged();
    },
    onError: (err) => toast.error("Не удалось приостановить", { description: describe(err) }),
  });
  const resumeMutation = useMutation({
    mutationFn: (enrollmentId: string) => resumeEnrollment(enrollmentId),
    onSuccess: () => {
      toast.success("Зачисление возобновлено");
      onChanged();
    },
    onError: (err) => toast.error("Не удалось возобновить", { description: describe(err) }),
  });

  const leftCount = group.enrollments.filter(
    (e) => e.status === "Left" || e.status === "Completed",
  ).length;

  return (
    <EntityDetailSection
      title="Состав группы"
      icon={Users}
      description={`${group.activeEnrollmentCount} из ${group.capacity} мест занято`}
      action={
        canCreate && !frozen ? (
          <Button variant="outline" size="sm" className="gap-1.5" onClick={onEnroll}>
            <UserPlus className="size-3.5" />
            Зачислить
          </Button>
        ) : undefined
      }
      footer={
        leftCount > 0 ? (
          <label className="flex cursor-pointer items-center gap-2 text-[12px] text-[var(--color-muted-foreground)]">
            <input
              type="checkbox"
              checked={hideLeft}
              onChange={(e) => setHideLeft(e.target.checked)}
            />
            Скрыть ушедших и завершивших ({leftCount})
          </label>
        ) : undefined
      }
    >
      {rows.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          {group.enrollments.length === 0
            ? "В группе пока нет учеников. Зачислите первого, чтобы можно было активировать группу."
            : "Нет строк, удовлетворяющих фильтру."}
        </p>
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
          {rows.map((e) => {
            const name = studentName.get(e.studentId) ?? short(e.studentId);
            const isActive = ACTIVE_STATUSES.has(e.status);
            return (
              <li
                key={e.id}
                className="flex items-center justify-between gap-3 py-3 first:pt-0 last:pb-0"
              >
                <div className="flex min-w-0 items-center gap-3">
                  <EntityInitialsAvatar name={name} size={36} />
                  <div className="min-w-0">
                    <p className="truncate text-[13px] font-medium text-[var(--color-foreground)]">
                      {name}
                      {e.discountPercent > 0 && (
                        <span className="ml-2 text-[11px] text-[var(--color-muted-foreground)]">
                          скидка {e.discountPercent}%
                        </span>
                      )}
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

                {!frozen && isActive && (
                  <div className="flex shrink-0 items-center gap-0.5">
                    {canCreate && e.status === "Active" && (
                      <IconAction
                        label={`Приостановить ${name}`}
                        onClick={() => pauseMutation.mutate(e.id)}
                        disabled={pauseMutation.isPending}
                      >
                        <Pause className="size-3.5" />
                      </IconAction>
                    )}
                    {canCreate && e.status === "Paused" && (
                      <IconAction
                        label={`Возобновить ${name}`}
                        onClick={() => resumeMutation.mutate(e.id)}
                        disabled={resumeMutation.isPending}
                      >
                        <Play className="size-3.5" />
                      </IconAction>
                    )}
                    {canTransfer && (
                      <IconAction
                        label={`Перевести ${name}`}
                        onClick={() => onTransfer(e)}
                      >
                        <ArrowLeftRight className="size-3.5" />
                      </IconAction>
                    )}
                    {canDelete && (
                      <IconAction
                        label={`Отчислить ${name}`}
                        onClick={() => onUnenroll(e)}
                        danger
                      >
                        <UserMinus className="size-3.5" />
                      </IconAction>
                    )}
                  </div>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </EntityDetailSection>
  );
}

function IconAction({
  label,
  onClick,
  disabled,
  danger,
  children,
}: {
  label: string;
  onClick: () => void;
  disabled?: boolean;
  danger?: boolean;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      aria-label={label}
      title={label}
      className={cn(
        "grid size-7 place-items-center rounded-md text-[var(--color-muted-foreground)] transition-colors",
        "hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]",
        "disabled:cursor-not-allowed disabled:opacity-30",
        danger && "hover:text-[var(--color-destructive)]",
      )}
    >
      {children}
    </button>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Enroll dialog — multi-select students (rule 9: the studentIds list is
//  built here and passed through mutate(arg)).
// ───────────────────────────────────────────────────────────────────────

function EnrollDialog({
  open,
  group,
  onClose,
  onDone,
}: {
  open: boolean;
  group: StudyGroupDetailDto;
  onClose: () => void;
  onDone: () => void;
}) {
  const [selected, setSelected] = useState<string[]>([]);
  const [picker, setPicker] = useState<string | null>(null);
  const [discount, setDiscount] = useState("0");
  const [enrollError, setEnrollError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setSelected([]);
      setPicker(null);
      setDiscount("0");
      setEnrollError(null);
    }
  }, [open]);

  const studentsQuery = useQuery({
    queryKey: ["students", { pageSize: 200, for: "enroll" }],
    queryFn: () => searchStudents({ pageSize: 200 }),
    enabled: open,
    staleTime: 60_000,
  });

  const alreadyIn = useMemo(
    () =>
      new Set(
        group.enrollments
          .filter((e) => e.status === "Active" || e.status === "Paused")
          .map((e) => e.studentId),
      ),
    [group.enrollments],
  );

  const nameById = useMemo(() => {
    const m = new Map<string, string>();
    for (const s of studentsQuery.data?.items ?? []) m.set(s.id, s.displayName);
    return m;
  }, [studentsQuery.data]);

  const options = useMemo(
    () =>
      (studentsQuery.data?.items ?? [])
        .filter((s) => !alreadyIn.has(s.id) && !selected.includes(s.id))
        .map((s) => ({ value: s.id, label: s.displayName })),
    [studentsQuery.data, alreadyIn, selected],
  );

  const mutation = useMutation({
    mutationFn: (vars: { studentIds: string[]; discountPercent: number }) =>
      enrollStudents({ studyGroupId: group.id, ...vars }),
    onSuccess: (ids) => {
      toast.success(`Зачислено учеников: ${ids.length}`);
      onDone();
      onClose();
    },
    onError: (err) => {
      const msg =
        err instanceof ApiRequestError && err.status === 409
          ? err.problem?.detail ?? "Мест нет — вместимость группы исчерпана."
          : describe(err);
      setEnrollError(msg);
      toast.error("Не удалось зачислить", { description: msg });
      // The server may enroll part of the batch before hitting capacity — refresh
      // the roster behind the still-open dialog so it reflects any partial change.
      onDone();
    },
  });

  const pct = Number.parseFloat(discount);
  const validPct = !Number.isNaN(pct) && pct >= 0 && pct <= 100;
  const freeSeats = group.capacity - group.activeEnrollmentCount;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setEnrollError(null);
    if (selected.length === 0 || !validPct) return;
    mutation.mutate({ studentIds: selected, discountPercent: pct });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Зачислить учеников</DialogTitle>
            <DialogDescription>
              Свободно мест: {freeSeats > 0 ? freeSeats : 0}. Можно выбрать
              несколько учеников — весь набор зачисляется одним запросом.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="en-student" label="Ученик">
              <Combobox
                id="en-student"
                label="Ученик"
                value={picker}
                onChange={(v) => {
                  if (v) {
                    setSelected((prev) => (prev.includes(v) ? prev : [...prev, v]));
                    setPicker(null);
                  }
                }}
                options={options}
                placeholder={
                  studentsQuery.isLoading ? "Загрузка…" : "Добавить ученика в набор"
                }
                searchable
                clearable
              />
            </Field>

            {selected.length > 0 && (
              <ul className="flex flex-wrap gap-2">
                {selected.map((id) => (
                  <li
                    key={id}
                    className="inline-flex items-center gap-1.5 rounded-full bg-[var(--color-muted)] px-2.5 py-1 text-[12px] text-[var(--color-foreground)]"
                  >
                    {nameById.get(id) ?? short(id)}
                    <button
                      type="button"
                      aria-label={`Убрать ${nameById.get(id) ?? short(id)}`}
                      onClick={() =>
                        setSelected((prev) => prev.filter((x) => x !== id))
                      }
                      className="text-[var(--color-muted-foreground)] hover:text-[var(--color-destructive)]"
                    >
                      <X className="size-3" />
                    </button>
                  </li>
                ))}
              </ul>
            )}

            <Field
              id="en-discount"
              label="Скидка, %"
              hint="Общая для всего набора. Тариф назначается после подключения Payments."
            >
              <Input
                id="en-discount"
                type="number"
                min="0"
                max="100"
                step="0.5"
                value={discount}
                onChange={(e) => setDiscount(e.target.value)}
                className="tabular-nums"
              />
            </Field>

            {enrollError && <ErrorBand message={enrollError} />}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || selected.length === 0 || !validPct}
              className="gap-1.5"
            >
              <UserPlus className="h-4 w-4" />
              {mutation.isPending ? "Зачисление…" : `Зачислить (${selected.length})`}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Unenroll dialog — reason only (the endpoint binds no date).
// ───────────────────────────────────────────────────────────────────────

function UnenrollDialog({
  enrollment,
  studyGroupId,
  onClose,
  onDone,
}: {
  enrollment: GroupEnrollmentDto;
  studyGroupId: string;
  onClose: () => void;
  onDone: () => void;
}) {
  const [reason, setReason] = useState("");

  const mutation = useMutation({
    mutationFn: (vars: { enrollmentId: string; reason: string }) =>
      unenrollStudent({ studyGroupId, ...vars }),
    onSuccess: () => {
      toast.success("Ученик отчислен — зачисление помечено «Ушёл»");
      onDone();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось отчислить", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate({ enrollmentId: enrollment.id, reason: reason.trim() });
  };

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Отчислить ученика</DialogTitle>
            <DialogDescription>
              Строка состава не удаляется — зачисление перейдёт в статус «Ушёл» и
              останется в истории.
            </DialogDescription>
          </DialogHeader>
          <DialogBody>
            <Field id="un-reason" label="Причина" hint="Необязательно, но полезно для истории">
              <textarea
                id="un-reason"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                rows={3}
                className={TEXTAREA_CLS}
                autoFocus
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" variant="destructive" disabled={mutation.isPending}>
              {mutation.isPending ? "Отчисление…" : "Отчислить"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Transfer dialog
// ───────────────────────────────────────────────────────────────────────

function TransferDialog({
  enrollment,
  currentGroupId,
  onClose,
  onDone,
}: {
  enrollment: GroupEnrollmentDto;
  currentGroupId: string;
  onClose: () => void;
  onDone: () => void;
}) {
  const [targetGroupId, setTargetGroupId] = useState<string | null>(null);
  const [transferDate, setTransferDate] = useState("");

  const groupsQuery = useQuery({
    queryKey: ["study-groups", { pageSize: 100, for: "transfer" }],
    queryFn: () => searchStudyGroups({ pageSize: 100 }),
    staleTime: 30_000,
  });

  const options = useMemo(
    () =>
      (groupsQuery.data?.items ?? [])
        .filter(
          (g) =>
            g.id !== currentGroupId &&
            g.status !== "Finished" &&
            g.status !== "Cancelled",
        )
        .map((g) => ({ value: g.id, label: `${g.code} — ${g.name}` })),
    [groupsQuery.data, currentGroupId],
  );

  const mutation = useMutation({
    mutationFn: (vars: { targetStudyGroupId: string; transferDate: string | null }) =>
      transferEnrollment({ enrollmentId: enrollment.id, ...vars }),
    onSuccess: () => {
      toast.success("Ученик переведён в другую группу");
      onDone();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось перевести", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!targetGroupId) return;
    mutation.mutate({
      targetStudyGroupId: targetGroupId,
      transferDate: transferDate || null,
    });
  };

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Перевести в другую группу</DialogTitle>
            <DialogDescription>
              Текущее зачисление закроется со статусом «Ушёл» (причина «Перевод»), в
              целевой группе откроется новое. Тариф и скидка переносятся.
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <Field id="tr-target" label="Целевая группа" required>
              <Combobox
                id="tr-target"
                label="Целевая группа"
                value={targetGroupId}
                onChange={setTargetGroupId}
                options={options}
                placeholder={
                  groupsQuery.isLoading ? "Загрузка…" : "Выберите группу"
                }
                searchable
              />
            </Field>
            <Field id="tr-date" label="Дата перевода">
              <Input
                id="tr-date"
                type="date"
                value={transferDate}
                onChange={(e) => setTransferDate(e.target.value)}
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || !targetGroupId}
              className="gap-1.5"
            >
              <ArrowLeftRight className="h-4 w-4" />
              {mutation.isPending ? "Перевод…" : "Перевести"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Cancel dialog
// ───────────────────────────────────────────────────────────────────────

function CancelGroupDialog({
  open,
  studyGroupId,
  onClose,
  onDone,
}: {
  open: boolean;
  studyGroupId: string;
  onClose: () => void;
  onDone: () => void;
}) {
  const [reason, setReason] = useState("");

  useEffect(() => {
    if (!open) setReason("");
  }, [open]);

  const mutation = useMutation({
    mutationFn: (r: string) => cancelStudyGroup(studyGroupId, r),
    onSuccess: () => {
      toast.success("Группа отменена");
      onDone();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось отменить группу", { description: describe(err) }),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            mutation.mutate(reason.trim());
          }}
        >
          <DialogHeader>
            <DialogTitle>Отменить группу?</DialogTitle>
            <DialogDescription>
              Группа перейдёт в статус «Отменена». После этого карточка и состав
              станут доступны только для чтения.
            </DialogDescription>
          </DialogHeader>
          <DialogBody>
            <Field id="cg-reason" label="Причина" hint="Необязательно">
              <textarea
                id="cg-reason"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                rows={3}
                className={TEXTAREA_CLS}
                autoFocus
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Не отменять
              </Button>
            </DialogClose>
            <Button type="submit" variant="destructive" disabled={mutation.isPending}>
              {mutation.isPending ? "Отмена…" : "Отменить группу"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Edit dialog — card fields (code is immutable, not shown)
// ───────────────────────────────────────────────────────────────────────

function StudyGroupEditDialog({
  open,
  group,
  teacherOptions,
  onClose,
  onSaved,
}: {
  open: boolean;
  group: StudyGroupDetailDto;
  teacherOptions: ComboboxOption[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const [name, setName] = useState(group.name);
  const [primaryTeacherId, setPrimaryTeacherId] = useState<string | null>(
    group.primaryTeacherId,
  );
  const [format, setFormat] = useState<GroupFormat>(group.format);
  const [capacity, setCapacity] = useState(String(group.capacity));
  const [startDate, setStartDate] = useState(group.startDate);
  const [endDate, setEndDate] = useState(group.endDate ?? "");
  const [meetingUrl, setMeetingUrl] = useState(group.meetingUrl ?? "");
  const [notes, setNotes] = useState(group.notes ?? "");

  useEffect(() => {
    if (open) {
      setName(group.name);
      setPrimaryTeacherId(group.primaryTeacherId);
      setFormat(group.format);
      setCapacity(String(group.capacity));
      setStartDate(group.startDate);
      setEndDate(group.endDate ?? "");
      setMeetingUrl(group.meetingUrl ?? "");
      setNotes(group.notes ?? "");
    }
  }, [open, group]);

  const teacherPickerOptions = useMemo(() => {
    // Keep the current primary teacher selectable even if outside the first page.
    if (
      primaryTeacherId &&
      !teacherOptions.some((o) => o.value === primaryTeacherId)
    ) {
      return [{ value: primaryTeacherId, label: short(primaryTeacherId) }, ...teacherOptions];
    }
    return teacherOptions;
  }, [teacherOptions, primaryTeacherId]);

  const mutation = useMutation({
    mutationFn: (input: UpdateStudyGroupInput) => updateStudyGroup(input),
    onSuccess: () => {
      toast.success("Группа обновлена");
      onSaved();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось обновить группу", { description: describe(err) }),
  });

  const cap = Number.parseInt(capacity, 10);
  const valid =
    name.trim().length > 0 &&
    !!primaryTeacherId &&
    !Number.isNaN(cap) &&
    cap > 0 &&
    startDate.length > 0;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid || !primaryTeacherId) return;
    mutation.mutate({
      studyGroupId: group.id,
      name: name.trim(),
      primaryTeacherId,
      format,
      capacity: cap,
      startDate,
      endDate: endDate || null,
      meetingUrl: meetingUrl.trim() || null,
      notes: notes.trim() || null,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Карточка группы</DialogTitle>
            <DialogDescription>
              Код группы ({group.code}) неизменяем и здесь не редактируется.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="ge-name" label="Название" required>
              <Input
                id="ge-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                autoFocus
              />
            </Field>
            <Field id="ge-teacher" label="Основной преподаватель" required>
              <Combobox
                id="ge-teacher"
                label="Основной преподаватель"
                value={primaryTeacherId}
                onChange={setPrimaryTeacherId}
                options={teacherPickerOptions}
                searchable
                required
              />
            </Field>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="ge-format" label="Формат" required>
                <Combobox
                  id="ge-format"
                  label="Формат"
                  value={format}
                  onChange={(v) => setFormat((v as GroupFormat) ?? group.format)}
                  options={GROUP_FORMATS.map((f) => ({
                    value: f,
                    label: FORMAT_LABEL[f],
                  }))}
                />
              </Field>
              <Field id="ge-capacity" label="Вместимость" required>
                <Input
                  id="ge-capacity"
                  type="number"
                  min="1"
                  value={capacity}
                  onChange={(e) => setCapacity(e.target.value)}
                  required
                  className="tabular-nums"
                />
              </Field>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="ge-start" label="Дата старта" required>
                <Input
                  id="ge-start"
                  type="date"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  required
                />
              </Field>
              <Field id="ge-end" label="Дата завершения">
                <Input
                  id="ge-end"
                  type="date"
                  value={endDate}
                  onChange={(e) => setEndDate(e.target.value)}
                />
              </Field>
            </div>
            <Field id="ge-url" label="Ссылка на встречу">
              <Input
                id="ge-url"
                type="url"
                value={meetingUrl}
                onChange={(e) => setMeetingUrl(e.target.value)}
                placeholder="https://…"
              />
            </Field>
            <Field id="ge-notes" label="Заметки">
              <textarea
                id="ge-notes"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={2}
                maxLength={2000}
                className={TEXTAREA_CLS}
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !valid}>
              {mutation.isPending ? "Сохранение…" : "Сохранить"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Attendance report — per-student Present/Absent/Late/Excused/Total over
//  a period, via GET /study-groups/{id}/attendance-report (Scheduling).
// ───────────────────────────────────────────────────────────────────────

function AttendanceReportSection({
  studyGroupId,
  studentName,
}: {
  studyGroupId: string;
  studentName: Map<string, string>;
}) {
  const [from, setFrom] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().slice(0, 10);
  });
  const [to, setTo] = useState(() => new Date().toISOString().slice(0, 10));

  const query = useQuery({
    queryKey: ["group-attendance-report", studyGroupId, { from, to }],
    queryFn: () => getGroupAttendanceReport(studyGroupId, from, to),
  });

  const rows: StudentAttendanceSummaryDto[] = useMemo(
    () =>
      [...(query.data?.students ?? [])].sort((a, b) =>
        (studentName.get(a.studentId) ?? a.studentId).localeCompare(
          studentName.get(b.studentId) ?? b.studentId,
        ),
      ),
    [query.data, studentName],
  );

  return (
    <EntityDetailSection
      title="Посещаемость"
      icon={ClipboardCheck}
      description="Сводка по каждому ученику за период."
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
      {query.isLoading ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : query.isError ? (
        <p className="text-[13px] text-[var(--color-destructive)]">
          {describe(query.error)}
        </p>
      ) : rows.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          За выбранный период нет данных о посещаемости.
        </p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[520px] text-[12.5px]">
            <thead>
              <tr className="border-b border-[var(--color-border)] text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                <th className="py-2 pr-3 text-left">Ученик</th>
                <th className="px-2 py-2 text-right">Был</th>
                <th className="px-2 py-2 text-right">Не был</th>
                <th className="px-2 py-2 text-right">Опоздал</th>
                <th className="px-2 py-2 text-right">Уваж.</th>
                <th className="pl-2 py-2 text-right">Всего</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
              {rows.map((r) => (
                <tr key={r.studentId}>
                  <td className="py-2 pr-3 text-[var(--color-foreground)]">
                    {studentName.get(r.studentId) ?? short(r.studentId)}
                  </td>
                  <td className="px-2 py-2 text-right tabular-nums">{r.presentCount}</td>
                  <td className="px-2 py-2 text-right tabular-nums">{r.absentCount}</td>
                  <td className="px-2 py-2 text-right tabular-nums">{r.lateCount}</td>
                  <td className="px-2 py-2 text-right tabular-nums">{r.excusedCount}</td>
                  <td className="pl-2 py-2 text-right font-medium tabular-nums">
                    {r.totalCount}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </EntityDetailSection>
  );
}
