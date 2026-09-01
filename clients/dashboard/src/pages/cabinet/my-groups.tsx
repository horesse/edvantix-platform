import { useMemo } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, UsersRound } from "lucide-react";
import { getMyStudyGroups } from "@/api/study-groups";
import { searchCourses } from "@/api/curriculum";
import {
  EntityEmpty,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityMobileCard,
  EntityPageHeader,
  EntityStatusBadge,
} from "@/components/list";
import { describe } from "@/lib/list-helpers";
import {
  FORMAT_LABEL,
  STATUS_LABEL,
  STATUS_TONE,
} from "@/pages/study-groups/study-groups-ui";

const DESKTOP_COLS = "grid-cols-[1fr_110px_24px] lg:grid-cols-[1.7fr_120px_90px_24px]";

export function CabinetGroupsPage() {
  const query = useQuery({
    queryKey: ["my-study-groups"],
    queryFn: getMyStudyGroups,
  });

  const coursesQuery = useQuery({
    queryKey: ["courses", { pageSize: 100, for: "my-study-groups" }],
    queryFn: () => searchCourses({ pageSize: 100 }),
    staleTime: 60_000,
  });
  const courseName = useMemo(() => {
    const m = new Map<string, string>();
    for (const c of coursesQuery.data?.items ?? []) m.set(c.id, c.title);
    return m;
  }, [coursesQuery.data]);

  const items = query.data ?? [];

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={UsersRound}
        title="Мои группы"
        total={query.data?.length ?? null}
        unit="группа"
        description="Учебные группы, где вы — преподаватель или ученик, а также группы ваших подопечных. Только просмотр."
      />

      {query.isLoading ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={UsersRound}
          title="У вас пока нет групп"
          body="Как только вас назначат преподавателем группы или зачислят учеником, она появится здесь."
        />
      ) : (
        <div>
          <div className="space-y-2 md:hidden">
            {items.map((g) => (
              <EntityMobileCard
                key={g.id}
                href={`/study-groups/${g.id}`}
                aria-label={`Открыть группу ${g.name}`}
                dim={g.status === "Cancelled"}
              >
                <div className="flex items-center justify-between">
                  <div className="flex min-w-0 items-center gap-3">
                    <EntityInitialsAvatar name={g.name} size={40} />
                    <div className="min-w-0">
                      <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
                        {g.name}
                      </p>
                      <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
                        <span className="font-mono">{g.code}</span>
                        {courseName.get(g.courseId) ? ` · ${courseName.get(g.courseId)}` : ""}
                      </p>
                    </div>
                  </div>
                  <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
                </div>
                <div className="mt-2 ml-[52px]">
                  <EntityStatusBadge tone={STATUS_TONE[g.status]}>
                    {STATUS_LABEL[g.status]}
                  </EntityStatusBadge>
                </div>
              </EntityMobileCard>
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>Группа</span>
              <span>Статус</span>
              <span className="hidden lg:block">Формат</span>
              <span />
            </EntityListHeader>
            {items.map((g, i) => (
              <EntityListRow
                key={g.id}
                className={DESKTOP_COLS}
                isLast={i === items.length - 1}
                dim={g.status === "Cancelled"}
              >
                <Link
                  to={`/study-groups/${g.id}`}
                  className="flex min-w-0 items-center gap-3 outline-none"
                >
                  <EntityInitialsAvatar name={g.name} size={36} />
                  <div className="min-w-0">
                    <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
                      {g.name}
                    </span>
                    <span className="block truncate text-[11px] text-[var(--color-muted-foreground)]">
                      <span className="font-mono">{g.code}</span>
                      {courseName.get(g.courseId) ? ` · ${courseName.get(g.courseId)}` : ""}
                    </span>
                  </div>
                </Link>
                <div className="flex items-center">
                  <EntityStatusBadge tone={STATUS_TONE[g.status]}>
                    {STATUS_LABEL[g.status]}
                  </EntityStatusBadge>
                </div>
                <span className="hidden items-center text-[12px] text-[var(--color-muted-foreground)] lg:flex">
                  {FORMAT_LABEL[g.format]}
                </span>
                <div className="flex items-center justify-end">
                  <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
                </div>
              </EntityListRow>
            ))}
          </EntityListCard>
        </div>
      )}

      {query.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      )}
    </div>
  );
}
