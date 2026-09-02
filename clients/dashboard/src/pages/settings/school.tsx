import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CalendarX2, Clock, DoorOpen, Hash, Landmark } from "lucide-react";
import { toast } from "sonner";
import {
  getTenantSettings,
  updateTenantSettings,
  type TenantSettingsDto,
} from "@/api/tenant-settings";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
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

  // Seed the form from the server response once (and again whenever a fresh
  // response lands after a save). Editing is local until the user hits Save.
  useEffect(() => {
    if (server) {
      setTimeZoneId(server.timeZoneId);
      setCurrency(server.currency);
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

  const dirty =
    !!server &&
    (timeZoneId !== server.timeZoneId || (currency ?? "") !== server.currency);

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
    if (!timeZoneId || !currency) return;
    save.mutate({ timeZoneId, currency });
  };

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
        footer={
          canManage ? (
            <div className="flex items-center justify-end gap-2">
              {dirty && (
                <span className="text-[11.5px] text-[var(--color-muted-foreground)]">
                  Есть несохранённые изменения
                </span>
              )}
              <Button
                type="submit"
                size="sm"
                disabled={!dirty || save.isPending || !timeZoneId || !currency}
              >
                {save.isPending ? "Сохранение…" : "Сохранить"}
              </Button>
            </div>
          ) : (
            <p className="text-[11.5px] text-[var(--color-muted-foreground)]">
              Только просмотр — изменение настроек школы требует права «Управление настройками школы».
            </p>
          )
        }
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
        title="Нумерация счетов"
        icon={Hash}
        description="Формат номеров счетов учеников. Пока задаётся сервером автоматически и не настраивается из интерфейса."
      >
        <div className="rounded-lg border border-dashed border-[var(--color-border)] bg-[var(--color-muted)]/40 px-4 py-3 text-[12.5px] text-[var(--color-muted-foreground)]">
          Номер формируется автоматически при выставлении счёта. Настройка префикса и
          счётчика появится в отдельном обновлении модуля «Оплаты».
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
