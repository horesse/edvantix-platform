import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  ArrowLeft,
  CheckCircle2,
  Link2,
  List,
  RefreshCw,
  Send,
  Trash2,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";
import {
  deleteWebhookSubscription,
  listWebhookDeliveries,
  listWebhookSubscriptions,
  testWebhookSubscription,
  type WebhookDeliveryDto,
  type WebhookSubscriptionDto,
} from "@/api/webhooks";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  ErrorBand,
  LoadingRow,
  PageHeader,
  Pagination,
  SettingsSection,
} from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const PAGE_SIZE = 25;

export function WebhookDetailPage() {
  const { id = "" } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [deliveryPage, setDeliveryPage] = useState(1);

  // No GET /subscriptions/{id} on the server, so list with a big enough page
  // and find by id. Subscription counts are typically small (< a few dozen).
  const subsQuery = useQuery({
    queryKey: ["webhooks", "subscriptions", "all"],
    queryFn: () => listWebhookSubscriptions(1, 200),
  });

  const sub: WebhookSubscriptionDto | undefined = subsQuery.data?.items.find((s) => s.id === id);

  const deliveries = useQuery({
    queryKey: ["webhooks", "deliveries", id, deliveryPage],
    queryFn: () => listWebhookDeliveries(id, deliveryPage, PAGE_SIZE),
    enabled: Boolean(sub),
    placeholderData: keepPreviousData,
    refetchInterval: 10_000,
  });

  const test = useMutation({
    mutationFn: () => testWebhookSubscription(id),
    onSuccess: (data) => {
      toast[data.success ? "success" : "warning"](
        data.success ? "Тестовое событие доставлено" : "Endpoint отклонил тестовое событие",
      );
      deliveries.refetch();
    },
    onError: (err) => toast.error("Тест не прошёл", { description: describe(err) }),
  });

  const remove = useMutation({
    mutationFn: () => deleteWebhookSubscription(id),
    onSuccess: () => {
      toast.success("Подписка удалена");
      queryClient.invalidateQueries({ queryKey: ["webhooks", "subscriptions"] });
      navigate("/webhooks");
    },
    onError: (err) => toast.error("Не удалось удалить", { description: describe(err) }),
  });

  return (
    <div className="space-y-8">
      <PageHeader
        crumbs={[{ label: "\\ Вебхуки" }, { label: sub?.url ?? "…", muted: true }]}
        trailing={sub ? (sub.isActive ? "АКТИВНА" : "НЕАКТИВНА") : "—"}
        title={sub?.url ?? "Подписка"}
        description={sub ? `Событий в подписке: ${sub.events.length}.` : "Загрузка подписки…"}
        actions={
          <Button variant="ghost" size="sm" onClick={() => navigate("/webhooks")}>
            <ArrowLeft className="mr-1 h-3.5 w-3.5" /> Подписки
          </Button>
        }
      />

      {subsQuery.isError && (
        <ErrorBand
          message={
            subsQuery.error instanceof ApiRequestError
              ? subsQuery.error.problem?.detail ?? subsQuery.error.message
              : "Не удалось загрузить подписку."
          }
        />
      )}

      {subsQuery.isLoading && <LoadingRow label="Загрузка подписки" />}

      {!subsQuery.isLoading && !sub && !subsQuery.isError && (
        <ErrorBand message="Подписка не найдена. Возможно, она удалена." />
      )}

      {sub && (
        <div className="space-y-4">
          <SettingsSection
            icon={Link2}
            title="Endpoint"
            description="Куда мы шлём POST с телами событий."
            footer={
              <div className="flex flex-wrap items-center gap-2">
                <Button variant="outline" size="sm" onClick={() => test.mutate()} disabled={test.isPending}>
                  <Send className="mr-1.5 h-3.5 w-3.5" />
                  {test.isPending ? "Отправка…" : "Отправить тестовое событие"}
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    if (window.confirm(`Удалить подписку на ${sub.url}?`)) {
                      remove.mutate();
                    }
                  }}
                  disabled={remove.isPending}
                  className="text-[var(--color-destructive)] hover:bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)]"
                >
                  <Trash2 className="mr-1.5 h-3.5 w-3.5" />
                  {remove.isPending ? "Удаление…" : "Удалить подписку"}
                </Button>
              </div>
            }
          >
            <dl className="grid grid-cols-1 gap-y-3 sm:grid-cols-2">
              <FieldRow label="URL" mono value={sub.url} />
              <FieldRow label="Статус" value={
                <Badge variant={sub.isActive ? "success" : "muted"} className="font-mono uppercase tracking-[0.14em]">
                  {sub.isActive ? "Активна" : "Неактивна"}
                </Badge>
              } />
              <FieldRow label="ID подписки" mono value={sub.id} />
              <FieldRow label="Создана" mono value={new Date(sub.createdAtUtc).toLocaleString("ru-RU")} />
            </dl>
          </SettingsSection>

          <SettingsSection
            icon={List}
            title="События"
            description="Типы событий, на которые подписан этот endpoint."
          >
            <div className="flex flex-wrap gap-1.5">
              {sub.events.map((e) => (
                <code key={e} className="code-chip">{e}</code>
              ))}
              {sub.events.length === 0 && (
                <span className="text-sm text-[var(--color-muted-foreground)]">— событий нет; подписка никогда не сработает</span>
              )}
            </div>
          </SettingsSection>

          <SettingsSection
            title="Доставки"
            description="Недавние попытки POST'ов на этот endpoint. Автообновление каждые 10 с."
            footer={
              <div className="flex items-center justify-between gap-2">
                <span className="font-mono text-[11px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
                  {deliveries.data ? `попыток: ${deliveries.data.totalCount}` : "—"}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => deliveries.refetch()}
                  disabled={deliveries.isFetching}
                >
                  <RefreshCw className={cn("mr-1.5 h-3.5 w-3.5", deliveries.isFetching && "animate-spin")} />
                  Обновить
                </Button>
              </div>
            }
          >
            {deliveries.isError ? (
              <ErrorBand message={describe(deliveries.error)} />
            ) : deliveries.isLoading ? (
              <LoadingRow label="Загрузка доставок" />
            ) : (deliveries.data?.items.length ?? 0) === 0 ? (
              <p className="text-sm text-[var(--color-muted-foreground)]">
                Доставок пока нет. Нажмите «Отправить тестовое событие» выше или дождитесь события.
              </p>
            ) : (
              <>
                <ol className="-mx-5 divide-y divide-[var(--color-border)] border-y border-[var(--color-border)]">
                  {(deliveries.data!.items ?? []).map((d) => (
                    <DeliveryRow key={d.id} delivery={d} />
                  ))}
                </ol>
                {deliveries.data!.totalPages > 1 && (
                  <div className="mt-4">
                    <Pagination
                      page={deliveries.data!.pageNumber}
                      totalPages={deliveries.data!.totalPages}
                      totalCount={deliveries.data!.totalCount}
                      shown={deliveries.data!.items.length}
                      fetching={deliveries.isFetching}
                      hasPrev={deliveries.data!.hasPrevious}
                      hasNext={deliveries.data!.hasNext}
                      onPrev={() => setDeliveryPage((p) => Math.max(1, p - 1))}
                      onNext={() => setDeliveryPage((p) => p + 1)}
                      noun="доставки"
                    />
                  </div>
                )}
              </>
            )}
          </SettingsSection>
        </div>
      )}
    </div>
  );
}

function DeliveryRow({ delivery }: { delivery: WebhookDeliveryDto }) {
  const Icon = delivery.success ? CheckCircle2 : XCircle;
  const tone = delivery.success ? "text-[var(--color-success)]" : "text-[var(--color-destructive)]";
  return (
    <li className="grid grid-cols-[auto_8rem_auto_1fr_auto_auto] items-center gap-3 px-5 py-2.5">
      <Icon className={cn("h-4 w-4", tone)} />
      <span className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
        {formatTimestamp(delivery.attemptedAtUtc)}
      </span>
      <code className="code-chip">{delivery.eventType}</code>
      <span className="truncate text-[11.5px] text-[var(--color-muted-foreground)]">
        {delivery.errorMessage ?? (delivery.success ? "OK" : "Ошибка")}
      </span>
      <Badge
        variant={delivery.success ? "success" : "danger"}
        className="font-mono uppercase tracking-[0.14em]"
      >
        HTTP {delivery.httpStatusCode || "—"}
      </Badge>
      <span className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
        попытка {delivery.attemptCount}
      </span>
    </li>
  );
}

function FieldRow({ label, value, mono }: { label: string; value: React.ReactNode; mono?: boolean }) {
  return (
    <div className="grid grid-cols-[10rem_1fr] items-baseline gap-4">
      <dt className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">{label}</dt>
      <dd className={cn("min-w-0 break-words text-sm", mono && "font-mono text-[0.8125rem]")}>
        {value}
      </dd>
    </div>
  );
}

function formatTimestamp(value: string): string {
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

function describe(err: unknown): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return String(err);
}
