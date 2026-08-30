import { useMemo, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  BookOpen,
  CalendarClock,
  CheckSquare,
  ClipboardCheck,
  DoorOpen,
  ExternalLink,
  FileText,
  RefreshCw,
  UserRound,
  UsersRound,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";
import {
  cancelSession,
  getSessionById,
  holdSession,
  type SessionDto,
} from "@/api/scheduling";
import { getLessonMaterials } from "@/api/curriculum";
import { searchStudents, searchTeachers } from "@/api/people";
import { searchStudyGroups } from "@/api/study-groups";
import { getRooms } from "@/api/scheduling";
import { getTenantSettings } from "@/api/tenant-settings";
import { useAuth } from "@/auth/use-auth";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { Button } from "@/components/ui/button";
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
  EntityInitialsAvatar,
  EntityStatusBadge,
  ErrorBand,
  Field,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";
import { formatZonedDateTime, formatZonedTime } from "@/lib/tz";
import { RescheduleDialog } from "./reschedule";
import {
  ATTENDANCE_STATUS_LABEL,
  ATTENDANCE_STATUS_TONE,
  SESSION_STATUS_LABEL,
  SESSION_STATUS_TONE,
  isTerminalSession,
} from "./scheduling-ui";

const TEXTAREA_CLS = cn(
  "flex w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm",
  "placeholder:text-[var(--color-muted-foreground)]",
  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
);

function short(id: string) {
  return id.slice(0, 8);
}

export function SessionDetailPage() {
  const { sessionId = "" } = useParams<{ sessionId: string }>();
  const queryClient = useQueryClient();
  const perms = useAuth().user?.permissions ?? [];
  const canUpdate = perms.includes("Permissions.Scheduling.Sessions.Update");
  const canCancel = perms.includes("Permissions.Scheduling.Sessions.Cancel");
  const canReschedule = perms.includes("Permissions.Scheduling.Sessions.Reschedule");
  const canMarkAttendance = perms.includes("Permissions.Scheduling.Attendance.Mark");
  const canViewMaterials = perms.includes(
    "Permissions.Curriculum.LessonMaterials.View",
  );

  const [cancelOpen, setCancelOpen] = useState(false);
  const [rescheduleOpen, setRescheduleOpen] = useState(false);

  const sessionKey = ["session", sessionId] as const;
  const query = useQuery({
    queryKey: sessionKey,
    queryFn: () => getSessionById(sessionId),
    enabled: !!sessionId,
  });
  const session = query.data;

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: sessionKey });
    void queryClient.invalidateQueries({ queryKey: ["sessions"] });
  };

  useRealtimeEvent<SessionDto>(
    "SessionScheduleChanged",
    (payload) => {
      if (payload?.id === sessionId) invalidate();
    },
    [sessionId],
  );

  const settingsQuery = useQuery({
    queryKey: ["tenant-settings"],
    queryFn: getTenantSettings,
    staleTime: 5 * 60_000,
  });
  const tz = settingsQuery.data?.timeZoneId || "UTC";

  const groupsQuery = useQuery({
    queryKey: ["study-groups", { pageSize: 100, for: "session" }],
    queryFn: () => searchStudyGroups({ pageSize: 100 }),
    staleTime: 60_000,
  });
  const teachersQuery = useQuery({
    queryKey: ["teachers", { pageSize: 200, for: "session" }],
    queryFn: () => searchTeachers({ pageSize: 200 }),
    staleTime: 60_000,
  });
  const roomsQuery = useQuery({
    queryKey: ["rooms"],
    queryFn: getRooms,
    staleTime: 60_000,
  });
  const studentsQuery = useQuery({
    queryKey: ["students", { pageSize: 300, for: "session" }],
    queryFn: () => searchStudents({ pageSize: 300 }),
    staleTime: 60_000,
    enabled: (session?.attendance.length ?? 0) > 0,
  });

  const groupName = useMemo(() => {
    const g = groupsQuery.data?.items.find((x) => x.id === session?.studyGroupId);
    return g ? `${g.code} — ${g.name}` : undefined;
  }, [groupsQuery.data, session?.studyGroupId]);
  const teacherName = useMemo(
    () =>
      teachersQuery.data?.items.find((t) => t.id === session?.teacherId)?.displayName,
    [teachersQuery.data, session?.teacherId],
  );
  const roomName = useMemo(
    () => roomsQuery.data?.find((r) => r.id === session?.roomId)?.name,
    [roomsQuery.data, session?.roomId],
  );
  const studentName = useMemo(() => {
    const m = new Map<string, string>();
    for (const s of studentsQuery.data?.items ?? []) m.set(s.id, s.displayName);
    return m;
  }, [studentsQuery.data]);

  const materialsQuery = useQuery({
    queryKey: ["lesson-materials", session?.lessonId],
    queryFn: () => getLessonMaterials(session!.lessonId!),
    enabled: !!session?.lessonId && canViewMaterials,
    staleTime: 60_000,
  });

  const holdMutation = useMutation({
    mutationFn: () => holdSession(sessionId),
    onSuccess: () => {
      toast.success("Занятие проведено — создана посещаемость");
      invalidate();
    },
    onError: (err) =>
      toast.error("Не удалось отметить проведение", { description: describe(err) }),
  });

  const frozen = session ? isTerminalSession(session.status) : false;

  return (
    <div className="pb-12">
      <EntityDetailBack to="/schedule" label="К календарю" />

      {query.isError && (
        <div className="mb-5">
          <ErrorBand message={describe(query.error)} />
        </div>
      )}

      {query.isLoading ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка занятия…</p>
      ) : session ? (
        <>
          <EntityDetailHero
            avatar={<EntityDetailAvatar name={session.resolvedTopic} icon={CalendarClock} />}
            title={session.resolvedTopic}
            badges={
              <EntityStatusBadge tone={SESSION_STATUS_TONE[session.status]}>
                {SESSION_STATUS_LABEL[session.status]}
              </EntityStatusBadge>
            }
            subtitle={
              <span>
                {formatZonedDateTime(session.startUtc, tz)} –{" "}
                {formatZonedTime(session.endUtc, tz)} · часовой пояс школы: {tz}
              </span>
            }
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
                {!frozen && canUpdate && (
                  <Button
                    size="sm"
                    onClick={() => holdMutation.mutate()}
                    disabled={holdMutation.isPending}
                    className="gap-1.5"
                  >
                    <CheckSquare className="h-3.5 w-3.5" />
                    Провести
                  </Button>
                )}
                {!frozen && canReschedule && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setRescheduleOpen(true)}
                    className="gap-1.5"
                  >
                    <CalendarClock className="h-3.5 w-3.5" />
                    <span className="hidden sm:inline">Перенести</span>
                  </Button>
                )}
                {!frozen && canCancel && (
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
              </>
            }
            stats={
              <>
                <EntityDetailStat
                  icon={UsersRound}
                  value={groupName ?? short(session.studyGroupId)}
                  label="группа"
                />
                <EntityDetailStat
                  icon={UserRound}
                  value={teacherName ?? short(session.teacherId)}
                  label="преподаватель"
                />
                <EntityDetailStat
                  icon={DoorOpen}
                  value={roomName ?? (session.roomId ? short(session.roomId) : "—")}
                  label="аудитория"
                />
              </>
            }
            meta={
              <>
                <EntityDetailMeta icon={UsersRound}>
                  <Link
                    to={`/study-groups/${session.studyGroupId}`}
                    className="hover:text-[var(--color-foreground)]"
                  >
                    Открыть группу
                  </Link>
                </EntityDetailMeta>
                {session.meetingUrl && (
                  <EntityDetailMeta icon={ExternalLink} hideOnMobile>
                    <a
                      href={session.meetingUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="hover:text-[var(--color-foreground)]"
                    >
                      Ссылка на встречу
                    </a>
                  </EntityDetailMeta>
                )}
                {session.rescheduledFromId && (
                  <EntityDetailMeta icon={CalendarClock}>
                    <Link
                      to={`/sessions/${session.rescheduledFromId}`}
                      className="hover:text-[var(--color-foreground)]"
                    >
                      Перенесено с другого занятия
                    </Link>
                  </EntityDetailMeta>
                )}
              </>
            }
          />

          {frozen && (
            <div className="mb-5 rounded-lg border border-[var(--color-border)] bg-[oklch(from_var(--color-muted)_l_c_h_/_0.3)] px-3 py-2 text-[12px] text-[var(--color-muted-foreground)]">
              Занятие{" "}
              {session.status === "Held"
                ? "проведено"
                : session.status === "Cancelled"
                  ? "отменено"
                  : "перенесено"}{" "}
              — карточка доступна только для чтения.
            </div>
          )}

          {session.status === "Cancelled" && session.cancelReason && (
            <EntityDetailSection title="Причина отмены" icon={XCircle}>
              <p className="text-[13px] text-[var(--color-foreground)]">
                {session.cancelReason}
              </p>
            </EntityDetailSection>
          )}

          {session.teacherComment && (
            <EntityDetailSection title="Комментарий преподавателя" icon={FileText}>
              <p className="whitespace-pre-wrap text-[13px] text-[var(--color-foreground)]">
                {session.teacherComment}
              </p>
            </EntityDetailSection>
          )}

          <div className="mt-4 space-y-4">
            <EntityDetailSection
              title="Посещаемость"
              icon={ClipboardCheck}
              description={
                session.status === "Planned"
                  ? "Появится после того, как занятие будет проведено."
                  : `${session.attendance.length} записей`
              }
              action={
                canMarkAttendance && session.status !== "Planned" ? (
                  <Button variant="outline" size="sm" asChild className="gap-1.5">
                    <Link to={`/attendance?sessionId=${session.id}`}>
                      <ClipboardCheck className="size-3.5" />
                      Отметить
                    </Link>
                  </Button>
                ) : undefined
              }
            >
              {session.attendance.length === 0 ? (
                <p className="text-[13px] text-[var(--color-muted-foreground)]">
                  Записей о посещаемости пока нет.
                </p>
              ) : (
                <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
                  {session.attendance.map((a) => {
                    const name = studentName.get(a.studentId) ?? short(a.studentId);
                    return (
                      <li
                        key={a.id}
                        className="flex items-center justify-between gap-3 py-2.5 first:pt-0 last:pb-0"
                      >
                        <div className="flex min-w-0 items-center gap-3">
                          <EntityInitialsAvatar name={name} size={32} />
                          <div className="min-w-0">
                            <p className="truncate text-[13px] text-[var(--color-foreground)]">
                              {name}
                            </p>
                            {a.comment && (
                              <p className="truncate text-[11.5px] text-[var(--color-muted-foreground)]">
                                {a.comment}
                              </p>
                            )}
                          </div>
                        </div>
                        <EntityStatusBadge tone={ATTENDANCE_STATUS_TONE[a.status]}>
                          {ATTENDANCE_STATUS_LABEL[a.status]}
                        </EntityStatusBadge>
                      </li>
                    );
                  })}
                </ul>
              )}
            </EntityDetailSection>

            {session.lessonId && canViewMaterials && (
              <EntityDetailSection
                title="Материалы урока"
                icon={BookOpen}
                description="Подтягиваются из программы (Curriculum)."
              >
                {materialsQuery.isLoading ? (
                  <p className="text-[13px] text-[var(--color-muted-foreground)]">
                    Загрузка…
                  </p>
                ) : materialsQuery.isError ? (
                  <p className="text-[13px] text-[var(--color-destructive)]">
                    {describe(materialsQuery.error)}
                  </p>
                ) : (materialsQuery.data?.length ?? 0) === 0 ? (
                  <p className="text-[13px] text-[var(--color-muted-foreground)]">
                    К уроку не прикреплены материалы.
                  </p>
                ) : (
                  <ul className="space-y-2">
                    {materialsQuery.data!.map((m) => (
                      <li
                        key={m.id}
                        className="flex items-center gap-2 text-[13px] text-[var(--color-foreground)]"
                      >
                        <FileText className="size-3.5 shrink-0 text-[var(--color-muted-foreground)]" />
                        {m.url ? (
                          <a
                            href={m.url}
                            target="_blank"
                            rel="noreferrer"
                            className="truncate hover:underline"
                          >
                            {m.title}
                          </a>
                        ) : (
                          <span className="truncate">{m.title}</span>
                        )}
                        {!m.visibleToStudents && (
                          <span className="text-[11px] text-[var(--color-muted-foreground)]">
                            (не виден ученикам)
                          </span>
                        )}
                      </li>
                    ))}
                  </ul>
                )}
              </EntityDetailSection>
            )}
          </div>

          {rescheduleOpen && (
            <RescheduleDialog
              session={session}
              timeZoneId={tz}
              onClose={() => setRescheduleOpen(false)}
              onDone={() => invalidate()}
            />
          )}

          <CancelSessionDialog
            open={cancelOpen}
            sessionId={session.id}
            onClose={() => setCancelOpen(false)}
            onDone={invalidate}
          />
        </>
      ) : (
        <p className="text-sm text-[var(--color-muted-foreground)]">Занятие не найдено.</p>
      )}
    </div>
  );
}

function CancelSessionDialog({
  open,
  sessionId,
  onClose,
  onDone,
}: {
  open: boolean;
  sessionId: string;
  onClose: () => void;
  onDone: () => void;
}) {
  const [reason, setReason] = useState("");

  const mutation = useMutation({
    mutationFn: (r: string) => cancelSession(sessionId, r),
    onSuccess: () => {
      toast.success("Занятие отменено");
      onDone();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось отменить занятие", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate(reason.trim());
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Отменить занятие?</DialogTitle>
            <DialogDescription>
              Занятие получит статус «Отменено» и станет доступно только для
              чтения. Причина попадёт в карточку занятия.
            </DialogDescription>
          </DialogHeader>
          <DialogBody>
            <Field id="cs-reason" label="Причина" hint="Необязательно">
              <textarea
                id="cs-reason"
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
              {mutation.isPending ? "Отмена…" : "Отменить занятие"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
