import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus, Webhook, X } from "lucide-react";
import { toast } from "sonner";
import {
  SUGGESTED_EVENT_TYPES,
  WEBHOOK_WILDCARD,
  createWebhookSubscription,
  listWebhookEventCatalog,
  type WebhookEventTypeDto,
} from "@/api/webhooks";
import { MODULE_LABEL } from "@/lib/webhook-labels";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Field } from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const schema = z.object({
  url: z
    .string()
    .trim()
    .url("Введите корректный http(s)-URL.")
    .refine((u) => u.startsWith("https://") || u.startsWith("http://"), {
      message: "Используйте http:// или https://",
    }),
  secret: z
    .string()
    .trim()
    .max(256)
    .optional(),
});

type FormValues = z.infer<typeof schema>;

/** Group catalog entries by module, preserving first-seen module order. */
function groupByModule(entries: readonly WebhookEventTypeDto[]): [string, WebhookEventTypeDto[]][] {
  const order: string[] = [];
  const map = new Map<string, WebhookEventTypeDto[]>();
  for (const e of entries) {
    if (!map.has(e.module)) {
      map.set(e.module, []);
      order.push(e.module);
    }
    map.get(e.module)!.push(e);
  }
  return order.map((m) => [m, map.get(m)!]);
}

export function CreateWebhookDialog({
  open,
  onOpenChange,
  onCreated,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated?: (id: string) => void;
}) {
  const [events, setEvents] = useState<string[]>([]);
  const [draftEvent, setDraftEvent] = useState("");

  // Live catalog is the source of truth; fall back to the offline mirror.
  const catalogQuery = useQuery({
    queryKey: ["webhooks", "event-catalog"],
    queryFn: listWebhookEventCatalog,
    enabled: open,
    staleTime: 5 * 60_000,
  });
  const catalog: readonly WebhookEventTypeDto[] = catalogQuery.data ?? SUGGESTED_EVENT_TYPES;
  const groups = useMemo(() => groupByModule(catalog), [catalog]);
  const catalogNames = useMemo(() => new Set(catalog.map((e) => e.name)), [catalog]);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { url: "", secret: "" },
  });

  const reset_ = () => {
    reset();
    setEvents([]);
    setDraftEvent("");
  };

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      createWebhookSubscription({
        url: values.url,
        secret: values.secret,
        events,
      }),
    onSuccess: (id) => {
      toast.success("Подписка создана", {
        description: "Проверьте endpoint кнопкой «Тест» в строке подписки.",
      });
      onCreated?.(id);
      reset_();
      onOpenChange(false);
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error("Не удалось создать", { description: detail });
    },
  });

  const onSubmit = handleSubmit((values) => {
    if (events.length === 0) {
      toast.warning("Выберите хотя бы одно событие", {
        description: "Подписка без событий никогда не сработает.",
      });
      return;
    }
    mutation.mutate(values);
  });

  const wildcardOn = events.includes(WEBHOOK_WILDCARD);

  const toggleEvent = (name: string) => {
    setEvents((prev) => (prev.includes(name) ? prev.filter((e) => e !== name) : [...prev, name]));
  };

  const toggleWildcard = () => {
    setEvents((prev) =>
      prev.includes(WEBHOOK_WILDCARD)
        ? prev.filter((e) => e !== WEBHOOK_WILDCARD)
        : [WEBHOOK_WILDCARD],
    );
  };

  const addCustomEvent = (raw: string) => {
    const name = raw.trim();
    if (!name) return;
    if (events.includes(name)) return;
    setEvents((prev) => [...prev, name]);
    setDraftEvent("");
  };

  const removeEvent = (name: string) => {
    setEvents((prev) => prev.filter((e) => e !== name));
  };

  const submitting = isSubmitting || mutation.isPending;
  // Selected names that aren't in the catalog and aren't the wildcard — shown
  // as removable chips so a forward-compat custom event stays visible.
  const customSelected = events.filter((e) => e !== WEBHOOK_WILDCARD && !catalogNames.has(e));

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) reset_();
        onOpenChange(o);
      }}
    >
      <DialogContent size="lg">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <span
              aria-hidden
              className="grid h-7 w-7 place-items-center rounded-md bg-[var(--color-accent-signal)]/15 text-[var(--color-accent-signal)]"
            >
              <Webhook className="h-4 w-4" />
            </span>
            <DialogTitle>Новая подписка вебхука</DialogTitle>
          </div>
          <DialogDescription>
            На ваш endpoint придёт JSON с деталями события. Каждый запрос подписывается
            HMAC-SHA256 в заголовке <code className="code-chip">X-FSH-Signature</code> секретом
            ниже — храните его у себя и проверяйте подпись до доверия телу.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit}>
          <DialogBody className="space-y-5">
            <Field id="webhook-url" label="URL endpoint" required error={errors.url?.message}>
              <Input
                id="webhook-url"
                type="url"
                placeholder="https://api.example.com/webhooks/edvantix"
                autoComplete="off"
                className="font-mono"
                aria-invalid={errors.url ? true : undefined}
                {...register("url")}
              />
            </Field>

            <Field
              id="webhook-secret"
              label="Секрет подписи"
              hint="Необязательно, но рекомендуется. Не меньше 32 случайных символов. Используется для HMAC."
              error={errors.secret?.message}
            >
              <Input
                id="webhook-secret"
                type="password"
                autoComplete="new-password"
                placeholder="Пусто — без подписи"
                className="font-mono"
                {...register("secret")}
              />
            </Field>

            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <div className="meta text-[var(--color-muted-foreground)]">
                  События ({events.length})
                  <span className="text-[var(--color-destructive)]" aria-hidden> ·</span>
                </div>
                <label className="flex cursor-pointer items-center gap-1.5 text-[11.5px] text-[var(--color-muted-foreground)]">
                  <input
                    type="checkbox"
                    checked={wildcardOn}
                    onChange={toggleWildcard}
                    className="size-3.5 accent-[var(--color-accent-signal)]"
                  />
                  Все события (<code className="code-chip">*</code>)
                </label>
              </div>

              {catalogQuery.isError && (
                <p className="text-[11.5px] text-[var(--color-warning)]">
                  Каталог событий недоступен — показан офлайн-список.
                </p>
              )}

              {/* Catalog — grouped by module */}
              <div
                className={cn(
                  "max-h-64 space-y-3 overflow-y-auto rounded-md border border-[var(--color-input)] p-3",
                  wildcardOn && "pointer-events-none opacity-50",
                )}
              >
                {groups.map(([module, entries]) => (
                  <fieldset key={module} className="space-y-1.5">
                    <legend className="font-mono text-[10.5px] font-semibold uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
                      {MODULE_LABEL[module] ?? module}
                    </legend>
                    <div className="space-y-1">
                      {entries.map((entry) => (
                        <label
                          key={entry.name}
                          className="flex cursor-pointer items-start gap-2 rounded px-1.5 py-1 hover:bg-[var(--color-muted)]/50"
                        >
                          <input
                            type="checkbox"
                            checked={events.includes(entry.name)}
                            onChange={() => toggleEvent(entry.name)}
                            className="mt-0.5 size-3.5 shrink-0 accent-[var(--color-accent-signal)]"
                          />
                          <span className="min-w-0">
                            <span className="block truncate font-mono text-[11.5px] text-[var(--color-foreground)]">
                              {entry.name}
                            </span>
                            <span className="block text-[11px] leading-snug text-[var(--color-muted-foreground)]">
                              {entry.description}
                            </span>
                          </span>
                        </label>
                      ))}
                    </div>
                  </fieldset>
                ))}
              </div>

              {/* Custom / forward-compat events */}
              <div className="space-y-1.5">
                <div className="meta text-[var(--color-muted-foreground)]">
                  Другое событие (для новых типов, которых ещё нет в каталоге)
                </div>
                <div className="flex flex-wrap items-center gap-1.5 rounded-md border border-[var(--color-input)] bg-transparent p-2 min-h-10">
                  {customSelected.map((e) => (
                    <span
                      key={e}
                      className="inline-flex items-center gap-1 rounded-md bg-[var(--color-accent-signal)]/15 px-2 py-0.5 font-mono text-[11px] text-[var(--color-foreground)]"
                    >
                      {e}
                      <button
                        type="button"
                        onClick={() => removeEvent(e)}
                        aria-label={`Убрать ${e}`}
                        className="text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </span>
                  ))}
                  <input
                    value={draftEvent}
                    onChange={(e) => setDraftEvent(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" || e.key === ",") {
                        e.preventDefault();
                        addCustomEvent(draftEvent);
                      } else if (e.key === "Backspace" && draftEvent === "" && customSelected.length > 0) {
                        removeEvent(customSelected[customSelected.length - 1]);
                      }
                    }}
                    placeholder="имя события, затем Enter…"
                    className="min-w-[10rem] flex-1 bg-transparent font-mono text-xs outline-none placeholder:text-[var(--color-muted-foreground)]/70"
                  />
                  <button
                    type="button"
                    onClick={() => addCustomEvent(draftEvent)}
                    className="inline-flex items-center gap-1 rounded-md border border-[var(--color-border)] px-2 py-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)] transition-colors hover:border-[var(--color-accent-signal)] hover:text-[var(--color-foreground)]"
                  >
                    <Plus className="h-2.5 w-2.5" /> добавить
                  </button>
                </div>
              </div>
            </div>
          </DialogBody>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={submitting}>
              Отмена
            </Button>
            <Button type="submit" variant="signal" disabled={submitting}>
              {submitting ? "Создание…" : "Создать подписку"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
