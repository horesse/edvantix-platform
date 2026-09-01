import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { z } from "zod";
import { CreditCard, Gauge } from "lucide-react";
import { toast } from "sonner";
import {
  createPlan,
  updatePlan,
  type BillingPlanDto,
  type PlanInterval,
  type QuotaResource,
} from "@/api/billing";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field, Select, type SelectOption } from "@/components/list";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const PLAN_KEY_PATTERN = /^[a-z0-9][a-z0-9-]{0,62}[a-z0-9]$/;

// A money/rate field is a free-text decimal string. These refinements run
// client-side so a negative price is rejected before any network call (the
// server also rejects it, but we don't rely on that).
const NON_NEGATIVE_MSG = "Должно быть неотрицательным числом.";

/** Required non-negative decimal (e.g. monthly base price). */
const requiredNonNegative = z
  .string()
  .trim()
  .min(1, "Обязательное поле.")
  .refine((v) => Number.isFinite(Number(v)) && Number(v) >= 0, NON_NEGATIVE_MSG);

/** Optional non-negative decimal (blank allowed → omitted). */
const optionalNonNegative = z
  .string()
  .trim()
  .refine((v) => v === "" || (Number.isFinite(Number(v)) && Number(v) >= 0), NON_NEGATIVE_MSG);

const INTERVAL_OPTIONS: SelectOption<PlanInterval>[] = [
  { value: "Monthly", label: "Ежемесячно", hint: "списание каждый месяц" },
  { value: "Yearly", label: "Ежегодно", hint: "списание раз в 12 месяцев" },
];

// School-domain resources first (the units a school actually thinks in),
// then the infrastructure gauges. Labels are in school terms — "Ученики" /
// "Преподаватели", not "Users".
const OVERAGE_RESOURCES: { key: QuotaResource; label: string; placeholder: string }[] = [
  { key: "ActiveStudents", label: "Ученики", placeholder: "150.00" },
  { key: "ActiveTeachers", label: "Преподаватели", placeholder: "400.00" },
  { key: "StudyGroups", label: "Учебные группы", placeholder: "100.00" },
  { key: "MonthlySessions", label: "Занятий в месяц", placeholder: "5.00" },
  { key: "StorageBytes", label: "Объём файлов (за байт)", placeholder: "0.00000001" },
  { key: "ApiCalls", label: "Вызовы API", placeholder: "0.0010" },
  { key: "Users", label: "Учётные записи", placeholder: "5.00" },
  { key: "ActiveFeatureFlags", label: "Флаги функций", placeholder: "1.00" },
];

type OverageState = Record<string, string>;

function toOverageNumbers(state: OverageState): Record<string, number> | null {
  const out: Record<string, number> = {};
  let any = false;
  for (const { key } of OVERAGE_RESOURCES) {
    const raw = state[key];
    if (raw === undefined || raw.trim() === "") continue;
    const n = Number(raw);
    // Submission is blocked upstream when a value is invalid, so anything that
    // reaches here is a non-negative finite number.
    if (!Number.isFinite(n) || n < 0) continue;
    out[key] = n;
    any = true;
  }
  return any ? out : null;
}

/** First validation message for a value against a schema, or undefined when valid. */
function fieldError(schema: z.ZodTypeAny, value: string): string | undefined {
  const result = schema.safeParse(value);
  return result.success ? undefined : result.error.issues[0]?.message;
}

function describe(err: unknown, fallback: string): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return fallback;
}

function SectionLabel({
  icon: Icon,
  title,
  description,
}: {
  icon: React.ComponentType<{ className?: string }>;
  title: string;
  description: string;
}) {
  return (
    <div className="flex items-start gap-2.5 pb-1">
      <span
        aria-hidden
        className="mt-0.5 grid h-6 w-6 shrink-0 place-items-center rounded-md bg-[var(--color-accent)] text-[var(--color-muted-foreground)]"
      >
        <Icon className="h-3.5 w-3.5" />
      </span>
      <div className="min-w-0">
        <p className="text-[12.5px] font-semibold text-[var(--color-foreground)]">{title}</p>
        <p className="text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">{description}</p>
      </div>
    </div>
  );
}

/**
 * Create or edit a billing plan in a dialog. Pass `plan` to edit (key + currency are immutable then),
 * omit it to create. On success it invalidates the plans cache and closes.
 */
export function PlanFormDialog({
  open,
  onOpenChange,
  plan,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  plan?: BillingPlanDto;
}) {
  const queryClient = useQueryClient();
  const isEdit = !!plan;

  const [key, setKey] = useState("");
  const [name, setName] = useState("");
  const [currency, setCurrency] = useState("USD");
  const [description, setDescription] = useState("");
  const [monthlyBasePrice, setMonthlyBasePrice] = useState("");
  const [interval, setInterval] = useState<PlanInterval>("Monthly");
  const [annualPrice, setAnnualPrice] = useState("");
  const [overage, setOverage] = useState<OverageState>({});

  // Reset/populate whenever the dialog opens (or the target plan changes).
  useEffect(() => {
    if (!open) return;
    setKey(plan?.key ?? "");
    setName(plan?.name ?? "");
    setCurrency(plan?.currency ?? "USD");
    setDescription(plan?.description ?? "");
    setMonthlyBasePrice(plan ? String(plan.monthlyBasePrice) : "");
    setInterval(plan?.interval === "Yearly" ? "Yearly" : "Monthly");
    setAnnualPrice(plan?.annualPrice != null ? String(plan.annualPrice) : "");
    const next: OverageState = {};
    for (const [resource, rate] of Object.entries(plan?.overageRates ?? {})) {
      if (rate !== undefined && rate !== null) next[resource] = String(rate);
    }
    setOverage(next);
  }, [open, plan]);

  const keyInvalid = !isEdit && key.length > 0 && !PLAN_KEY_PATTERN.test(key);
  const priceNum = Number(monthlyBasePrice);
  // Only surface the price error once something's been typed; submit-time
  // validation (onSubmit) still blocks an empty required field.
  const priceError =
    monthlyBasePrice.length > 0 ? fieldError(requiredNonNegative, monthlyBasePrice) : undefined;
  const annualNum = Number(annualPrice);
  const annualError = fieldError(optionalNonNegative, annualPrice);
  const annualPricePayload = interval === "Yearly" && annualPrice.trim().length > 0 ? annualNum : null;
  const descriptionPayload = description.trim().length > 0 ? description.trim() : null;

  // Per-resource overage validation — a negative or non-numeric rate blocks submit.
  const overageErrors = useMemo(() => {
    const out: Partial<Record<string, string>> = {};
    for (const { key: resKey } of OVERAGE_RESOURCES) {
      const err = fieldError(optionalNonNegative, overage[resKey] ?? "");
      if (err) out[resKey] = err;
    }
    return out;
  }, [overage]);
  const hasOverageError = Object.keys(overageErrors).length > 0;
  // Aggregate validity for disabling submit. Monthly price is required + non-negative.
  const pricingInvalid =
    !!fieldError(requiredNonNegative, monthlyBasePrice) || !!annualError || hasOverageError;

  const onClose = () => onOpenChange(false);

  const createMutation = useMutation({
    mutationFn: createPlan,
    onSuccess: () => {
      toast.success(`Тариф «${name}» создан`);
      queryClient.invalidateQueries({ queryKey: ["billing", "plans"] });
      onClose();
    },
    onError: (err) => toast.error("Не удалось создать", { description: describe(err, "Не удалось создать тариф.") }),
  });

  const updateMutation = useMutation({
    mutationFn: updatePlan,
    onSuccess: () => {
      toast.success(`Тариф «${name}» обновлён`);
      queryClient.invalidateQueries({ queryKey: ["billing", "plans"] });
      onClose();
    },
    onError: (err) => toast.error("Не удалось сохранить", { description: describe(err, "Не удалось обновить тариф.") }),
  });

  const pending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (pricingInvalid) return;
    const overageRates = toOverageNumbers(overage);

    if (isEdit && plan) {
      updateMutation.mutate({
        planId: plan.id,
        name: name.trim(),
        monthlyBasePrice: priceNum,
        overageRates,
        interval,
        annualPrice: annualPricePayload,
        description: descriptionPayload,
      });
      return;
    }
    if (keyInvalid) return;
    createMutation.mutate({
      key: key.trim(),
      name: name.trim(),
      currency: currency.trim().toUpperCase(),
      monthlyBasePrice: priceNum,
      overageRates,
      interval,
      annualPrice: annualPricePayload,
      description: descriptionPayload,
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent size="lg">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className="grid h-9 w-9 shrink-0 place-items-center rounded-xl
                bg-[oklch(from_var(--color-primary)_l_c_h_/_0.12)] text-[var(--color-primary)]
                ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]"
            >
              <CreditCard className="h-[18px] w-[18px]" />
            </span>
            <DialogTitle className="text-[16px]">{isEdit ? "Изменить тариф" : "Новый тариф"}</DialogTitle>
          </div>
          <DialogDescription className="mt-1">
            {isEdit
              ? "Измените название, описание, цены, период или ставки за превышение. Ключ и валюта неизменны."
              : "Ключ тарифа — канонический слаг, на который ссылаются подписки школ и настройки квот."}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit}>
          <DialogBody className="space-y-6">
            {/* ── Details ── */}
            <div className="space-y-3">
              <SectionLabel
                icon={CreditCard}
                title="Параметры тарифа"
                description="Идентификатор и цены. Период задаёт длину срока и частоту списаний со школы."
              />
              <div className="h-px bg-[var(--color-border)] opacity-60" />
              <div className="grid gap-4 sm:grid-cols-2">
                <Field
                  id="pf-key"
                  label="Ключ"
                  hint="Слаг в нижнем регистре (например, «pro», «team-2025»). Неизменяем."
                  required={!isEdit}
                  error={keyInvalid ? "Некорректный слаг." : undefined}
                >
                  <Input
                    id="pf-key"
                    value={key}
                    onChange={(e) => setKey(e.target.value)}
                    placeholder="pro"
                    className="font-mono"
                    disabled={isEdit}
                    autoComplete="off"
                  />
                </Field>
                <Field id="pf-name" label="Отображаемое название" required>
                  <Input id="pf-name" value={name} onChange={(e) => setName(e.target.value)} placeholder="Pro" />
                </Field>
                <Field id="pf-currency" label="Валюта" hint="ISO 4217. Неизменяема." required={!isEdit}>
                  <Input
                    id="pf-currency"
                    value={currency}
                    onChange={(e) => setCurrency(e.target.value.toUpperCase())}
                    placeholder="USD"
                    className="font-mono"
                    disabled={isEdit}
                    autoComplete="off"
                  />
                </Field>
                <Field
                  id="pf-monthlyBasePrice"
                  label="Базовая цена в месяц"
                  hint="Каноническая месячная ставка; для месячных тарифов — цена за срок."
                  required
                  error={priceError}
                >
                  <Input
                    id="pf-monthlyBasePrice"
                    value={monthlyBasePrice}
                    onChange={(e) => setMonthlyBasePrice(e.target.value)}
                    inputMode="decimal"
                    placeholder="29.00"
                  />
                </Field>
                <Field id="pf-interval" label="Период списания" required>
                  <Select<PlanInterval>
                    id="pf-interval"
                    value={interval}
                    onValueChange={(v) => setInterval(v === "Yearly" ? "Yearly" : "Monthly")}
                    options={INTERVAL_OPTIONS}
                  />
                </Field>
                {interval === "Yearly" && (
                  <Field
                    id="pf-annualPrice"
                    label="Цена за год"
                    hint="За годовой срок. Пусто → 12× месячной."
                    error={annualError}
                  >
                    <Input
                      id="pf-annualPrice"
                      value={annualPrice}
                      onChange={(e) => setAnnualPrice(e.target.value)}
                      inputMode="decimal"
                      placeholder={monthlyBasePrice ? String(Number(monthlyBasePrice) * 12) : "290.00"}
                    />
                  </Field>
                )}
              </div>
              <Field
                id="pf-description"
                label="Описание"
                hint="Короткий текст для операторской карточки тарифа. Необязательно."
              >
                <textarea
                  id="pf-description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={2}
                  placeholder="Например: до 150 учеников и 12 преподавателей, безлимитные группы."
                  className={cn(
                    "w-full min-w-0 rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2",
                    "text-sm shadow-xs outline-none placeholder:text-[var(--color-muted-foreground)]",
                    "transition-[color,box-shadow,border-color,background-color] duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                    "dark:bg-[oklch(from_var(--color-input)_l_c_h_/_0.3)]",
                    "focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
                  )}
                />
              </Field>
            </div>

            {/* ── Overage rates ── */}
            <div className="space-y-3">
              <SectionLabel
                icon={Gauge}
                title="Ставки за превышение"
                description="Цена за единицу сверх лимита тарифа — по ученикам, преподавателям, группам и занятиям. Пустое поле — ресурс пропускается."
              />
              <div className="h-px bg-[var(--color-border)] opacity-60" />
              <div className="grid gap-4 sm:grid-cols-2">
                {OVERAGE_RESOURCES.map((res) => (
                  <Field
                    key={res.key}
                    id={`pf-overage-${res.key}`}
                    label={res.label}
                    error={overageErrors[res.key]}
                  >
                    <Input
                      id={`pf-overage-${res.key}`}
                      value={overage[res.key] ?? ""}
                      onChange={(e) => setOverage((s) => ({ ...s, [res.key]: e.target.value }))}
                      inputMode="decimal"
                      placeholder={res.placeholder}
                    />
                  </Field>
                ))}
              </div>
            </div>
          </DialogBody>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={pending}>
              Отмена
            </Button>
            <Button type="submit" disabled={pending || keyInvalid || pricingInvalid}>
              {pending ? "Сохранение…" : isEdit ? "Сохранить" : "Создать тариф"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
