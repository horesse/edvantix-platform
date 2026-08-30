import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { RotateCcw, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { listTrashedCourses, restoreCourse, type CourseDto } from "@/api/curriculum";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import {
  EntityDetailBack,
  EntityEmpty,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityPager,
} from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import { LEVEL_LABEL } from "./curriculum-ui";

const PAGE_SIZE = 20;
const DESKTOP_COLS = "grid-cols-[1fr_120px_120px] lg:grid-cols-[1.6fr_140px_140px_120px]";

export function CoursesTrashPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canRestore = perms.includes("Permissions.Curriculum.Courses.Restore");

  const [pageNumber, setPageNumber] = useState(1);

  const query = useQuery({
    queryKey: ["courses", "trash", pageNumber],
    queryFn: () => listTrashedCourses(pageNumber, PAGE_SIZE),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityDetailBack to="/courses" label="К списку курсов" />

      <EntityPageHeader
        icon={Trash2}
        title="Корзина курсов"
        total={data?.totalCount ?? null}
        unit="курс"
        description="Удалённые курсы. Восстановление возвращает курс в статусе «Черновик» со всеми разделами и уроками."
      />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={Trash2}
          title="Корзина пуста"
          body="Удалённые курсы появятся здесь и их можно будет восстановить."
        />
      ) : (
        <div>
          <EntityListCard>
            <EntityListHeader className={DESKTOP_COLS}>
              <span>Курс</span>
              <span className="hidden lg:block">Уровень</span>
              <span>Создан</span>
              <span />
            </EntityListHeader>
            {items.map((c, i) => (
              <TrashRow
                key={c.id}
                course={c}
                isLast={i === items.length - 1}
                canRestore={canRestore}
              />
            ))}
          </EntityListCard>

          <EntityPager
            page={data?.pageNumber ?? 1}
            totalPages={Math.max(data?.totalPages ?? 1, 1)}
            hasPrev={data?.hasPrevious ?? false}
            hasNext={data?.hasNext ?? false}
            onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
            onNext={() => setPageNumber((p) => p + 1)}
          />
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

function TrashRow({
  course,
  isLast,
  canRestore,
}: {
  course: CourseDto;
  isLast: boolean;
  canRestore: boolean;
}) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const mutation = useMutation({
    mutationFn: () => restoreCourse(course.id),
    onSuccess: (newId) => {
      toast.success("Курс восстановлен");
      void queryClient.invalidateQueries({ queryKey: ["courses"] });
      navigate(`/courses/${newId}`);
    },
    onError: (err) => toast.error("Не удалось восстановить", { description: describe(err) }),
  });

  return (
    <EntityListRow className={DESKTOP_COLS} isLast={isLast} dim>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={course.title} size={36} />
        <div className="min-w-0">
          <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)]">
            {course.title}
          </span>
          <span className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
            {course.slug}
          </span>
        </div>
      </div>

      <span className="hidden items-center text-[12px] text-[var(--color-muted-foreground)] lg:flex">
        {LEVEL_LABEL[course.level]}
      </span>

      <span className="flex items-center text-[12px] text-[var(--color-muted-foreground)]">
        {formatDate(course.createdAtUtc)}
      </span>

      <div className="flex items-center justify-end">
        {canRestore && (
          <Button
            size="sm"
            variant="outline"
            className="h-8 gap-1.5 px-3 text-[12px]"
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending}
          >
            <RotateCcw className="size-3.5" />
            {mutation.isPending ? "…" : "Восстановить"}
          </Button>
        )}
      </div>
    </EntityListRow>
  );
}
