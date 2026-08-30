import { useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CalendarX2, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  addNonWorkingDay,
  getNonWorkingDays,
  removeNonWorkingDay,
  type NonWorkingDayDto,
} from "@/api/scheduling";
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
import { EntityEmpty, ErrorBand, Field, PageHero } from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";

export function NonWorkingDaysSettingsPage() {
  const perms = useAuth().user?.permissions ?? [];
  // Non-working days are schedule-generation config — gated by the templates right.
  const canView = perms.includes("Permissions.Scheduling.ScheduleTemplates.View");
  const canManage = perms.includes("Permissions.Scheduling.ScheduleTemplates.Manage");
  const queryClient = useQueryClient();

  const [addOpen, setAddOpen] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<NonWorkingDayDto | null>(null);

  const query = useQuery({
    queryKey: ["non-working-days"],
    queryFn: () => getNonWorkingDays(),
    enabled: canView,
  });

  const invalidate = () =>
    void queryClient.invalidateQueries({ queryKey: ["non-working-days"] });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => removeNonWorkingDay(id),
    onSuccess: () => {
      toast.success("День удалён");
      setDeleteTarget(null);
      invalidate();
    },
    onError: (err) =>
      toast.error("Не удалось удалить день", { description: describe(err) }),
  });

  if (!canView) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="Справочники" title="Нерабочие дни" />
        <EntityEmpty
          icon={CalendarX2}
          title="Нет доступа"
          body="Нужно право «Просмотр шаблонов расписания»."
        />
      </div>
    );
  }

  const days = [...(query.data ?? [])].sort((a, b) => a.date.localeCompare(b.date));

  return (
    <div className="space-y-4">
      <PageHero
        eyebrow="Справочники"
        title="Нерабочие дни"
        subtitle="Праздники и каникулы. Генератор расписания пропускает эти даты (не сдвигает занятия)."
        actions={
          canManage ? (
            <Button size="sm" className="gap-1.5" onClick={() => setAddOpen(true)}>
              <Plus className="h-3.5 w-3.5" />
              Добавить день
            </Button>
          ) : undefined
        }
      />

      {query.isError && <ErrorBand message={describe(query.error)} />}

      {query.isLoading ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : days.length === 0 ? (
        <EntityEmpty
          icon={CalendarX2}
          title="Нерабочих дней нет"
          body="Добавьте праздники и каникулы, чтобы генератор их пропускал."
          action={
            canManage ? (
              <Button onClick={() => setAddOpen(true)} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Добавить день
              </Button>
            ) : undefined
          }
        />
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)] rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
          {days.map((d) => (
            <li
              key={d.id}
              className="flex items-center justify-between gap-3 px-4 py-3 first:rounded-t-xl last:rounded-b-xl"
            >
              <div className="min-w-0">
                <p className="text-[13px] font-medium tabular-nums text-[var(--color-foreground)]">
                  {formatDate(d.date)}
                </p>
                {d.description && (
                  <p className="text-[11.5px] text-[var(--color-muted-foreground)]">
                    {d.description}
                  </p>
                )}
              </div>
              {canManage && (
                <Button
                  variant="ghost"
                  size="sm"
                  aria-label={`Удалить ${formatDate(d.date)}`}
                  className="shrink-0 text-[var(--color-destructive)]"
                  onClick={() => setDeleteTarget(d)}
                >
                  <Trash2 className="size-3.5" />
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}

      {addOpen && (
        <AddNonWorkingDayDialog onClose={() => setAddOpen(false)} onSaved={invalidate} />
      )}

      {deleteTarget && (
        <Dialog open onOpenChange={(o) => !o && setDeleteTarget(null)}>
          <DialogContent className="!max-w-md">
            <DialogHeader>
              <DialogTitle>Удалить нерабочий день?</DialogTitle>
              <DialogDescription>
                {formatDate(deleteTarget.date)}
                {deleteTarget.description ? ` — ${deleteTarget.description}` : ""}. После
                удаления генератор снова будет создавать занятия на эту дату.
              </DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <DialogClose asChild>
                <Button type="button" variant="outline" disabled={deleteMutation.isPending}>
                  Отмена
                </Button>
              </DialogClose>
              <Button
                variant="destructive"
                disabled={deleteMutation.isPending}
                onClick={() => deleteMutation.mutate(deleteTarget.id)}
              >
                {deleteMutation.isPending ? "Удаление…" : "Удалить"}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}

function AddNonWorkingDayDialog({
  onClose,
  onSaved,
}: {
  onClose: () => void;
  onSaved: () => void;
}) {
  const [date, setDate] = useState("");
  const [description, setDescription] = useState("");

  const mutation = useMutation({
    mutationFn: (vars: { date: string; description: string | null }) =>
      addNonWorkingDay(vars),
    onSuccess: () => {
      toast.success("Нерабочий день добавлен");
      onSaved();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось добавить день", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!date) return;
    mutation.mutate({ date, description: description.trim() || null });
  };

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Новый нерабочий день</DialogTitle>
            <DialogDescription>Дата и необязательное описание.</DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <Field id="nwd-date" label="Дата" required>
              <Input
                id="nwd-date"
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                required
                autoFocus
              />
            </Field>
            <Field id="nwd-desc" label="Описание">
              <Input
                id="nwd-desc"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Например: Новый год"
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !date}>
              {mutation.isPending ? "Добавление…" : "Добавить"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
