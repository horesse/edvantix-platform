import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { AlertTriangle } from "lucide-react";
import {
  findDuplicatePersonCandidates,
  type DuplicatePersonCandidate,
} from "@/api/people";
import { cn } from "@/lib/cn";

// ─────────────────────────────────────────────────────────────────────────
//  EDX-018 — «Возможно, это дубль».
//  Неблокирующая плашка для диалогов создания ученика / представителя /
//  преподавателя. Спрашивает GET /people/duplicate-candidates по введённым
//  ФИО + телефону/e-mail; рендерит null, пока данных для проверки мало или
//  кандидатов нет. Карточки открываются в новой вкладке, чтобы не потерять
//  заполненный диалог. Создание никогда не запрещаем.
// ─────────────────────────────────────────────────────────────────────────

const CARD_PATH: Record<DuplicatePersonCandidate["personType"], string> = {
  Student: "/students",
  Teacher: "/teachers",
  Guardian: "/guardians",
};

const TYPE_LABEL: Record<DuplicatePersonCandidate["personType"], string> = {
  Student: "ученик",
  Teacher: "преподаватель",
  Guardian: "представитель",
};

function matchLabel(c: DuplicatePersonCandidate): string {
  if (c.phoneMatches && c.emailMatches) return "совпадают телефон и e-mail";
  if (c.phoneMatches) return "совпадает телефон";
  return "совпадает e-mail";
}

export function DuplicatePersonNotice({
  lastName,
  firstName,
  phone,
  email,
  onCountChange,
  className,
}: {
  lastName: string;
  firstName: string;
  phone?: string;
  email?: string;
  onCountChange?: (count: number) => void;
  className?: string;
}) {
  const ln = lastName.trim();
  const fn = firstName.trim();
  const ph = (phone ?? "").trim();
  const em = (email ?? "").trim();

  const [debounced, setDebounced] = useState({ ln, fn, ph, em });
  useEffect(() => {
    const t = setTimeout(() => setDebounced({ ln, fn, ph, em }), 400);
    return () => clearTimeout(t);
  }, [ln, fn, ph, em]);

  const enabled =
    debounced.ln.length > 1 &&
    debounced.fn.length > 0 &&
    (debounced.ph.length > 0 || debounced.em.length > 0);

  const query = useQuery({
    queryKey: ["people", "duplicate-candidates", debounced],
    queryFn: () =>
      findDuplicatePersonCandidates({
        lastName: debounced.ln,
        firstName: debounced.fn,
        phone: debounced.ph || null,
        email: debounced.em || null,
      }),
    enabled,
    staleTime: 30_000,
  });

  const candidates = enabled ? (query.data ?? []) : [];

  useEffect(() => {
    onCountChange?.(candidates.length);
  }, [candidates.length, onCountChange]);

  if (candidates.length === 0) return null;

  return (
    <div
      role="alert"
      className={cn(
        "flex items-start gap-3 rounded-xl border px-4 py-3 text-[13px]",
        "border-[oklch(from_var(--color-warning)_l_c_h_/_0.35)] bg-[oklch(from_var(--color-warning)_l_c_h_/_0.08)] text-[var(--color-foreground)]",
        className,
      )}
    >
      <AlertTriangle
        className="mt-0.5 size-4 shrink-0 text-[var(--color-warning)]"
        aria-hidden
      />
      <div className="min-w-0 space-y-1">
        <p className="font-semibold">
          {candidates.length === 1
            ? "Возможно, это дубль — такой человек уже есть"
            : "Возможно, это дубль — такие люди уже есть"}
        </p>
        <ul className="space-y-0.5 text-[12px] text-[var(--color-muted-foreground)]">
          {candidates.map((c) => (
            <li key={`${c.personType}-${c.id}`} className="truncate">
              <Link
                to={`${CARD_PATH[c.personType]}/${c.id}`}
                target="_blank"
                rel="noopener noreferrer"
                className="font-medium text-[var(--color-primary)] hover:underline"
              >
                {c.displayName}
              </Link>{" "}
              — {TYPE_LABEL[c.personType]}, {matchLabel(c)}
            </li>
          ))}
        </ul>
        <p className="text-[12px] text-[var(--color-muted-foreground)]">
          Создание не блокируется — если это другой человек, продолжайте.
        </p>
      </div>
    </div>
  );
}
