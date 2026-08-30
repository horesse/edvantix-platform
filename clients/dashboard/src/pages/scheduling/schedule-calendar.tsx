import { useCallback, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import FullCalendar from "@fullcalendar/react";
import type {
  DatesSetArg,
  EventClickArg,
  EventDropArg,
  EventInput,
} from "@fullcalendar/core";
import ruLocale from "@fullcalendar/core/locales/ru";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin from "@fullcalendar/interaction";
import {
  CalendarDays,
  ChevronLeft,
  ChevronRight,
  RefreshCw,
} from "lucide-react";
import { toast } from "sonner";
import {
  getCalendar,
  rescheduleSession,
  type CalendarEntryDto,
  type SessionDto,
} from "@/api/scheduling";
import { searchStudyGroups } from "@/api/study-groups";
import { searchTeachers } from "@/api/people";
import { getRooms } from "@/api/scheduling";
import { getTenantSettings } from "@/api/tenant-settings";
import { useAuth } from "@/auth/use-auth";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { ApiRequestError } from "@/lib/api-client";
import { describe } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";
import { Button } from "@/components/ui/button";
import { Combobox, EntityEmpty, PageHero } from "@/components/list";
import { utcIsoToZonedWallClock, wallClockToUtc } from "@/lib/tz";
import { ForceRescheduleDialog, type PendingReschedule } from "./reschedule";
import {
  SESSION_STATUS_LABEL,
  SESSION_STATUS_TONE,
  conflictLines,
  groupColor,
} from "./scheduling-ui";
import "./calendar.css";

type CalView = "timeGridWeek" | "dayGridMonth";

export function ScheduleCalendarPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const perms = useAuth().user?.permissions ?? [];
  const canView = perms.includes("Permissions.Scheduling.Sessions.View");
  const canReschedule = perms.includes("Permissions.Scheduling.Sessions.Reschedule");

  const calendarRef = useRef<FullCalendar>(null);
  const [view, setView] = useState<CalView>("timeGridWeek");
  const [title, setTitle] = useState("");
  const [range, setRange] = useState<{ from: string; to: string } | null>(null);
  const [studyGroupId, setStudyGroupId] = useState<string | null>(null);
  const [teacherId, setTeacherId] = useState<string | null>(null);
  const [roomId, setRoomId] = useState<string | null>(null);
  const [pending, setPending] = useState<PendingReschedule | null>(null);

  const settingsQuery = useQuery({
    queryKey: ["tenant-settings"],
    queryFn: getTenantSettings,
    staleTime: 5 * 60_000,
  });
  const tz = settingsQuery.data?.timeZoneId || "UTC";

  const groupsQuery = useQuery({
    queryKey: ["study-groups", { pageSize: 100, for: "schedule" }],
    queryFn: () => searchStudyGroups({ pageSize: 100 }),
    staleTime: 60_000,
  });
  const teachersQuery = useQuery({
    queryKey: ["teachers", { pageSize: 100, for: "schedule" }],
    queryFn: () => searchTeachers({ pageSize: 100 }),
    staleTime: 60_000,
  });
  const roomsQuery = useQuery({
    queryKey: ["rooms"],
    queryFn: getRooms,
    staleTime: 60_000,
  });

  const groupById = useMemo(() => {
    const m = new Map<string, { code: string; name: string }>();
    for (const g of groupsQuery.data?.items ?? [])
      m.set(g.id, { code: g.code, name: g.name });
    return m;
  }, [groupsQuery.data]);

  // Widen the query window by a day each side so events near the visible edge
  // aren't dropped by the UTC-vs-school-tz offset (see lib/tz.ts).
  const queryParams = useMemo(() => {
    if (!range) return null;
    const pad = 24 * 60 * 60 * 1000;
    return {
      from: new Date(new Date(range.from).getTime() - pad).toISOString(),
      to: new Date(new Date(range.to).getTime() + pad).toISOString(),
      studyGroupId,
      teacherId,
      roomId,
    };
  }, [range, studyGroupId, teacherId, roomId]);

  const calendarQuery = useQuery({
    queryKey: ["sessions", "calendar", queryParams],
    queryFn: () => getCalendar(queryParams!),
    enabled: !!queryParams && canView,
  });

  const invalidateCalendar = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ["sessions", "calendar"] });
  }, [queryClient]);

  useRealtimeEvent<SessionDto>("SessionScheduleChanged", () => {
    invalidateCalendar();
  });

  const events: EventInput[] = useMemo(() => {
    return (calendarQuery.data ?? []).map((s: CalendarEntryDto) => {
      const g = groupById.get(s.studyGroupId);
      const draggable =
        canReschedule && s.status === "Planned" && view === "timeGridWeek";
      const isDim = s.status === "Cancelled" || s.status === "Rescheduled";
      const color = isDim ? "oklch(0.6 0 0)" : groupColor(s.studyGroupId);
      return {
        id: s.sessionId,
        title: `${g?.code ?? "Группа"}${s.topic ? ` · ${s.topic}` : ""}`,
        start: utcIsoToZonedWallClock(s.startUtc, tz),
        end: utcIsoToZonedWallClock(s.endUtc, tz),
        backgroundColor: color,
        borderColor: color,
        editable: draggable,
        classNames: isDim ? ["fc-session-dim"] : [],
        extendedProps: { status: s.status, studyGroupId: s.studyGroupId },
      } satisfies EventInput;
    });
  }, [calendarQuery.data, groupById, canReschedule, view, tz]);

  const onDatesSet = useCallback((arg: DatesSetArg) => {
    setTitle(arg.view.title);
    setRange({ from: arg.start.toISOString(), to: arg.end.toISOString() });
  }, []);

  const onEventClick = useCallback(
    (arg: EventClickArg) => {
      navigate(`/sessions/${arg.event.id}`);
    },
    [navigate],
  );

  const onEventDrop = useCallback(
    async (arg: EventDropArg) => {
      const { event } = arg;
      if (!event.start || !event.end) {
        arg.revert();
        return;
      }
      const newStartUtc = wallClockToUtc(event.start, tz).toISOString();
      const newEndUtc = wallClockToUtc(event.end, tz).toISOString();
      try {
        await rescheduleSession({
          sessionId: event.id,
          newStartUtc,
          newEndUtc,
          force: false,
        });
        toast.success("Занятие перенесено");
        invalidateCalendar();
      } catch (err) {
        // A drag always snaps back; the reschedule creates a *new* session id,
        // so the calendar is refetched rather than the chip left in place.
        arg.revert();
        if (err instanceof ApiRequestError && err.status === 409) {
          setPending({
            sessionId: event.id,
            newStartUtc,
            newEndUtc,
            conflicts: conflictLines(err),
          });
          return;
        }
        toast.error("Не удалось перенести", { description: describe(err) });
      }
    },
    [tz, invalidateCalendar],
  );

  const api = () => calendarRef.current?.getApi();

  if (!canView) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="Расписание" title="Календарь занятий" />
        <EntityEmpty
          icon={CalendarDays}
          title="Нет доступа"
          body="Для просмотра расписания нужно право «Просмотр занятий»."
        />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <PageHero
        eyebrow="Расписание"
        title="Календарь занятий"
        subtitle={`Неделя и месяц. Часовой пояс школы: ${tz}. Перетащите занятие в режиме недели, чтобы перенести его.`}
      />

      <div className="flex flex-wrap items-center gap-2">
        <Combobox
          label="Группа"
          value={studyGroupId}
          onChange={setStudyGroupId}
          options={(groupsQuery.data?.items ?? []).map((g) => ({
            value: g.id,
            label: `${g.code} — ${g.name}`,
          }))}
          variant="filter"
          searchable
          clearable
        />
        <Combobox
          label="Преподаватель"
          value={teacherId}
          onChange={setTeacherId}
          options={(teachersQuery.data?.items ?? []).map((t) => ({
            value: t.id,
            label: t.displayName,
          }))}
          variant="filter"
          searchable
          clearable
        />
        <Combobox
          label="Аудитория"
          value={roomId}
          onChange={setRoomId}
          options={(roomsQuery.data ?? []).map((r) => ({
            value: r.id,
            label: r.name,
          }))}
          variant="filter"
          searchable
          clearable
        />
        <Button
          variant="outline"
          size="sm"
          className="ml-auto gap-1.5"
          onClick={() => void calendarQuery.refetch()}
          disabled={calendarQuery.isFetching}
        >
          <RefreshCw
            className={cn("h-3.5 w-3.5", calendarQuery.isFetching && "animate-spin")}
          />
          <span className="hidden sm:inline">Обновить</span>
        </Button>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-1">
          <Button
            variant="outline"
            size="sm"
            aria-label="Назад"
            onClick={() => api()?.prev()}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => api()?.today()}
          >
            Сегодня
          </Button>
          <Button
            variant="outline"
            size="sm"
            aria-label="Вперёд"
            onClick={() => api()?.next()}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
          <span className="ml-2 text-[13px] font-semibold capitalize text-[var(--color-foreground)]">
            {title}
          </span>
        </div>
        <div className="flex items-center gap-1">
          <Button
            variant={view === "timeGridWeek" ? "default" : "outline"}
            size="sm"
            onClick={() => {
              setView("timeGridWeek");
              api()?.changeView("timeGridWeek");
            }}
          >
            Неделя
          </Button>
          <Button
            variant={view === "dayGridMonth" ? "default" : "outline"}
            size="sm"
            onClick={() => {
              setView("dayGridMonth");
              api()?.changeView("dayGridMonth");
            }}
          >
            Месяц
          </Button>
        </div>
      </div>

      {calendarQuery.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(calendarQuery.error)}
        </div>
      )}

      <div className="fsh-calendar rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-2 sm:p-3">
        <FullCalendar
          ref={calendarRef}
          plugins={[timeGridPlugin, dayGridPlugin, interactionPlugin]}
          initialView="timeGridWeek"
          locale={ruLocale}
          timeZone="UTC"
          headerToolbar={false}
          height="auto"
          nowIndicator
          allDaySlot={false}
          slotMinTime="07:00:00"
          slotMaxTime="23:00:00"
          firstDay={1}
          expandRows
          editable={canReschedule}
          eventStartEditable={canReschedule}
          eventDurationEditable={false}
          events={events}
          datesSet={onDatesSet}
          eventClick={onEventClick}
          eventDrop={onEventDrop}
        />
      </div>

      <div className="flex flex-wrap items-center gap-3 text-[11.5px] text-[var(--color-muted-foreground)]">
        <span className="font-medium">Статусы:</span>
        {(["Planned", "Held", "Cancelled", "Rescheduled"] as const).map((s) => (
          <span key={s} className="inline-flex items-center gap-1.5">
            <span
              className={cn(
                "inline-block size-2.5 rounded-full",
                SESSION_STATUS_TONE[s] === "success" && "bg-[var(--color-success,#16a34a)]",
              )}
              style={
                s === "Cancelled" || s === "Rescheduled"
                  ? { background: "oklch(0.6 0 0)" }
                  : { background: groupColor(s) }
              }
            />
            {SESSION_STATUS_LABEL[s]}
          </span>
        ))}
        <span className="ml-2">Цвет занятия — по учебной группе.</span>
      </div>

      {pending && (
        <ForceRescheduleDialog
          pending={pending}
          timeZoneId={tz}
          onClose={() => setPending(null)}
          onDone={() => {
            setPending(null);
            invalidateCalendar();
          }}
        />
      )}
    </div>
  );
}
