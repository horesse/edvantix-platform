import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { BadgePercent, Ban, Pencil, Plus, Wallet } from "lucide-react";
import { toast } from "sonner";
import {
  createTariff,
  deactivateTariff,
  getTariffs,
  TARIFF_KINDS,
  updateTariff,
  type CreateTariffInput,
  type TariffDto,
  type TariffKind,
  type UpdateTariffInput,
} from "@/api/payments";
import { searchCourses } from "@/api/curriculum";
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
  EntityEmpty,
  EntityFilterPill,
  EntityStatusBadge,
  ErrorBand,
  Field,
  PageHero,
} from "@/components/list";
import { describe } from "@/lib/list-helpers";
import {
  formatMoney,
  TARIFF_KIND_HINT,
  TARIFF_KIND_LABEL,
} from "./payments-ui";

type ActiveFilter = "all" | "active" | "inactive";

export function TariffsPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canView = perms.includes("Permissions.Payments.Tariffs.View");
  const canManage = perms.includes("Permissions.Payments.Tariffs.Manage");
  const queryClient = useQueryClient();

  const [activeFilter, setActiveFilter] = useState<ActiveFilter>("all");
  const [createOpen, setCreateOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<TariffDto | null>(null);
  const [deactivateTarget, setDeactivateTarget] = useState<TariffDto | null>(null);

  const isActive =
    activeFilter === "all" ? null : activeFilter === "active" ? true : false;

  const query = useQuery({
    queryKey: ["tariffs", { isActive }],
    queryFn: () => getTariffs(isActive),
    enabled: canView,
  });

  const coursesQuery = useQuery({
    queryKey: ["courses", { pageSize: 100, for: "tariffs" }],
    queryFn: () => searchCourses({ pageSize: 100 }),
    staleTime: 60_000,
    enabled: canView,
  });
  const courseName = useMemo(() => {
    const m = new Map<string, string>();
    for (const c of coursesQuery.data?.items ?? []) m.set(c.id, c.title);
    return m;
  }, [coursesQuery.data]);

  const invalidate = () =>
    void queryClient.invalidateQueries({ queryKey: ["tariffs"] });

  const deactivateMut = useMutation({
    mutationFn: (id: string) => deactivateTariff(id),
    onSuccess: () => {
      toast.success("Тариф деактивирован");
      setDeactivateTarget(null);
      invalidate();
    },
    onError: (err) =>
      toast.error("Не удалось деактивировать тариф", { description: describe(err) }),
  });

  if (!canView) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="Оплаты" title="Тарифы" />
        <EntityEmpty
          icon={Wallet}
          title="Нет доступа"
          body="Нужно право «Просмотр тарифов»."
        />
      </div>
    );
  }

  const tariffs = [...(query.data ?? [])].sort((a, b) =>
    a.name.localeCompare(b.name, "ru"),
  );

  return (
    <div className="space-y-4">
      <PageHero
        eyebrow="Оплаты"
        title="Тарифы"
        subtitle="Правила, по которым посещаемость превращается в счёт. Вид тарифа и валюта фиксируются при создании и не меняются."
        actions={
          canManage ? (
            <Button
              size="sm"
              className="gap-1.5"
              onClick={() => setCreateOpen(true)}
            >
              <Plus className="h-3.5 w-3.5" />
              Новый тариф
            </Button>
          ) : undefined
        }
      />

      <EntityFilterPill<ActiveFilter>
        label="Статус"
        value={activeFilter}
        onChange={setActiveFilter}
        options={[
          { value: "all", label: "Все" },
          { value: "active", label: "Активные" },
          { value: "inactive", label: "Архив" },
        ]}
      />

      {query.isError && <ErrorBand message={describe(query.error)} />}

      {query.isLoading ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : tariffs.length === 0 ? (
        <EntityEmpty
          icon={Wallet}
          title="Тарифов пока нет"
          body="Создайте тариф, чтобы выставлять по нему счета ученикам."
          action={
            canManage ? (
              <Button
                onClick={() => setCreateOpen(true)}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                <Plus className="mr-1.5 size-4" />
                Новый тариф
              </Button>
            ) : undefined
          }
        />
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)] rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
          {tariffs.map((t) => (
            <li
              key={t.id}
              className="flex items-center justify-between gap-3 px-4 py-3 first:rounded-t-xl last:rounded-b-xl"
            >
              <div className="min-w-0">
                <p className="flex items-center gap-2 text-[13px] font-medium text-[var(--color-foreground)]">
                  {t.name}
                  <EntityStatusBadge tone={t.isActive ? "success" : "default"}>
                    {t.isActive ? "Активен" : "Архив"}
                  </EntityStatusBadge>
                </p>
                <p className="mt-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
                  {TARIFF_KIND_LABEL[t.kind]} · {formatMoney(t.amount, t.currency)}
                  {t.kind === "PerPackage" &&
                    ` · ${t.lessonsCount} зан.${
                      t.validDays > 0 ? ` / ${t.validDays} дн.` : " / бессрочно"
                    }`}
                  {t.courseId ? ` · ${courseName.get(t.courseId) ?? "курс"}` : ""}
                  {t.chargeOnExcusedAbsence ? " · платно при уваж. пропуске" : ""}
                </p>
              </div>
              {canManage && (
                <div className="flex shrink-0 items-center gap-1">
                  <Button
                    variant="ghost"
                    size="sm"
                    aria-label={`Изменить ${t.name}`}
                    onClick={() => setEditTarget(t)}
                  >
                    <Pencil className="size-3.5" />
                  </Button>
                  {t.isActive && (
                    <Button
                      variant="ghost"
                      size="sm"
                      aria-label={`Деактивировать ${t.name}`}
                      className="text-[var(--color-destructive)]"
                      onClick={() => setDeactivateTarget(t)}
                    >
                      <Ban className="size-3.5" />
                    </Button>
                  )}
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      {(createOpen || editTarget) && (
        <TariffDialog
          tariff={editTarget}
          onClose={() => {
            setCreateOpen(false);
            setEditTarget(null);
          }}
          onSaved={invalidate}
        />
      )}

      {deactivateTarget && (
        <Dialog open onOpenChange={(o) => !o && setDeactivateTarget(null)}>
          <DialogContent className="!max-w-md">
            <DialogHeader>
              <DialogTitle>Деактивировать тариф?</DialogTitle>
              <DialogDescription>
                «{deactivateTarget.name}» перестанет предлагаться при выставлении
                новых счетов. Уже выставленные счета не изменятся.
              </DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <DialogClose asChild>
                <Button
                  type="button"
                  variant="outline"
                  disabled={deactivateMut.isPending}
                >
                  Отмена
                </Button>
              </DialogClose>
              <Button
                variant="destructive"
                disabled={deactivateMut.isPending}
                onClick={() => deactivateMut.mutate(deactivateTarget.id)}
              >
                {deactivateMut.isPending ? "…" : "Деактивировать"}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}

function TariffDialog({
  tariff,
  onClose,
  onSaved,
}: {
  tariff: TariffDto | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const editing = !!tariff;
  const [name, setName] = useState(tariff?.name ?? "");
  const [kind, setKind] = useState<TariffKind>(tariff?.kind ?? "PerLesson");
  const [amount, setAmount] = useState(String(tariff?.amount ?? ""));
  const [currency, setCurrency] = useState(tariff?.currency ?? "RUB");
  const [courseId, setCourseId] = useState<string | null>(tariff?.courseId ?? null);
  const [lessonsCount, setLessonsCount] = useState(
    String(tariff?.lessonsCount ?? 8),
  );
  const [validDays, setValidDays] = useState(String(tariff?.validDays ?? 60));
  const [chargeOnExcused, setChargeOnExcused] = useState(
    tariff?.chargeOnExcusedAbsence ?? false,
  );

  useEffect(() => {
    if (!tariff) return;
    setName(tariff.name);
    setKind(tariff.kind);
    setAmount(String(tariff.amount));
    setCurrency(tariff.currency);
    setCourseId(tariff.courseId ?? null);
    setLessonsCount(String(tariff.lessonsCount));
    setValidDays(String(tariff.validDays));
    setChargeOnExcused(tariff.chargeOnExcusedAbsence);
  }, [tariff]);

  const coursesQuery = useQuery({
    queryKey: ["courses", { pageSize: 100, for: "tariff-form" }],
    queryFn: () => searchCourses({ pageSize: 100 }),
    staleTime: 60_000,
  });
  const courseOptions = useMemo(() => {
    const opts = (coursesQuery.data?.items ?? []).map((c) => ({
      value: c.id,
      label: c.title,
    }));
    if (courseId && !opts.some((o) => o.value === courseId)) {
      opts.unshift({ value: courseId, label: courseId });
    }
    return opts;
  }, [coursesQuery.data, courseId]);

  const createMut = useMutation({
    mutationFn: (input: CreateTariffInput) => createTariff(input),
    onSuccess: () => {
      toast.success("Тариф создан");
      onSaved();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось создать тариф", { description: describe(err) }),
  });
  const updateMut = useMutation({
    mutationFn: (input: UpdateTariffInput) => updateTariff(tariff!.id, input),
    onSuccess: () => {
      toast.success("Тариф обновлён");
      onSaved();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось обновить тариф", { description: describe(err) }),
  });

  const amountNum = Number.parseFloat(amount.replace(",", "."));
  const lessonsNum = Number.parseInt(lessonsCount, 10);
  const validNum = Number.parseInt(validDays, 10);
  const isPackage = kind === "PerPackage";
  const valid =
    name.trim().length > 0 &&
    !Number.isNaN(amountNum) &&
    amountNum >= 0 &&
    (editing || currency.trim().length === 3) &&
    (!isPackage ||
      (!Number.isNaN(lessonsNum) &&
        lessonsNum > 0 &&
        !Number.isNaN(validNum) &&
        validNum >= 0));
  const pending = createMut.isPending || updateMut.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    if (editing) {
      updateMut.mutate({
        name: name.trim(),
        courseId,
        amount: amountNum,
        lessonsCount: isPackage ? lessonsNum : tariff!.lessonsCount,
        validDays: isPackage ? validNum : tariff!.validDays,
        chargeOnExcusedAbsence: chargeOnExcused,
      });
    } else {
      createMut.mutate({
        name: name.trim(),
        courseId,
        kind,
        amount: amountNum,
        currency: currency.trim().toUpperCase(),
        lessonsCount: isPackage ? lessonsNum : 0,
        validDays: isPackage ? validNum : 0,
        chargeOnExcusedAbsence: chargeOnExcused,
      });
    }
  };

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>
              {editing ? "Изменить тариф" : "Новый тариф"}
            </DialogTitle>
            <DialogDescription>
              {editing
                ? "Вид тарифа и валюту изменить нельзя — они зафиксированы при создании."
                : "Вид тарифа и валюта фиксируются при создании и после уже не меняются."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="tf-name" label="Название" required>
              <Input
                id="tf-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                autoFocus
              />
            </Field>

            {!editing && (
              <Field
                id="tf-kind"
                label="Вид тарифа"
                required
                hint={TARIFF_KIND_HINT[kind]}
              >
                <Combobox
                  id="tf-kind"
                  label="Вид тарифа"
                  value={kind}
                  onChange={(v) => setKind((v as TariffKind) ?? "PerLesson")}
                  options={TARIFF_KINDS.map((k) => ({
                    value: k,
                    label: TARIFF_KIND_LABEL[k],
                  }))}
                />
              </Field>
            )}
            {editing && (
              <p className="rounded-lg bg-[var(--color-muted)] px-3 py-2 text-[12px] text-[var(--color-muted-foreground)]">
                Вид: <b>{TARIFF_KIND_LABEL[kind]}</b> · Валюта: <b>{currency}</b>
              </p>
            )}

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="tf-amount" label="Сумма" required>
                <Input
                  id="tf-amount"
                  type="number"
                  min="0"
                  step="0.01"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  required
                  className="tabular-nums"
                />
              </Field>
              {!editing && (
                <Field
                  id="tf-currency"
                  label="Валюта"
                  required
                  hint="Трёхбуквенный код, напр. RUB"
                >
                  <Input
                    id="tf-currency"
                    value={currency}
                    onChange={(e) =>
                      setCurrency(e.target.value.toUpperCase().slice(0, 3))
                    }
                    required
                    maxLength={3}
                    className="uppercase"
                  />
                </Field>
              )}
            </div>

            <Field
              id="tf-course"
              label="Курс"
              hint="Необязательно — тариф-«умолчание» для массового выставления по курсу."
            >
              <Combobox
                id="tf-course"
                label="Курс"
                value={courseId}
                onChange={setCourseId}
                options={courseOptions}
                placeholder={
                  coursesQuery.isLoading ? "Загрузка…" : "Без привязки к курсу"
                }
                searchable
                clearable
              />
            </Field>

            {isPackage && (
              <div className="grid gap-3 sm:grid-cols-2">
                <Field
                  id="tf-lessons"
                  label="Занятий в пакете"
                  required
                  hint="Только для пакетного тарифа."
                >
                  <Input
                    id="tf-lessons"
                    type="number"
                    min="1"
                    value={lessonsCount}
                    onChange={(e) => setLessonsCount(e.target.value)}
                    required
                    className="tabular-nums"
                  />
                </Field>
                <Field
                  id="tf-valid"
                  label="Срок действия, дней"
                  required
                  hint="0 — пакет бессрочный."
                >
                  <Input
                    id="tf-valid"
                    type="number"
                    min="0"
                    value={validDays}
                    onChange={(e) => setValidDays(e.target.value)}
                    required
                    className="tabular-nums"
                  />
                </Field>
              </div>
            )}

            <label
              htmlFor="tf-excused"
              className="flex items-center gap-2 text-[13px] text-[var(--color-foreground)]"
            >
              <Switch
                id="tf-excused"
                checked={chargeOnExcused}
                onCheckedChange={setChargeOnExcused}
              />
              Начислять и за уважительный пропуск
            </label>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={pending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={pending || !valid} className="gap-1.5">
              <BadgePercent className="h-4 w-4" />
              {pending ? "Сохранение…" : editing ? "Сохранить" : "Создать"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
