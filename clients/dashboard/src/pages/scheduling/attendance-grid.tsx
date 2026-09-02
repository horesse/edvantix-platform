import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ClipboardCheck, Save } from "lucide-react";
import { toast } from "sonner";
import {
  getSessionAttendance,
  markAttendance,
  searchSessions,
  ATTENDANCE_STATUSES,
  type AttendanceMarkInput,
  type AttendanceStatus,
} from "@/api/scheduling";
import { getStudyGroupById, searchStudyGroups } from "@/api/study-groups";
import { searchStudents } from "@/api/people";
import { getTenantSettings } from "@/api/tenant-settings";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Combobox,
  EntityEmpty,
  EntityInitialsAvatar,
  ErrorBand,
  Field,
  PageHero,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";
import { formatZonedDateTime } from "@/lib/tz";
import { ATTENDANCE_STATUS_LABEL, SESSION_STATUS_LABEL } from "./scheduling-ui";

type Row = {
  studentId: string;
  serverStatus: AttendanceStatus;
  serverComment: string;
  status: AttendanceStatus;
  comment: string;
};

export function AttendanceGridPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canView = perms.includes("Permissions.Scheduling.Attendance.View");
  const canMark = perms.includes("Permissions.Scheduling.Attendance.Mark");
  const queryClient = useQueryClient();

  const [searchParams, setSearchParams] = useSearchParams();
  const [studyGroupId, setStudyGroupId] = useState<string | null>(null);
  const [sessionId, setSessionId] = useState<string | null>(
    searchParams.get("sessionId"),
  );

  const settingsQuery = useQuery({
    queryKey: ["tenant-settings"],
    queryFn: getTenantSettings,
    staleTime: 5 * 60_000,
  });
  const tz = settingsQuery.data?.timeZoneId || "UTC";

  const groupsQuery = useQuery({
    queryKey: ["study-groups", { pageSize: 100, for: "attendance" }],
    queryFn: () => searchStudyGroups({ pageSize: 100 }),
    staleTime: 60_000,
  });

  // Session list — filtered by group when one is picked.
  const sessionsQuery = useQuery({
    queryKey: ["sessions", { studyGroupId, for: "attendance", pageSize: 100 }],
    queryFn: () =>
      searchSessions({
        studyGroupId: studyGroupId ?? undefined,
        pageSize: 100,
        sortBy: "startUtc",
        sortDir: "desc",
      }),
    enabled: canView,
  });

  const sessionOptions = useMemo(
    () =>
      (sessionsQuery.data?.items ?? []).map((s) => ({
        value: s.id,
        label: `${formatZonedDateTime(s.startUtc, tz)} · ${
          SESSION_STATUS_LABEL[s.status]
        }${s.topic ? ` · ${s.topic}` : ""}`,
      })),
    [sessionsQuery.data, tz],
  );

  // Keep the ?sessionId= query param in sync so the session card can deep-link.
  useEffect(() => {
    const next = new URLSearchParams(searchParams);
    if (sessionId) next.set("sessionId", sessionId);
    else next.delete("sessionId");
    setSearchParams(next, { replace: true });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessionId]);

  const attendanceKey = ["session-attendance", sessionId] as const;
  const attendanceQuery = useQuery({
    queryKey: attendanceKey,
    queryFn: () => getSessionAttendance(sessionId!),
    enabled: !!sessionId && canView,
  });

  // Fall back to the group roster when the session has no attendance rows yet
  // (not held). PUT creates the missing rows on the fly.
  const groupDetailQuery = useQuery({
    queryKey: ["study-group", studyGroupId],
    queryFn: () => getStudyGroupById(studyGroupId!),
    enabled: !!studyGroupId && !!sessionId,
    staleTime: 30_000,
  });

  const studentsQuery = useQuery({
    queryKey: ["students", { pageSize: 200, for: "attendance" }],
    queryFn: () => searchStudents({ pageSize: 200 }),
    staleTime: 60_000,
  });
  const studentName = useMemo(() => {
    const m = new Map<string, string>();
    for (const s of studentsQuery.data?.items ?? []) m.set(s.id, s.displayName);
    return m;
  }, [studentsQuery.data]);

  const [rows, setRows] = useState<Row[]>([]);

  useEffect(() => {
    if (!sessionId) {
      setRows([]);
      return;
    }
    const att = attendanceQuery.data ?? [];
    if (att.length > 0) {
      setRows(
        att.map((a) => ({
          studentId: a.studentId,
          serverStatus: a.status,
          serverComment: a.comment ?? "",
          status: a.status,
          comment: a.comment ?? "",
        })),
      );
      return;
    }
    // No rows yet — seed from the active roster, default Present.
    const roster = (groupDetailQuery.data?.enrollments ?? [])
      .filter((e) => e.status === "Active" || e.status === "Paused")
      .map((e) => e.studentId);
    setRows(
      roster.map((studentId) => ({
        studentId,
        serverStatus: "Present" as AttendanceStatus,
        serverComment: "",
        status: "Present" as AttendanceStatus,
        comment: "",
      })),
    );
  }, [sessionId, attendanceQuery.data, groupDetailQuery.data]);

  const dirtyMarks: AttendanceMarkInput[] = useMemo(
    () =>
      rows
        .filter(
          (r) =>
            r.status !== r.serverStatus ||
            r.comment.trim() !== r.serverComment.trim(),
        )
        .map((r) => ({
          studentId: r.studentId,
          status: r.status,
          comment: r.comment.trim() || null,
        })),
    [rows],
  );

  const mutation = useMutation({
    mutationFn: (marks: AttendanceMarkInput[]) => markAttendance(sessionId!, marks),
    onSuccess: () => {
      toast.success("Посещаемость сохранена");
      void queryClient.invalidateQueries({ queryKey: ["session-attendance", sessionId] });
      void queryClient.invalidateQueries({ queryKey: ["session", sessionId] });
    },
    onError: (err) =>
      toast.error("Не удалось сохранить посещаемость", { description: describe(err) }),
  });

  const setRow = (studentId: string, patch: Partial<Row>) =>
    setRows((prev) =>
      prev.map((r) => (r.studentId === studentId ? { ...r, ...patch } : r)),
    );

  if (!canView) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="Расписание" title="Посещаемость" />
        <EntityEmpty
          icon={ClipboardCheck}
          title="Нет доступа"
          body="Нужно право «Просмотр посещаемости»."
        />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <PageHero
        eyebrow="Расписание"
        title="Посещаемость"
        subtitle="Сетка «ученики × занятие». Строки по умолчанию — «Был», отмечайте только исключения. Сохранение отправляет всю сетку одним запросом."
      />

      <div className="grid gap-3 sm:grid-cols-2">
        <Field id="att-group" label="Учебная группа">
          <Combobox
            id="att-group"
            label="Учебная группа"
            value={studyGroupId}
            onChange={(v) => {
              setStudyGroupId(v);
              setSessionId(null);
            }}
            options={(groupsQuery.data?.items ?? []).map((g) => ({
              value: g.id,
              label: `${g.code} — ${g.name}`,
            }))}
            searchable
            clearable
          />
        </Field>
        <Field id="att-session" label="Занятие">
          <Combobox
            id="att-session"
            label="Занятие"
            value={sessionId}
            onChange={setSessionId}
            options={sessionOptions}
            placeholder={
              sessionsQuery.isLoading
                ? "Загрузка…"
                : sessionOptions.length === 0
                  ? "Нет занятий"
                  : "Выберите занятие"
            }
            searchable
          />
        </Field>
      </div>

      {attendanceQuery.isError && <ErrorBand message={describe(attendanceQuery.error)} />}

      {!sessionId ? (
        <EntityEmpty
          icon={ClipboardCheck}
          title="Занятие не выбрано"
          body="Отметить посещаемость можно после выбора занятия."
        />
      ) : rows.length === 0 ? (
        <EntityEmpty
          icon={ClipboardCheck}
          title="Нет учеников"
          body="В группе занятия нет активных зачислений."
        />
      ) : (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
          <div className="hidden grid-cols-[1.6fr_1fr_1.4fr] gap-3 border-b border-[var(--color-border)] px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] sm:grid">
            <span>Ученик</span>
            <span>Статус</span>
            <span>Комментарий</span>
          </div>
          <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
            {rows.map((r) => {
              const name = studentName.get(r.studentId) ?? r.studentId.slice(0, 8);
              const changed =
                r.status !== r.serverStatus ||
                r.comment.trim() !== r.serverComment.trim();
              return (
                <li
                  key={r.studentId}
                  className={cn(
                    "grid grid-cols-1 gap-2 px-4 py-3 sm:grid-cols-[1.6fr_1fr_1.4fr] sm:items-center sm:gap-3",
                    changed && "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.04)]",
                  )}
                >
                  <div className="flex min-w-0 items-center gap-3">
                    <EntityInitialsAvatar name={name} size={32} />
                    <span className="truncate text-[13px] text-[var(--color-foreground)]">
                      {name}
                    </span>
                  </div>
                  <div className="flex gap-1">
                    {ATTENDANCE_STATUSES.map((s) => (
                      <button
                        key={s}
                        type="button"
                        aria-pressed={r.status === s}
                        aria-label={`${name}: ${ATTENDANCE_STATUS_LABEL[s]}`}
                        onClick={() => setRow(r.studentId, { status: s })}
                        className={cn(
                          "h-7 rounded-md border px-2 text-[11.5px] font-medium transition-colors",
                          r.status === s
                            ? "border-transparent bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                            : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
                        )}
                      >
                        {ATTENDANCE_STATUS_LABEL[s]}
                      </button>
                    ))}
                  </div>
                  <Input
                    aria-label={`Комментарий для ${name}`}
                    value={r.comment}
                    onChange={(e) => setRow(r.studentId, { comment: e.target.value })}
                    placeholder="—"
                    className="h-8 text-[13px]"
                  />
                </li>
              );
            })}
          </ul>
          <div className="flex items-center justify-between gap-3 border-t border-[var(--color-border)] px-4 py-3">
            <p className="text-[12px] text-[var(--color-muted-foreground)]">
              {dirtyMarks.length === 0
                ? "Изменений нет"
                : `Будет отправлено записей: ${dirtyMarks.length}`}
              {sessionId && (
                <>
                  {" · "}
                  <Link
                    to={`/sessions/${sessionId}`}
                    className="hover:text-[var(--color-foreground)] hover:underline"
                  >
                    карточка занятия
                  </Link>
                </>
              )}
            </p>
            <Button
              type="button"
              disabled={!canMark || mutation.isPending || dirtyMarks.length === 0}
              onClick={() => mutation.mutate(dirtyMarks)}
              className="gap-1.5"
            >
              <Save className="h-4 w-4" />
              {mutation.isPending ? "Сохранение…" : "Сохранить"}
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
