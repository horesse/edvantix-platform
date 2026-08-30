import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  CalendarPlus,
  Eye,
  Pencil,
  Sparkles,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";
import {
  createScheduleTemplate,
  deleteScheduleTemplate,
  generateSessions,
  getScheduleTemplates,
  getRooms,
  previewGeneration,
  updateScheduleTemplate,
  DAYS_OF_WEEK,
  type CreateScheduleTemplateInput,
  type DayOfWeekName,
  type GenerationPreviewDto,
  type ScheduleTemplateDto,
  type UpdateScheduleTemplateInput,
} from "@/api/scheduling";
import { getStudyGroupById } from "@/api/study-groups";
import { searchTeachers } from "@/api/people";
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
  Combobox,
  EntityDetailBack,
  EntityDetailSection,
  EntityEmpty,
  EntityStatusBadge,
  ErrorBand,
  Field,
  PageHero,
} from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import { formatZonedDateTime } from "@/lib/tz";
import { getTenantSettings } from "@/api/tenant-settings";
import {
  CONFLICT_TYPE_LABEL,
  DAY_OF_WEEK_LABEL,
  SKIP_REASON_LABEL,
  trimSeconds,
} from "./scheduling-ui";

export function GroupScheduleTemplatesPage() {
  const { studyGroupId = "" } = useParams<{ studyGroupId: string }>();
  const queryClient = useQueryClient();
  const perms = useAuth().user?.permissions ?? [];
  const canView = perms.includes("Permissions.Scheduling.ScheduleTemplates.View");
  const canManage = perms.includes("Permissions.Scheduling.ScheduleTemplates.Manage");
  const canGenerate = perms.includes("Permissions.Scheduling.Sessions.Generate");

  const [editTarget, setEditTarget] = useState<ScheduleTemplateDto | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<ScheduleTemplateDto | null>(null);
  const [previewId, setPreviewId] = useState<string | null>(null);

  const templatesKey = ["schedule-templates", studyGroupId] as const;
  const query = useQuery({
    queryKey: templatesKey,
    queryFn: () => getScheduleTemplates(studyGroupId),
    enabled: !!studyGroupId && canView,
  });

  const groupQuery = useQuery({
    queryKey: ["study-group", studyGroupId],
    queryFn: () => getStudyGroupById(studyGroupId),
    enabled: !!studyGroupId,
  });
  const teachersQuery = useQuery({
    queryKey: ["teachers", { pageSize: 200, for: "templates" }],
    queryFn: () => searchTeachers({ pageSize: 200 }),
    staleTime: 60_000,
  });
  const roomsQuery = useQuery({
    queryKey: ["rooms"],
    queryFn: getRooms,
    staleTime: 60_000,
  });
  const settingsQuery = useQuery({
    queryKey: ["tenant-settings"],
    queryFn: getTenantSettings,
    staleTime: 5 * 60_000,
  });
  const tz = settingsQuery.data?.timeZoneId || "UTC";

  const teacherName = useMemo(() => {
    const m = new Map<string, string>();
    for (const t of teachersQuery.data?.items ?? []) m.set(t.id, t.displayName);
    return m;
  }, [teachersQuery.data]);
  const roomName = useMemo(() => {
    const m = new Map<string, string>();
    for (const r of roomsQuery.data ?? []) m.set(r.id, r.name);
    return m;
  }, [roomsQuery.data]);

  const teacherOptions = useMemo(
    () =>
      (teachersQuery.data?.items ?? []).map((t) => ({
        value: t.id,
        label: t.displayName,
      })),
    [teachersQuery.data],
  );
  const roomOptions = useMemo(
    () => (roomsQuery.data ?? []).map((r) => ({ value: r.id, label: r.name })),
    [roomsQuery.data],
  );

  const invalidateTemplates = () =>
    void queryClient.invalidateQueries({ queryKey: templatesKey });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteScheduleTemplate(id),
    onSuccess: () => {
      toast.success("Шаблон удалён");
      setDeleteTarget(null);
      invalidateTemplates();
    },
    onError: (err) =>
      toast.error("Не удалось удалить шаблон", { description: describe(err) }),
  });

  const templates = useMemo(() => {
    const list = [...(query.data ?? [])];
    list.sort((a, b) => {
      const di = DAYS_OF_WEEK.indexOf(a.dayOfWeek) - DAYS_OF_WEEK.indexOf(b.dayOfWeek);
      return di !== 0 ? di : a.startTime.localeCompare(b.startTime);
    });
    return list;
  }, [query.data]);

  if (!canView) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="Расписание" title="Шаблоны расписания" />
        <EntityEmpty
          icon={CalendarPlus}
          title="Нет доступа"
          body="Нужно право «Просмотр шаблонов расписания»."
        />
      </div>
    );
  }

  return (
    <div className="pb-12">
      <EntityDetailBack to={`/study-groups/${studyGroupId}`} label="К конструктору группы" />

      <PageHero
        eyebrow="Расписание"
        title="Шаблоны расписания группы"
        subtitle={
          groupQuery.data
            ? `${groupQuery.data.code} — ${groupQuery.data.name}. Регулярные слоты, из которых генерируются занятия.`
            : "Регулярные слоты, из которых генерируются занятия."
        }
        actions={
          canManage ? (
            <Button size="sm" className="gap-1.5" onClick={() => setCreateOpen(true)}>
              <CalendarPlus className="h-3.5 w-3.5" />
              Новый шаблон
            </Button>
          ) : undefined
        }
      />

      {query.isError && (
        <div className="mt-4">
          <ErrorBand message={describe(query.error)} />
        </div>
      )}

      <div className="mt-4 space-y-4">
        <EntityDetailSection
          title="Слоты"
          icon={CalendarPlus}
          description="Пустой преподаватель означает «берётся основной преподаватель группы»."
        >
          {query.isLoading ? (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">Загрузка…</p>
          ) : templates.length === 0 ? (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">
              Шаблонов пока нет. Добавьте слот, затем сделайте предпросмотр генерации.
            </p>
          ) : (
            <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
              {templates.map((t) => (
                <li
                  key={t.id}
                  className="flex flex-wrap items-center justify-between gap-3 py-3 first:pt-0 last:pb-0"
                >
                  <div className="min-w-0">
                    <p className="text-[13px] font-medium text-[var(--color-foreground)]">
                      {DAY_OF_WEEK_LABEL[t.dayOfWeek]}, {trimSeconds(t.startTime)} ·{" "}
                      {t.durationMinutes} мин{" "}
                      {!t.isActive && (
                        <EntityStatusBadge tone="default">неактивен</EntityStatusBadge>
                      )}
                    </p>
                    <p className="text-[11.5px] text-[var(--color-muted-foreground)]">
                      {t.teacherId
                        ? teacherName.get(t.teacherId) ?? t.teacherId.slice(0, 8)
                        : "основной преподаватель группы"}
                      {" · "}
                      {t.roomId
                        ? roomName.get(t.roomId) ?? t.roomId.slice(0, 8)
                        : "без аудитории"}
                      {" · с "}
                      {formatDate(t.validFrom)}
                      {t.validTo ? ` по ${formatDate(t.validTo)}` : ""}
                    </p>
                  </div>
                  <div className="flex shrink-0 items-center gap-1">
                    {canGenerate && (
                      <Button
                        variant="outline"
                        size="sm"
                        className="gap-1.5"
                        onClick={() => setPreviewId((p) => (p === t.id ? null : t.id))}
                      >
                        <Eye className="size-3.5" />
                        Предпросмотр
                      </Button>
                    )}
                    {canManage && (
                      <>
                        <Button
                          variant="ghost"
                          size="sm"
                          aria-label="Изменить шаблон"
                          onClick={() => setEditTarget(t)}
                        >
                          <Pencil className="size-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          aria-label="Удалить шаблон"
                          className="text-[var(--color-destructive)]"
                          onClick={() => setDeleteTarget(t)}
                        >
                          <Trash2 className="size-3.5" />
                        </Button>
                      </>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </EntityDetailSection>

        {previewId && (
          <GenerationPreviewPanel
            key={previewId}
            scheduleTemplateId={previewId}
            timeZoneId={tz}
            canGenerate={canGenerate}
            onGenerated={() => {
              setPreviewId(null);
              void queryClient.invalidateQueries({ queryKey: ["sessions"] });
            }}
          />
        )}
      </div>

      {(createOpen || editTarget) && (
        <TemplateDialog
          studyGroupId={studyGroupId}
          template={editTarget}
          groupPrimaryTeacherId={groupQuery.data?.primaryTeacherId ?? null}
          teacherOptions={teacherOptions}
          roomOptions={roomOptions}
          teacherName={teacherName}
          onClose={() => {
            setCreateOpen(false);
            setEditTarget(null);
          }}
          onSaved={invalidateTemplates}
        />
      )}

      {deleteTarget && (
        <Dialog open onOpenChange={(o) => !o && setDeleteTarget(null)}>
          <DialogContent className="!max-w-md">
            <DialogHeader>
              <DialogTitle>Удалить шаблон?</DialogTitle>
              <DialogDescription>
                {DAY_OF_WEEK_LABEL[deleteTarget.dayOfWeek]},{" "}
                {trimSeconds(deleteTarget.startTime)}. Уже созданные занятия не
                удаляются.
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

// ───────────────────────────────────────────────────────────────────────
//  Generation preview panel
// ───────────────────────────────────────────────────────────────────────

function GenerationPreviewPanel({
  scheduleTemplateId,
  timeZoneId,
  canGenerate,
  onGenerated,
}: {
  scheduleTemplateId: string;
  timeZoneId: string;
  canGenerate: boolean;
  onGenerated: () => void;
}) {
  const [horizonWeeks, setHorizonWeeks] = useState("8");
  const weeks = Number.parseInt(horizonWeeks, 10);
  const validWeeks = !Number.isNaN(weeks) && weeks > 0 && weeks <= 52;

  const previewQuery = useQuery({
    queryKey: ["schedule-preview", scheduleTemplateId, validWeeks ? weeks : 8],
    queryFn: () => previewGeneration(scheduleTemplateId, validWeeks ? weeks : 8),
  });

  const generateMutation = useMutation({
    mutationFn: () => generateSessions(scheduleTemplateId, validWeeks ? weeks : 8),
    onSuccess: (res) => {
      toast.success(
        `Создано занятий: ${res.createdSessionIds.length}` +
          (res.skipped.length ? `, пропущено: ${res.skipped.length}` : ""),
      );
      onGenerated();
    },
    onError: (err) =>
      toast.error("Не удалось сгенерировать занятия", { description: describe(err) }),
  });

  const preview: GenerationPreviewDto | undefined = previewQuery.data;

  return (
    <EntityDetailSection
      title="Предпросмотр генерации"
      icon={Sparkles}
      description="Ничего не создаётся, пока вы не нажмёте «Применить»."
      action={
        <div className="flex items-center gap-2 text-[12px] text-[var(--color-muted-foreground)]">
          <span>Горизонт, недель</span>
          <Input
            type="number"
            min="1"
            max="52"
            aria-label="Горизонт генерации, недель"
            value={horizonWeeks}
            onChange={(e) => setHorizonWeeks(e.target.value)}
            className="h-8 w-20 tabular-nums"
          />
        </div>
      }
    >
      {previewQuery.isLoading ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">Считаем…</p>
      ) : previewQuery.isError ? (
        <ErrorBand message={describe(previewQuery.error)} />
      ) : preview ? (
        <div className="space-y-4">
          <div>
            <p className="mb-1.5 text-[12px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
              К созданию: {preview.toCreate.length}
            </p>
            {preview.toCreate.length === 0 ? (
              <p className="text-[13px] text-[var(--color-muted-foreground)]">
                Нет ни одного нового занятия в этом горизонте.
              </p>
            ) : (
              <ul className="flex flex-wrap gap-1.5">
                {preview.toCreate.map((c) => (
                  <li
                    key={c.startUtc}
                    className="rounded-md bg-[oklch(from_var(--color-primary)_l_c_h_/_0.08)] px-2 py-1 text-[11.5px] text-[var(--color-foreground)]"
                  >
                    {formatZonedDateTime(c.startUtc, timeZoneId)}
                  </li>
                ))}
              </ul>
            )}
          </div>

          {preview.skipped.length > 0 && (
            <div>
              <p className="mb-1.5 text-[12px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                Пропущено: {preview.skipped.length}
              </p>
              <ul className="space-y-1.5">
                {preview.skipped.map((s) => (
                  <li
                    key={`${s.localDate}-${s.reason}`}
                    className="rounded-md border border-[var(--color-border)] px-2.5 py-1.5 text-[12px]"
                  >
                    <span className="font-medium text-[var(--color-foreground)]">
                      {formatDate(s.localDate)}
                    </span>{" "}
                    <EntityStatusBadge
                      tone={s.reason === "Conflict" ? "danger" : "warning"}
                    >
                      {SKIP_REASON_LABEL[s.reason]}
                    </EntityStatusBadge>
                    {s.reason === "Conflict" && s.conflicts.length > 0 && (
                      <ul className="ml-1 mt-1 list-disc pl-4 text-[11.5px] text-[var(--color-muted-foreground)]">
                        {s.conflicts.map((cf) => (
                          <li key={cf.conflictingSessionId}>
                            {CONFLICT_TYPE_LABEL[cf.type]} —{" "}
                            <Link
                              to={`/sessions/${cf.conflictingSessionId}`}
                              className="hover:text-[var(--color-foreground)] hover:underline"
                            >
                              занятие в {formatZonedDateTime(
                                cf.conflictingSessionStartUtc,
                                timeZoneId,
                              )}
                            </Link>
                          </li>
                        ))}
                      </ul>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          )}

          <div className="flex items-center justify-end gap-2 border-t border-[var(--color-border)] pt-3">
            <Button
              disabled={
                !canGenerate ||
                generateMutation.isPending ||
                preview.toCreate.length === 0
              }
              onClick={() => generateMutation.mutate()}
              className="gap-1.5"
            >
              <Sparkles className="h-4 w-4" />
              {generateMutation.isPending
                ? "Генерация…"
                : `Применить (${preview.toCreate.length})`}
            </Button>
          </div>
        </div>
      ) : null}
    </EntityDetailSection>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Create / edit dialog
// ───────────────────────────────────────────────────────────────────────

function TemplateDialog({
  studyGroupId,
  template,
  groupPrimaryTeacherId,
  teacherOptions,
  roomOptions,
  teacherName,
  onClose,
  onSaved,
}: {
  studyGroupId: string;
  template: ScheduleTemplateDto | null;
  groupPrimaryTeacherId: string | null;
  teacherOptions: { value: string; label: string }[];
  roomOptions: { value: string; label: string }[];
  teacherName: Map<string, string>;
  onClose: () => void;
  onSaved: () => void;
}) {
  const editing = !!template;
  const [dayOfWeek, setDayOfWeek] = useState<DayOfWeekName>(
    template?.dayOfWeek ?? "Monday",
  );
  const [startTime, setStartTime] = useState(
    template ? trimSeconds(template.startTime) : "18:00",
  );
  const [durationMinutes, setDurationMinutes] = useState(
    String(template?.durationMinutes ?? 90),
  );
  const [roomId, setRoomId] = useState<string | null>(template?.roomId ?? null);
  const [teacherId, setTeacherId] = useState<string | null>(template?.teacherId ?? null);
  const [validFrom, setValidFrom] = useState(template?.validFrom ?? "");
  const [validTo, setValidTo] = useState(template?.validTo ?? "");
  const [isActive, setIsActive] = useState(template?.isActive ?? true);

  useEffect(() => {
    if (!template) return;
    setDayOfWeek(template.dayOfWeek);
    setStartTime(trimSeconds(template.startTime));
    setDurationMinutes(String(template.durationMinutes));
    setRoomId(template.roomId ?? null);
    setTeacherId(template.teacherId ?? null);
    setValidFrom(template.validFrom);
    setValidTo(template.validTo ?? "");
    setIsActive(template.isActive);
  }, [template]);

  const createMutation = useMutation({
    mutationFn: (input: CreateScheduleTemplateInput) => createScheduleTemplate(input),
    onSuccess: () => {
      toast.success("Шаблон создан");
      onSaved();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось создать шаблон", { description: describe(err) }),
  });
  const updateMutation = useMutation({
    mutationFn: (input: UpdateScheduleTemplateInput) => updateScheduleTemplate(input),
    onSuccess: () => {
      toast.success("Шаблон обновлён");
      onSaved();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось обновить шаблон", { description: describe(err) }),
  });

  const dur = Number.parseInt(durationMinutes, 10);
  const valid =
    !!startTime && !Number.isNaN(dur) && dur > 0 && validFrom.length > 0;
  const pending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    const base = {
      studyGroupId,
      dayOfWeek,
      startTime,
      durationMinutes: dur,
      roomId,
      teacherId,
      validFrom,
      validTo: validTo || null,
    };
    if (editing && template) {
      updateMutation.mutate({
        ...base,
        scheduleTemplateId: template.id,
        isActive,
      });
    } else {
      createMutation.mutate(base);
    }
  };

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{editing ? "Изменить шаблон" : "Новый шаблон"}</DialogTitle>
            <DialogDescription>
              День недели, локальное время начала и длительность. Аудитория и
              преподаватель — необязательны.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-3">
              <Field id="tpl-day" label="День недели" required>
                <Combobox
                  id="tpl-day"
                  label="День недели"
                  value={dayOfWeek}
                  onChange={(v) => setDayOfWeek((v as DayOfWeekName) ?? "Monday")}
                  options={DAYS_OF_WEEK.map((d) => ({
                    value: d,
                    label: DAY_OF_WEEK_LABEL[d],
                  }))}
                />
              </Field>
              <Field id="tpl-time" label="Начало" required>
                <Input
                  id="tpl-time"
                  type="time"
                  value={startTime}
                  onChange={(e) => setStartTime(e.target.value)}
                  required
                />
              </Field>
              <Field id="tpl-dur" label="Длит., мин" required>
                <Input
                  id="tpl-dur"
                  type="number"
                  min="1"
                  value={durationMinutes}
                  onChange={(e) => setDurationMinutes(e.target.value)}
                  required
                  className="tabular-nums"
                />
              </Field>
            </div>

            <Field
              id="tpl-teacher"
              label="Преподаватель"
              hint={
                teacherId
                  ? undefined
                  : groupPrimaryTeacherId
                    ? `Пусто — будет взят основной преподаватель группы (${
                        teacherName.get(groupPrimaryTeacherId) ??
                        groupPrimaryTeacherId.slice(0, 8)
                      })`
                    : "Пусто — будет взят основной преподаватель группы"
              }
            >
              <Combobox
                id="tpl-teacher"
                label="Преподаватель"
                value={teacherId}
                onChange={setTeacherId}
                options={teacherOptions}
                searchable
                clearable
              />
            </Field>

            <Field id="tpl-room" label="Аудитория" hint="Пусто — без аудитории">
              <Combobox
                id="tpl-room"
                label="Аудитория"
                value={roomId}
                onChange={setRoomId}
                options={roomOptions}
                searchable
                clearable
              />
            </Field>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="tpl-from" label="Действует с" required>
                <Input
                  id="tpl-from"
                  type="date"
                  value={validFrom}
                  onChange={(e) => setValidFrom(e.target.value)}
                  required
                />
              </Field>
              <Field id="tpl-to" label="Действует по">
                <Input
                  id="tpl-to"
                  type="date"
                  value={validTo}
                  onChange={(e) => setValidTo(e.target.value)}
                />
              </Field>
            </div>

            {editing && (
              <label
                htmlFor="tpl-active"
                className="flex items-center gap-2 text-[13px] text-[var(--color-foreground)]"
              >
                <Switch
                  id="tpl-active"
                  checked={isActive}
                  onCheckedChange={setIsActive}
                />
                Шаблон активен
              </label>
            )}
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
