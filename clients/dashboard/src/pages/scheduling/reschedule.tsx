import { useMemo, useState, type FormEvent } from "react";
import { useMutation } from "@tanstack/react-query";
import { CalendarClock } from "lucide-react";
import { toast } from "sonner";
import { rescheduleSession, type SessionDetailDto } from "@/api/scheduling";
import { ApiRequestError } from "@/lib/api-client";
import { describe } from "@/lib/list-helpers";
import { conflictLines } from "./scheduling-ui";
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
import { ErrorBand, Field } from "@/components/list";
import {
  utcIsoToZonedWallClock,
  zonedWallClockToUtcIso,
  formatZonedDateTime,
} from "@/lib/tz";

function splitWallClock(wall: string): { date: string; time: string } {
  const [date, time = "00:00:00"] = wall.split("T");
  return { date, time: time.slice(0, 5) };
}

// ─────────────────────────────────────────────────────────────────────────
//  RescheduleDialog — full date/time form. Used from the session card's
//  "Перенести" action. On 409 it does not close: it shows the conflict and
//  offers a "force" retry.
// ─────────────────────────────────────────────────────────────────────────

export function RescheduleDialog({
  session,
  timeZoneId,
  onClose,
  onDone,
}: {
  session: Pick<SessionDetailDto, "id" | "startUtc" | "endUtc" | "roomId" | "teacherId">;
  timeZoneId: string;
  onClose: () => void;
  onDone: (newSessionId: string) => void;
}) {
  const durationMs = useMemo(
    () => new Date(session.endUtc).getTime() - new Date(session.startUtc).getTime(),
    [session.startUtc, session.endUtc],
  );

  const initial = splitWallClock(utcIsoToZonedWallClock(session.startUtc, timeZoneId));
  const [date, setDate] = useState(initial.date);
  const [time, setTime] = useState(initial.time);
  const [conflicts, setConflicts] = useState<string[] | null>(null);

  const newStartUtc = useMemo(() => {
    if (!date || !time) return null;
    return zonedWallClockToUtcIso(`${date}T${time}:00`, timeZoneId);
  }, [date, time, timeZoneId]);
  const newEndUtc = useMemo(
    () => (newStartUtc ? new Date(new Date(newStartUtc).getTime() + durationMs).toISOString() : null),
    [newStartUtc, durationMs],
  );

  const mutation = useMutation({
    mutationFn: (vars: { force: boolean }) =>
      rescheduleSession({
        sessionId: session.id,
        newStartUtc: newStartUtc!,
        newEndUtc: newEndUtc!,
        roomId: session.roomId,
        teacherId: session.teacherId,
        force: vars.force,
      }),
    onSuccess: (newId) => {
      toast.success("Занятие перенесено");
      onDone(newId);
      onClose();
    },
    onError: (err) => {
      if (err instanceof ApiRequestError && err.status === 409) {
        setConflicts(conflictLines(err));
        return;
      }
      toast.error("Не удалось перенести", { description: describe(err) });
    },
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setConflicts(null);
    if (!newStartUtc || !newEndUtc) return;
    mutation.mutate({ force: false });
  };

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Перенести занятие</DialogTitle>
            <DialogDescription>
              Текущее занятие получит статус «Перенесено», а на новое время
              создаётся замена со ссылкой на исходное. Время указывается в
              часовом поясе школы.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <Field id="rs-date" label="Дата" required>
                <Input
                  id="rs-date"
                  type="date"
                  value={date}
                  onChange={(e) => setDate(e.target.value)}
                  required
                />
              </Field>
              <Field id="rs-time" label="Начало" required>
                <Input
                  id="rs-time"
                  type="time"
                  value={time}
                  onChange={(e) => setTime(e.target.value)}
                  required
                />
              </Field>
            </div>
            {newStartUtc && (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                Новое начало: {formatZonedDateTime(newStartUtc, timeZoneId)} (
                {Math.round(durationMs / 60000)} мин)
              </p>
            )}

            {conflicts && (
              <div className="space-y-2">
                <ErrorBand message="Новый слот пересекается с другим занятием:" />
                <ul className="ml-1 list-disc space-y-1 pl-4 text-[12px] text-[var(--color-destructive)]">
                  {conflicts.map((c) => (
                    <li key={c}>{c}</li>
                  ))}
                </ul>
              </div>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            {conflicts ? (
              <Button
                type="button"
                variant="destructive"
                disabled={mutation.isPending || !newStartUtc}
                onClick={() => mutation.mutate({ force: true })}
                className="gap-1.5"
              >
                <CalendarClock className="h-4 w-4" />
                {mutation.isPending ? "Перенос…" : "Перенести всё равно"}
              </Button>
            ) : (
              <Button
                type="submit"
                disabled={mutation.isPending || !newStartUtc}
                className="gap-1.5"
              >
                <CalendarClock className="h-4 w-4" />
                {mutation.isPending ? "Перенос…" : "Перенести"}
              </Button>
            )}
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─────────────────────────────────────────────────────────────────────────
//  ForceRescheduleDialog — confirm-only. Shown after a calendar drag has
//  already POSTed reschedule(force:false) and got a 409 back. Confirm =
//  retry with force:true; cancel = leave it (the caller refetches, which
//  snaps the dragged event back to its original slot).
// ─────────────────────────────────────────────────────────────────────────

export type PendingReschedule = {
  sessionId: string;
  newStartUtc: string;
  newEndUtc: string;
  roomId?: string | null;
  teacherId?: string | null;
  conflicts: string[];
};

export function ForceRescheduleDialog({
  pending,
  timeZoneId,
  onClose,
  onDone,
}: {
  pending: PendingReschedule;
  timeZoneId: string;
  onClose: () => void;
  onDone: (newSessionId: string) => void;
}) {
  const mutation = useMutation({
    mutationFn: () =>
      rescheduleSession({
        sessionId: pending.sessionId,
        newStartUtc: pending.newStartUtc,
        newEndUtc: pending.newEndUtc,
        roomId: pending.roomId,
        teacherId: pending.teacherId,
        force: true,
      }),
    onSuccess: (newId) => {
      toast.success("Занятие перенесено");
      onDone(newId);
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось перенести", { description: describe(err) }),
  });

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <DialogHeader>
          <DialogTitle>Слот занят</DialogTitle>
          <DialogDescription>
            Перенос на {formatZonedDateTime(pending.newStartUtc, timeZoneId)}{" "}
            пересекается с другим занятием:
          </DialogDescription>
        </DialogHeader>
        <DialogBody>
          <ul className="ml-1 list-disc space-y-1 pl-4 text-[12px] text-[var(--color-destructive)]">
            {pending.conflicts.map((c) => (
              <li key={c}>{c}</li>
            ))}
          </ul>
        </DialogBody>
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
            className="gap-1.5"
          >
            <CalendarClock className="h-4 w-4" />
            {mutation.isPending ? "Перенос…" : "Перенести всё равно"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
