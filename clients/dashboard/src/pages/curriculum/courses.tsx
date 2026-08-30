import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { BookOpen, ChevronRight, Clock, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  COURSE_LEVELS,
  createCourse,
  getSubjectTree,
  searchCourses,
  type CourseDto,
  type CourseLevel,
  type CourseStatus,
  type CreateCourseInput,
} from "@/api/curriculum";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Combobox,
  EntityEmpty,
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityMobileCard,
  EntityPageHeader,
  EntityPager,
  EntitySearch,
  EntityStatusBadge,
  Field,
  type ComboboxOption,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";
import { flattenSubjects, LEVEL_LABEL, STATUS_LABEL, STATUS_TONE } from "./curriculum-ui";

const PAGE_SIZE = 20;

type StatusFilter = CourseStatus | "all";
type SortKey = "title" | "createdAtUtc" | "durationHours";

const SORT_OPTIONS: ComboboxOption[] = [
  { value: "title", label: "По названию" },
  { value: "createdAtUtc", label: "По дате создания" },
  { value: "durationHours", label: "По длительности" },
];

const DESKTOP_COLS =
  "grid-cols-[1fr_120px_24px] lg:grid-cols-[1.7fr_130px_130px_120px_24px]";

export function CoursesPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canCreate = perms.includes("Permissions.Curriculum.Courses.Create");
  const canViewTrash = perms.includes("Permissions.Curriculum.Courses.ViewTrash");

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [levelFilter, setLevelFilter] = useState<string | null>(null);
  const [subjectFilter, setSubjectFilter] = useState<string | null>(null);
  const [sortBy, setSortBy] = useState<SortKey>("createdAtUtc");
  const [createOpen, setCreateOpen] = useState(false);

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  useEffect(
    () => setPageNumber(1),
    [statusFilter, levelFilter, subjectFilter, sortBy],
  );

  const subjectsQuery = useQuery({
    queryKey: ["subjects", "tree"],
    queryFn: getSubjectTree,
    staleTime: 60_000,
  });
  const subjectOptions = useMemo(
    () => flattenSubjects(subjectsQuery.data ?? []),
    [subjectsQuery.data],
  );

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debouncedSearch || undefined,
      status: statusFilter === "all" ? null : statusFilter,
      level: (levelFilter as CourseLevel | null) ?? null,
      subjectId: subjectFilter,
      sortBy,
      sortDir: sortBy === "title" ? ("asc" as const) : ("desc" as const),
    }),
    [
      pageNumber,
      debouncedSearch,
      statusFilter,
      levelFilter,
      subjectFilter,
      sortBy,
    ],
  );

  const query = useQuery({
    queryKey: ["courses", queryParams],
    queryFn: () => searchCourses(queryParams),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const filtersActive =
    debouncedSearch.length > 0 ||
    statusFilter !== "all" ||
    levelFilter !== null ||
    subjectFilter !== null;

  const clearFilters = () => {
    setSearch("");
    setStatusFilter("all");
    setLevelFilter(null);
    setSubjectFilter(null);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={BookOpen}
        title="Курсы"
        total={data?.totalCount ?? null}
        unit="курс"
        description="Учебные программы школы: разделы, уроки и материалы собираются в конструкторе курса."
      >
        {canViewTrash && (
          <Button
            variant="outline"
            asChild
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
          >
            <Link to="/courses/trash">
              <Trash2 className="size-4" />
              Корзина
            </Link>
          </Button>
        )}
        {canCreate && (
          <Button
            onClick={() => setCreateOpen(true)}
            className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
          >
            <Plus className="size-4" />
            Новый курс
          </Button>
        )}
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Поиск по названию…"
      />

      <div className="flex flex-wrap items-center gap-2">
        <EntityFilterPill<StatusFilter>
          label="Статус"
          value={statusFilter}
          onChange={setStatusFilter}
          options={[
            { value: "all", label: "Все" },
            { value: "Draft", label: "Черновики" },
            { value: "Published", label: "Опубликованы" },
            { value: "Archived", label: "Архив" },
          ]}
        />
        <Combobox
          label="Направление"
          value={subjectFilter}
          onChange={setSubjectFilter}
          options={subjectOptions}
          variant="filter"
          searchable
          clearable
        />
        <Combobox
          label="Уровень"
          value={levelFilter}
          onChange={setLevelFilter}
          options={COURSE_LEVELS.map((l) => ({ value: l, label: LEVEL_LABEL[l] }))}
          variant="filter"
          clearable
        />
        <Combobox
          label="Сортировка"
          value={sortBy}
          onChange={(v) => setSortBy((v as SortKey) ?? "createdAtUtc")}
          options={SORT_OPTIONS}
          variant="filter"
        />
      </div>

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={BookOpen}
          title={filtersActive ? "Ничего не найдено" : "Пока нет курсов"}
          body={
            filtersActive
              ? "Измените запрос или сбросьте фильтры."
              : "Создайте первый курс — затем наполните его разделами и уроками в конструкторе."
          }
          action={
            filtersActive ? (
              <Button variant="outline" onClick={clearFilters} className="h-9 rounded-lg px-4 text-[13px]">
                Сбросить фильтры
              </Button>
            ) : canCreate ? (
              <Button onClick={() => setCreateOpen(true)} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Новый курс
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div>
          <div className="space-y-2 md:hidden">
            {items.map((c) => (
              <CourseMobileCard key={c.id} course={c} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>Курс</span>
              <span>Статус</span>
              <span className="hidden lg:block">Уровень</span>
              <span className="hidden lg:block">Часы</span>
              <span />
            </EntityListHeader>
            {items.map((c, i) => (
              <CourseDesktopRow key={c.id} course={c} isLast={i === items.length - 1} />
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

      <CreateCourseDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        subjectOptions={subjectOptions}
      />
    </div>
  );
}

function CourseMobileCard({ course }: { course: CourseDto }) {
  return (
    <EntityMobileCard
      href={`/courses/${course.id}`}
      aria-label={`Открыть курс ${course.title}`}
      dim={course.status === "Archived"}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={course.title} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {course.title}
            </p>
            <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
              {LEVEL_LABEL[course.level]} · {course.durationHours} ч
            </p>
          </div>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 ml-[52px]">
        <EntityStatusBadge tone={STATUS_TONE[course.status]}>
          {STATUS_LABEL[course.status]}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function CourseDesktopRow({ course, isLast }: { course: CourseDto; isLast: boolean }) {
  return (
    <EntityListRow className={DESKTOP_COLS} isLast={isLast} dim={course.status === "Archived"}>
      <Link to={`/courses/${course.id}`} className="flex min-w-0 items-center gap-3 outline-none">
        <EntityInitialsAvatar name={course.title} size={36} />
        <div className="min-w-0">
          <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {course.title}
          </span>
          <span className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
            {course.slug}
          </span>
        </div>
      </Link>

      <div className="flex items-center">
        <EntityStatusBadge tone={STATUS_TONE[course.status]}>
          {STATUS_LABEL[course.status]}
        </EntityStatusBadge>
      </div>

      <span className="hidden items-center text-[12px] text-[var(--color-muted-foreground)] lg:flex">
        {LEVEL_LABEL[course.level]}
      </span>

      <span className="hidden items-center gap-1 text-[12px] text-[var(--color-muted-foreground)] lg:flex">
        <Clock className="size-3" />
        {course.durationHours} ч
      </span>

      <div className="flex items-center justify-end">
        <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
      </div>
    </EntityListRow>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Create dialog
// ───────────────────────────────────────────────────────────────────────

function CreateCourseDialog({
  open,
  onClose,
  subjectOptions,
}: {
  open: boolean;
  onClose: () => void;
  subjectOptions: ComboboxOption[];
}) {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState("");
  const [subjectId, setSubjectId] = useState<string | null>(null);
  const [level, setLevel] = useState<CourseLevel>("Beginner");
  const [durationHours, setDurationHours] = useState("0");
  const [description, setDescription] = useState("");

  useEffect(() => {
    if (!open) {
      setTitle("");
      setSubjectId(null);
      setLevel("Beginner");
      setDurationHours("0");
      setDescription("");
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: (input: CreateCourseInput) => createCourse(input),
    onSuccess: () => {
      toast.success("Курс создан");
      void queryClient.invalidateQueries({ queryKey: ["courses"] });
      onClose();
    },
    onError: (err) => toast.error("Не удалось создать курс", { description: describe(err) }),
  });

  const hours = Number.parseInt(durationHours, 10);
  const valid = title.trim().length > 0 && !!subjectId && !Number.isNaN(hours) && hours >= 0;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid || !subjectId) return;
    mutation.mutate({
      subjectId,
      title: title.trim(),
      description: description.trim() || null,
      level,
      durationHours: hours,
      coverFileId: null,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Новый курс</DialogTitle>
            <DialogDescription>
              Курс создаётся в статусе «Черновик». Разделы и уроки добавляются
              позже в конструкторе.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="c-title" label="Название" required>
              <Input
                id="c-title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
                autoFocus
              />
            </Field>
            <Field id="c-subject" label="Направление" required>
              <Combobox
                id="c-subject"
                label="Направление"
                value={subjectId}
                onChange={setSubjectId}
                options={subjectOptions}
                searchable
                required
              />
            </Field>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="c-level" label="Уровень" required>
                <Combobox
                  id="c-level"
                  label="Уровень"
                  value={level}
                  onChange={(v) => setLevel((v as CourseLevel) ?? "Beginner")}
                  options={COURSE_LEVELS.map((l) => ({ value: l, label: LEVEL_LABEL[l] }))}
                />
              </Field>
              <Field id="c-hours" label="Длительность, часов" required>
                <Input
                  id="c-hours"
                  type="number"
                  min="0"
                  value={durationHours}
                  onChange={(e) => setDurationHours(e.target.value)}
                  required
                  className="tabular-nums"
                />
              </Field>
            </div>
            <Field id="c-desc" label="Описание">
              <textarea
                id="c-desc"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                maxLength={4000}
                className={cn(
                  "flex w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm",
                  "placeholder:text-[var(--color-muted-foreground)]",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
                )}
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !valid} className="gap-1.5">
              <Plus className="h-4 w-4" />
              {mutation.isPending ? "Создание…" : "Создать"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
