import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CalendarX2, Clock, DoorOpen, Hash, Landmark, Lock } from "lucide-react";
import { toast } from "sonner";
import {
  DEFAULT_INVOICE_NUMBER_TEMPLATE,
  getTenantSettings,
  updateTenantSettings,
  type TenantSettingsDto,
} from "@/api/tenant-settings";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import { Combobox, Field, PageHero, type ComboboxOption } from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { SettingsSection } from "./settings-layout";

const HERO = {
  eyebrow: "Школа",
  title: "Настройки школы",
  subtitle:
    "Часовой пояс, валюта и нумерация счетов. Применяется ко всей школе, не к вашему профилю.",
} as const;

// ── Option sources ─────────────────────────────────────────────────────────
// `Intl.supportedValuesOf` is available in every browser we target and in the
// Vite dev/build toolchain, but guard it anyway — a stale runtime just falls
// back to a curated shortlist, and the current server value is always merged
// in so the picker can never fail to show what the school already has.

const FALLBACK_TIME_ZONES = [
  "UTC",
  "Europe/Kaliningrad",
  "Europe/Moscow",
  "Europe/Samara",
  "Asia/Yekaterinburg",
  "Asia/Omsk",
  "Asia/Novosibirsk",
  "Asia/Krasnoyarsk",
  "Asia/Irkutsk",
  "Asia/Yakutsk",
  "Asia/Vladivostok",
  "Asia/Almaty",
  "Asia/Tashkent",
  "Asia/Tbilisi",
  "Asia/Yerevan",
  "Asia/Baku",
  "Europe/Minsk",
  "Europe/Kyiv",
];

const FALLBACK_CURRENCIES = [
  "RUB", "USD", "EUR", "KZT", "BYN", "UAH", "GEL", "AMD", "AZN", "UZS", "KGS", "TJS", "GBP",
];

function supported(kind: "timeZone" | "currency", fallback: string[]): string[] {
  try {
    const fn = (Intl as unknown as {
      supportedValuesOf?: (k: string) => string[];
    }).supportedValuesOf;
    const list = fn?.(kind);
    if (Array.isArray(list) && list.length > 0) return list;
  } catch {
    /* fall through */
  }
  return fallback;
}

function withCurrent(list: string[], current: string | undefined): string[] {
  if (!current || list.includes(current)) return list;
  return [current, ...list];
}

function toOptions(values: string[], describeFn?: (v: string) => string | undefined): ComboboxOption[] {
  return values.map((v) => ({ value: v, label: v, hint: describeFn?.(v) }));
}

let currencyNames: Intl.DisplayNames | null = null;
try {
  currencyNames = new Intl.DisplayNames(["ru"], { type: "currency" });
} catch {
  currencyNames = null;
}
function currencyLabel(code: string): string | undefined {
  try {
    return currencyNames?.of(code) ?? undefined;
  } catch {
    return undefined;
  }
}

/** Current UTC offset of an IANA zone, formatted `UTC+3` / `UTC−4:30`. */
function zoneOffsetLabel(zone: string): string | undefined {
  try {
    const dtf = new Intl.DateTimeFormat("en-US", { timeZone: zone, timeZoneName: "shortOffset" });
    const part = dtf.formatToParts(new Date()).find((p) => p.type === "timeZoneName");
    return part?.value.replace("GMT", "UTC");
  } catch {
    return undefined;
  }
}

// ── Invoice-number template ───────────────────────────────────────────────
// Mirrors Payments' `InvoiceNumberFormat` (Render / IsValid) so the editor can
// validate and preview without a round-trip. The backend re-validates on PUT
// (`UpdateTenantSettingsCommandValidator`) — this is a UX convenience only.

const INVOICE_TOKEN_RE = /\{(YYYY|YY|MM|N{1,10})\}/g;
const INVOICE_TEMPLATE_MAX = 64;

/** Known placeholders only, no stray braces, ≥1 `{N…}` counter, within length. */
function isValidInvoiceTemplate(template: string): boolean {
  const t = template.trim();
  if (!t || t.length > INVOICE_TEMPLATE_MAX) return false;
  if (t.replace(INVOICE_TOKEN_RE, "").match(/[{}]/)) return false;
  return [...t.matchAll(INVOICE_TOKEN_RE)].some((m) => m[1].startsWith("N"));
}

/** True when the template carries a year token — counter resets per calendar year. */
function isYearScopedInvoiceTemplate(template: string): boolean {
  return template.includes("{YYYY}") || template.includes("{YY}");
}

/** Render `template` for a sample counter value and date (UTC, as the backend does). */
function renderInvoiceTemplate(template: string, sequence: number, now: Date): string {
  let hasCounter = false;
  const rendered = template.replace(INVOICE_TOKEN_RE, (_m, token: string) => {
    switch (token) {
      case "YYYY":
        return String(now.getUTCFullYear()).padStart(4, "0");
      case "YY":
        return String(now.getUTCFullYear() % 100).padStart(2, "0");
      case "MM":
        return String(now.getUTCMonth() + 1).padStart(2, "0");
      default: // run of N's
        hasCounter = true;
        return String(sequence).padStart(token.length, "0");
    }
  });
  return hasCounter
    ? rendered
    : `${rendered}-${String(sequence).padStart(4, "0")}`;
}

// ── Page ──────────────────────────────────────────────────────────────────

export function SchoolSettings() {
  const perms = useAuth().user?.permissions ?? [];
  const canManage = perms.includes("Permissions.SchoolSettings.Manage");
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ["tenant-settings"],
    queryFn: getTenantSettings,
    placeholderData: keepPreviousData,
  });

  const server = query.data;
  const [timeZoneId, setTimeZoneId] = useState<string | null>(null);
  const [currency, setCurrency] = useState<string | null>(null);
  const [restrictMaterialsOnDebt, setRestrictMaterialsOnDebt] = useState(false);
  const [debtGraceDays, setDebtGraceDays] = useState(7);
  const [invoiceTemplate, setInvoiceTemplate] = useState(
    DEFAULT_INVOICE_NUMBER_TEMPLATE,
  );

  // Seed the form from the server response once (and again whenever a fresh
  // response lands after a save). Editing is local until the user hits Save.
  useEffect(() => {
    if (server) {
      setTimeZoneId(server.timeZoneId);
      setCurrency(server.currency);
      setRestrictMaterialsOnDebt(server.restrictMaterialsOnDebt ?? false);
      setDebtGraceDays(server.debtGraceDays ?? 7);
      setInvoiceTemplate(
        server.invoiceNumberTemplate || DEFAULT_INVOICE_NUMBER_TEMPLATE,
      );
    }
  }, [server]);

  const tzOptions = useMemo(
    () =>
      toOptions(
        withCurrent(supported("timeZone", FALLBACK_TIME_ZONES), server?.timeZoneId).sort(),
        zoneOffsetLabel,
      ),
    [server?.timeZoneId],
  );
  const currencyOptions = useMemo(
    () =>
      toOptions(
        withCurrent(supported("currency", FALLBACK_CURRENCIES), server?.currency).sort(),
        currencyLabel,
      ),
    [server?.currency],
  );

  const graceValid =
    Number.isInteger(debtGraceDays) && debtGraceDays >= 0 && debtGraceDays <= 90;

  const templateValid = isValidInvoiceTemplate(invoiceTemplate);
  const templatePreview = useMemo(
    () =>
      templateValid
        ? renderInvoiceTemplate(invoiceTemplate.trim(), 1, new Date())
        : null,
    [invoiceTemplate, templateValid],
  );

  const dirty =
    !!server &&
    (timeZoneId !== server.timeZoneId ||
      (currency ?? "") !== server.currency ||
      restrictMaterialsOnDebt !== (server.restrictMaterialsOnDebt ?? false) ||
      debtGraceDays !== (server.debtGraceDays ?? 7) ||
      invoiceTemplate.trim() !==
        (server.invoiceNumberTemplate || DEFAULT_INVOICE_NUMBER_TEMPLATE));

  const save = useMutation({
    mutationFn: (input: TenantSettingsDto) => updateTenantSettings(input),
    onSuccess: (_data, input) => {
      toast.success("Настройки школы сохранены");
      queryClient.setQueryData<TenantSettingsDto>(["tenant-settings"], input);
      void queryClient.invalidateQueries({ queryKey: ["tenant-settings"] });
    },
    onError: (e) => toast.error(describe(e)),
  });

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!timeZoneId || !currency || !graceValid || !templateValid) return;
    save.mutate({
      timeZoneId,
      currency,
      restrictMaterialsOnDebt,
      debtGraceDays,
      invoiceNumberTemplate: invoiceTemplate.trim(),
    });
  };

  // One Save covers the whole form; every editable section renders the same footer.
  const saveFooter = canManage ? (
    <div className="flex items-center justify-end gap-2">
      {dirty && (
        <span className="text-[11.5px] text-[var(--color-muted-foreground)]">
          Есть несохранённые изменения
        </span>
      )}
      <Button
        type="submit"
        size="sm"
        disabled={
          !dirty ||
          save.isPending ||
          !timeZoneId ||
          !currency ||
          !graceValid ||
          !templateValid
        }
      >
        {save.isPending ? "Сохранение…" : "Сохранить"}
      </Button>
    </div>
  ) : (
    <p className="text-[11.5px] text-[var(--color-muted-foreground)]">
      Только просмотр — изменение настроек школы требует права «Управление настройками школы».
    </p>
  );

  if (query.isLoading && !server) {
    return (
      <div className="space-y-4">
        <PageHero {...HERO} />
        <Skeleton className="h-40 w-full rounded-xl" />
        <Skeleton className="h-32 w-full rounded-xl" />
      </div>
    );
  }

  if (query.isError && !server) {
    return (
      <div className="space-y-4">
        <PageHero {...HERO} />
        <SettingsSection title="Школа" icon={Landmark}>
          <p role="alert" className="text-[13px] text-[var(--color-destructive)]">
            Не удалось загрузить настройки школы: {describe(query.error)}
          </p>
        </SettingsSection>
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} className="space-y-4">
      <PageHero {...HERO} />
      <SettingsSection
        title="Регион и валюта"
        icon={Clock}
        description="Часовой пояс школы используется для отображения расписания и занятий; валюта — для тарифов, счетов и отчётов по оплатам."
        footer={saveFooter}
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <Field id="school-tz" label="Часовой пояс" required>
            <Combobox
              id="school-tz"
              label="Часовой пояс"
              value={timeZoneId}
              onChange={setTimeZoneId}
              options={tzOptions}
              searchable
              disabled={!canManage}
              placeholder="Выберите часовой пояс"
            />
          </Field>
          <Field id="school-currency" label="Валюта" required>
            <Combobox
              id="school-currency"
              label="Валюта"
              value={currency}
              onChange={setCurrency}
              options={currencyOptions}
              searchable
              disabled={!canManage}
              placeholder="Выберите валюту"
            />
          </Field>
        </div>
      </SettingsSection>

      <SettingsSection
        title="Доступ к материалам"
        icon={Lock}
        description="Если включено, ученик (и его представитель) теряет доступ к учебным материалам, пока по счёту есть просрочка старше грейс-периода. Расписание, занятия и счета остаются доступны. Сохранение — общей кнопкой в блоке «Регион и валюта»."
      >
        <div className="space-y-4">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0">
              <p
                id="school-debt-restrict-label"
                className="text-[13px] font-medium text-[var(--color-foreground)]"
              >
                Ограничивать материалы при задолженности
              </p>
              <p className="mt-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
                По умолчанию выключено.
              </p>
            </div>
            <Switch
              checked={restrictMaterialsOnDebt}
              onCheckedChange={setRestrictMaterialsOnDebt}
              disabled={!canManage}
              aria-labelledby="school-debt-restrict-label"
            />
          </div>
          <div className="max-w-[220px]">
            <Field
              id="school-debt-grace"
              label="Грейс-период, дней"
              hint="0–90. Через столько дней после срока оплаты материалы закрываются."
            >
              <Input
                id="school-debt-grace"
                type="number"
                inputMode="numeric"
                min={0}
                max={90}
                value={Number.isNaN(debtGraceDays) ? "" : debtGraceDays}
                onChange={(e) => setDebtGraceDays(e.target.valueAsNumber)}
                disabled={!canManage || !restrictMaterialsOnDebt}
                aria-invalid={!graceValid}
              />
            </Field>
          </div>
        </div>
      </SettingsSection>

      <SettingsSection
        title="Нумерация счетов"
        icon={Hash}
        description="Формат номера счёта ученика. Номер присваивается при создании счёта. Сохранение — общей кнопкой в блоке «Регион и валюта»."
      >
        <div className="max-w-[380px] space-y-3">
          <Field
            id="school-invoice-template"
            label="Шаблон номера"
            required
            hint="Плейсхолдеры: {YYYY} / {YY} — год, {MM} — месяц, {N…} — счётчик (число символов N задаёт ширину, дополняется нулями). Остальной текст выводится как есть. Нужен хотя бы один {N…}."
          >
            <Input
              id="school-invoice-template"
              value={invoiceTemplate}
              onChange={(e) => setInvoiceTemplate(e.target.value)}
              disabled={!canManage}
              maxLength={64}
              spellCheck={false}
              autoComplete="off"
              aria-invalid={!templateValid}
              className="font-mono"
            />
          </Field>
          {templateValid ? (
            <p className="text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">
              Следующий номер:{" "}
              <span className="font-mono font-medium text-[var(--color-foreground)]">
                {templatePreview}
              </span>
              .{" "}
              {isYearScopedInvoiceTemplate(invoiceTemplate)
                ? "Счётчик обнуляется в начале каждого календарного года."
                : "Счётчик сквозной за всё время работы школы."}{" "}
              Счётчик продолжается с текущей позиции школы — у первого счёта он
              может быть не 1.
            </p>
          ) : (
            <p
              role="alert"
              className="text-[11.5px] leading-relaxed text-[var(--color-destructive)]"
            >
              Шаблон может содержать только {"{YYYY}"} {"{YY}"} {"{MM}"} {"{N…}"} и
              обязан включать хотя бы один счётчик {"{N…}"}; лишние фигурные скобки
              и длина больше 64 символов недопустимы.
            </p>
          )}
        </div>
      </SettingsSection>

      <SettingsSection
        title="Рабочий календарь"
        icon={CalendarX2}
        description="Нерабочие дни и аудитории школы вынесены в отдельные экраны — они влияют на генерацию расписания и проверку конфликтов."
      >
        <div className="flex flex-col gap-2 sm:flex-row">
          <Button asChild variant="outline" size="sm" className="justify-start gap-2">
            <Link to="/school/non-working-days">
              <CalendarX2 className="size-3.5" />
              Нерабочие дни
            </Link>
          </Button>
          <Button asChild variant="outline" size="sm" className="justify-start gap-2">
            <Link to="/school/rooms">
              <DoorOpen className="size-3.5" />
              Аудитории
            </Link>
          </Button>
        </div>
      </SettingsSection>
    </form>
  );
}
