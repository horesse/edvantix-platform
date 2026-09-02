import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { Lock } from "lucide-react";
import { getMyMaterialsAccess } from "@/api/payments";
import { formatDate } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

// ─────────────────────────────────────────────────────────────────────────
//  EDX-015 — «Доступ к материалам ограничен из-за задолженности».
//  Плашка кабинета: показывается только когда сервер вернул
//  `restricted: true` (флаг школы включён И у ученика/подопечного есть
//  просрочка старше грейс-периода). В остальных случаях — рендерит null,
//  поэтому её можно безусловно вставлять на страницы кабинета.
//  Источник: GET /student-invoices/my/materials-access.
// ─────────────────────────────────────────────────────────────────────────

export function MaterialsDebtNotice({ className }: { className?: string }) {
  const query = useQuery({
    queryKey: ["my-materials-access"],
    queryFn: getMyMaterialsAccess,
    staleTime: 60_000,
  });

  const status = query.data;
  if (!status?.restricted) return null;

  return (
    <div
      role="status"
      className={cn(
        "flex items-start gap-3 rounded-xl border px-4 py-3 text-[13px]",
        "border-[oklch(from_var(--color-warning)_l_c_h_/_0.35)] bg-[oklch(from_var(--color-warning)_l_c_h_/_0.08)] text-[var(--color-foreground)]",
        className,
      )}
    >
      <Lock
        className="mt-0.5 size-4 shrink-0 text-[var(--color-warning)]"
        aria-hidden
      />
      <div className="min-w-0 space-y-1">
        <p className="font-semibold">Доступ к материалам ограничен из-за задолженности</p>
        <p className="text-[12px] text-[var(--color-muted-foreground)]">
          {status.overdueSince
            ? `Есть счёт, просроченный с ${formatDate(status.overdueSince)}. `
            : ""}
          Учебные материалы снова откроются после оплаты. Расписание и занятия
          остаются доступны.{" "}
          <Link
            to="/my/invoices"
            className="font-medium text-[var(--color-primary)] hover:underline"
          >
            Перейти к счетам
          </Link>
        </p>
      </div>
    </div>
  );
}
