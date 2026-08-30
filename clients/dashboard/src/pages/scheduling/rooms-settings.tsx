import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { DoorOpen, Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createRoom,
  deleteRoom,
  getRooms,
  updateRoom,
  type RoomDto,
  type RoomInput,
} from "@/api/scheduling";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
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
  EntityEmpty,
  EntityStatusBadge,
  ErrorBand,
  Field,
  PageHero,
} from "@/components/list";
import { describe } from "@/lib/list-helpers";

export function RoomsSettingsPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canView = perms.includes("Permissions.Scheduling.Rooms.View");
  const canManage = perms.includes("Permissions.Scheduling.Rooms.Manage");
  const queryClient = useQueryClient();

  const [createOpen, setCreateOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<RoomDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<RoomDto | null>(null);

  const query = useQuery({
    queryKey: ["rooms"],
    queryFn: getRooms,
    enabled: canView,
  });

  const invalidate = () => void queryClient.invalidateQueries({ queryKey: ["rooms"] });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteRoom(id),
    onSuccess: () => {
      toast.success("Аудитория удалена");
      setDeleteTarget(null);
      invalidate();
    },
    onError: (err) =>
      toast.error("Не удалось удалить аудиторию", { description: describe(err) }),
  });

  if (!canView) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="Справочники" title="Аудитории" />
        <EntityEmpty icon={DoorOpen} title="Нет доступа" body="Нужно право «Просмотр аудиторий»." />
      </div>
    );
  }

  const rooms = [...(query.data ?? [])].sort((a, b) => a.name.localeCompare(b.name));

  return (
    <div className="space-y-4">
      <PageHero
        eyebrow="Справочники"
        title="Аудитории"
        subtitle="Помещения для очных занятий. Виртуальная аудитория («онлайн») не участвует в проверке конфликтов по месту."
        actions={
          canManage ? (
            <Button size="sm" className="gap-1.5" onClick={() => setCreateOpen(true)}>
              <Plus className="h-3.5 w-3.5" />
              Новая аудитория
            </Button>
          ) : undefined
        }
      />

      {query.isError && <ErrorBand message={describe(query.error)} />}

      {query.isLoading ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : rooms.length === 0 ? (
        <EntityEmpty
          icon={DoorOpen}
          title="Аудиторий пока нет"
          body="Добавьте помещения, чтобы назначать их занятиям и шаблонам."
          action={
            canManage ? (
              <Button onClick={() => setCreateOpen(true)} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Новая аудитория
              </Button>
            ) : undefined
          }
        />
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)] rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
          {rooms.map((r) => (
            <li
              key={r.id}
              className="flex items-center justify-between gap-3 px-4 py-3 first:rounded-t-xl last:rounded-b-xl"
            >
              <div className="min-w-0">
                <p className="text-[13px] font-medium text-[var(--color-foreground)]">
                  {r.name}{" "}
                  {r.isVirtual && <EntityStatusBadge tone="info">онлайн</EntityStatusBadge>}
                </p>
                <p className="text-[11.5px] text-[var(--color-muted-foreground)]">
                  Вместимость: {r.capacity}
                  {r.location ? ` · ${r.location}` : ""}
                </p>
              </div>
              {canManage && (
                <div className="flex shrink-0 items-center gap-1">
                  <Button
                    variant="ghost"
                    size="sm"
                    aria-label={`Изменить ${r.name}`}
                    onClick={() => setEditTarget(r)}
                  >
                    <Pencil className="size-3.5" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    aria-label={`Удалить ${r.name}`}
                    className="text-[var(--color-destructive)]"
                    onClick={() => setDeleteTarget(r)}
                  >
                    <Trash2 className="size-3.5" />
                  </Button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      {(createOpen || editTarget) && (
        <RoomDialog
          room={editTarget}
          onClose={() => {
            setCreateOpen(false);
            setEditTarget(null);
          }}
          onSaved={invalidate}
        />
      )}

      {deleteTarget && (
        <Dialog open onOpenChange={(o) => !o && setDeleteTarget(null)}>
          <DialogContent className="!max-w-md">
            <DialogHeader>
              <DialogTitle>Удалить аудиторию?</DialogTitle>
              <DialogDescription>
                «{deleteTarget.name}» будет удалена. Занятия, где она указана,
                останутся, но без привязки к месту.
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

function RoomDialog({
  room,
  onClose,
  onSaved,
}: {
  room: RoomDto | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const editing = !!room;
  const [name, setName] = useState(room?.name ?? "");
  const [capacity, setCapacity] = useState(String(room?.capacity ?? 10));
  const [location, setLocation] = useState(room?.location ?? "");
  const [isVirtual, setIsVirtual] = useState(room?.isVirtual ?? false);

  useEffect(() => {
    if (!room) return;
    setName(room.name);
    setCapacity(String(room.capacity));
    setLocation(room.location ?? "");
    setIsVirtual(room.isVirtual);
  }, [room]);

  const createMutation = useMutation({
    mutationFn: (input: RoomInput) => createRoom(input),
    onSuccess: () => {
      toast.success("Аудитория создана");
      onSaved();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось создать аудиторию", { description: describe(err) }),
  });
  const updateMutation = useMutation({
    mutationFn: (input: RoomInput) => updateRoom(room!.id, input),
    onSuccess: () => {
      toast.success("Аудитория обновлена");
      onSaved();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось обновить аудиторию", { description: describe(err) }),
  });

  const cap = Number.parseInt(capacity, 10);
  const valid = name.trim().length > 0 && !Number.isNaN(cap) && cap > 0;
  const pending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    const input: RoomInput = {
      name: name.trim(),
      capacity: cap,
      location: location.trim() || null,
      isVirtual,
    };
    if (editing) updateMutation.mutate(input);
    else createMutation.mutate(input);
  };

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{editing ? "Изменить аудиторию" : "Новая аудитория"}</DialogTitle>
            <DialogDescription>
              Виртуальная аудитория исключается из проверки конфликтов по месту.
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <Field id="room-name" label="Название" required>
              <Input
                id="room-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                autoFocus
              />
            </Field>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="room-cap" label="Вместимость" required>
                <Input
                  id="room-cap"
                  type="number"
                  min="1"
                  value={capacity}
                  onChange={(e) => setCapacity(e.target.value)}
                  required
                  className="tabular-nums"
                />
              </Field>
              <Field id="room-loc" label="Расположение">
                <Input
                  id="room-loc"
                  value={location}
                  onChange={(e) => setLocation(e.target.value)}
                  placeholder="Этаж, корпус…"
                />
              </Field>
            </div>
            <label
              htmlFor="room-virtual"
              className="flex items-center gap-2 text-[13px] text-[var(--color-foreground)]"
            >
              <Switch
                id="room-virtual"
                checked={isVirtual}
                onCheckedChange={setIsVirtual}
              />
              Виртуальная (онлайн)
            </label>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={pending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={pending || !valid}>
              {pending ? "Сохранение…" : editing ? "Сохранить" : "Создать"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
