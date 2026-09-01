import { Users } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useWard } from "./use-ward";

/**
 * Переключатель подопечных для кабинета представителя. Ничего не рендерит,
 * если у пользователя нет подопечных (ученик, или представитель без
 * привязанных учеников). Выбор общий для всех экранов кабинета через
 * `WardProvider`.
 */
export function WardSwitcher() {
  const { wards, hasWards, selectedWardId, setSelectedWardId } = useWard();

  if (!hasWards) return null;

  return (
    <div
      className="flex flex-wrap items-center gap-1.5"
      role="group"
      aria-label="Подопечный"
    >
      <span className="mr-1 inline-flex items-center gap-1.5 text-[12px] font-medium text-[var(--color-muted-foreground)]">
        <Users className="size-3.5" aria-hidden />
        Подопечный
      </span>
      <Button
        size="sm"
        variant={selectedWardId === null ? "default" : "outline"}
        aria-pressed={selectedWardId === null}
        onClick={() => setSelectedWardId(null)}
      >
        Все
      </Button>
      {wards.map((w) => (
        <Button
          key={w.id}
          size="sm"
          variant={selectedWardId === w.id ? "default" : "outline"}
          aria-pressed={selectedWardId === w.id}
          onClick={() => setSelectedWardId(w.id)}
        >
          {w.name}
        </Button>
      ))}
    </div>
  );
}
