import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, Plus, Users, UsersRound } from "lucide-react";
import { toast } from "sonner";
import {
  createStudyGroup,
  GROUP_FORMATS,
  searchStudyGroups,
  type CreateStudyGroupInput,
  type GroupFormat,
  type StudyGroupDto,
  type StudyGroupStatus,
} from "@/api/study-groups";
import { searchCourses } from "@/api/curriculum";
import { searchTeachers } from "@/api/people";
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
import { FORMAT_LABEL, STATUS_LABEL, STATUS_TONE } from "./study-groups-ui";

const PAGE_SIZE = 20;

type StatusFilter = StudyGroupStatus | "all";
type SortKey = "code" | "name" | "startDate" | "status";

const SORT_OPTIONS: ComboboxOption[] = [
  { value: "code", label: "По коду" },
  { value: "name", label: "По названию" },
  { value: "startDate", label: "По дате старта" },
  { value: "status", label: "По статусу" },
];

const DESKTOP_COLS =
  "grid-cols-[1fr_110px_24px] lg:grid-cols-[1.7fr_120px_120px_90px_24px]";

const TEXTAREA_CLS = cn(
  "flex w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm",
  "placeholder:text-[var(--color-muted-foreground)]",
  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
);

export function StudyGroupsPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canCreate = perms.includes("Permissions.StudyGroups.StudyGroups.Create");
  const canViewOwn = perms.includes("Permissions.StudyGroups.StudyGroups.ViewOwn");

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [formatFilter, setFormatFilter] = useState<string | null>(null);
  const [courseFilter, setCourseFilter] = useState<string | null>(null);
  const [teacherFilter, setTeacherFilter] = useState<string | null>(null);
  const [sortBy, setSortBy] = useState<SortKey>("code");
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
    [statusFilter, formatFilter, courseFilter, teacherFilter, sortBy],
  );

  const coursesQuery = useQuery({
    queryKey: ["courses", { pageSize: 100, for: "study-groups" }],
    queryFn: () => searchCourses({ pageSize: 100 }),
    staleTime: 60_000,
  });
  const teachersQuery = useQuery({
    queryKey: ["teachers", { pageSize: 100, for: "study-groups" }],
    queryFn: () => searchTeachers({ pageSize: 100 }),
    staleTime: 60_000,
  });

  const courseName = useMemo(() => {
    const m = new Map<string, string>();
    for (const c of coursesQuery.data?.items ?? []) m.set(c.id, c.title);
    return m;
  }, [coursesQuery.data]);
  const teacherName = useMemo(() => {
    const m = new Map<string, string>();
    for (const t of teachersQuery.data?.items ?? []) m.set(t.id, t.displayName);
    return m;
  }, [teachersQuery.data]);

  const courseOptions = useMemo(
    () => (coursesQuery.data?.items ?? []).map((c) => ({ value: c.id, label: c.title })),
    [coursesQuery.data],
  );
  const teacherOptions = useMemo(
    () =>
      (teachersQuery.data?.items ?? []).map((t) => ({ value: t.id, label: t.displayName })),
    [teachersQuery.data],
  );

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debouncedSearch || undefined,
      status: statusFilter === "all" ? null : statusFilter,
      format: (formatFilter as GroupFormat | null) ?? null,
      courseId: courseFilter,
      teacherId: teacherFilter,
      sortBy,
      sortDir:
        sortBy === "name" || sortBy === "code" ? ("asc" as const) : ("desc" as const),
    }),
    [
      pageNumber,
      debouncedSearch,
      statusFilter,
      formatFilter,
      courseFilter,
      teacherFilter,
      sortBy,
    ],
  );

  const query = useQuery({
    queryKey: ["study-groups", queryParams],
    queryFn: () => searchStudyGroups(queryParams),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const filtersActive =
    debouncedSearch.length > 0 ||
    statusFilter !== "all" ||
    formatFilter !== null ||
    courseFilter !== null ||
    teacherFilter !== null;

  const clearFilters = () => {
    setSearch("");
    setStatusFilter("all");
    setFormatFilter(null);
    setCourseFilter(null);
    setTeacherFilter(null);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={UsersRound}
        title="Учебные группы"
        total={data?.totalCount ?? null}
        unit="группа"
        description="Наборы учеников по курсу: состав, преподаватели и жизненный цикл собираются в конструкторе группы."
      >
        {canViewOwn && (
          <Button
            variant="outline"
            asChild
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
          >
            <Link to="/study-groups/my">
              <Users className="size-4" />
              Мои группы
            </Link>
          </Button>
        )}
        {canCreate && (
          <Button
            onClick={() => setCreateOpen(true)}
            className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
          >
            <Plus className="size-4" />
            Новая группа
          </Button>
        )}
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Поиск по коду или названию…"
      />

      <div className="flex flex-wrap items-center gap-2">
        <EntityFilterPill<StatusFilter>
          label="Статус"
          value={statusFilter}
          onChange={setStatusFilter}
          options={[
            { value: "all", label: "Все" },
            { value: "Forming", label: "Набор" },
            { value: "Active", label: "Идёт" },
            { value: "Finished", label: "Завершены" },
            { value: "Cancelled", label: "Отменены" },
          ]}
        />
        <Combobox
          label="Курс"
          value={courseFilter}
          onChange={setCourseFilter}
          options={courseOptions}
          variant="filter"
          searchable
          clearable
        />
        <Combobox
          label="Преподаватель"
          value={teacherFilter}
          onChange={setTeacherFilter}
          options={teacherOptions}
          variant="filter"
          searchable
          clearable
        />
        <Combobox
          label="Формат"
          value={formatFilter}
          onChange={setFormatFilter}
          options={GROUP_FORMATS.map((f) => ({ value: f, label: FORMAT_LABEL[f] }))}
          variant="filter"
          clearable
        />
        <Combobox
          label="Сортировка"
          value={sortBy}
          onChange={(v) => setSortBy((v as SortKey) ?? "code")}
          options={SORT_OPTIONS}
          variant="filter"
        />
      </div>

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={UsersRound}
          title={filtersActive ? "Ничего не найдено" : "Пока нет учебных групп"}
          body={
            filtersActive
              ? "Измените запрос или сбросьте фильтры."
              : "Создайте первую группу — затем наберите состав и активируйте её в конструкторе."
          }
          action={
            filtersActive ? (
              <Button
                variant="outline"
                onClick={clearFilters}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                Сбросить фильтры
              </Button>
            ) : canCreate ? (
              <Button
                onClick={() => setCreateOpen(true)}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                <Plus className="mr-1.5 size-4" />
                Новая группа
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div>
          <div className="space-y-2 md:hidden">
            {items.map((g) => (
              <GroupMobileCard
                key={g.id}
                group={g}
                courseName={courseName.get(g.courseId)}
              />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>Группа</span>
              <span>Статус</span>
              <span className="hidden lg:block">Формат</span>
              <span className="hidden lg:block">Состав</span>
              <span />
            </EntityListHeader>
            {items.map((g, i) => (
              <GroupDesktopRow
                key={g.id}
                group={g}
                courseName={courseName.get(g.courseId)}
                teacherName={teacherName.get(g.primaryTeacherId)}
                isLast={i === items.length - 1}
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

      <CreateStudyGroupDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        teacherOptions={teacherOptions}
        textareaCls={TEXTAREA_CLS}
      />
    </div>
  );
}

function GroupMobileCard({
  group,
  courseName,
}: {
  group: StudyGroupDto;
  courseName?: string;
}) {
  return (
    <EntityMobileCard
      href={`/study-groups/${group.id}`}
      aria-label={`Открыть группу ${group.name}`}
      dim={group.status === "Cancelled"}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={group.name} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {group.name}
            </p>
            <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
              <span className="font-mono">{group.code}</span>
              {courseName ? ` · ${courseName}` : ""}
            </p>
          </div>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 ml-[52px] flex items-center gap-2">
        <EntityStatusBadge tone={STATUS_TONE[group.status]}>
          {STATUS_LABEL[group.status]}
        </EntityStatusBadge>
        <span className="text-[11px] text-[var(--color-muted-foreground)]">
          {group.activeEnrollmentCount}/{group.capacity} · {FORMAT_LABEL[group.format]}
        </span>
      </div>
    </EntityMobileCard>
  );
}

function GroupDesktopRow({
  group,
  courseName,
  teacherName,
  isLast,
}: {
  group: StudyGroupDto;
  courseName?: string;
  teacherName?: string;
  isLast: boolean;
}) {
  return (
    <EntityListRow
      className={DESKTOP_COLS}
      isLast={isLast}
      dim={group.status === "Cancelled"}
    >
      <Link
        to={`/study-groups/${group.id}`}
        className="flex min-w-0 items-center gap-3 outline-none"
      >
        <EntityInitialsAvatar name={group.name} size={36} />
        <div className="min-w-0">
          <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {group.name}
          </span>
          <span className="block truncate text-[11px] text-[var(--color-muted-foreground)]">
            <span className="font-mono">{group.code}</span>
            {courseName ? ` · ${courseName}` : ""}
            {teacherName ? ` · ${teacherName}` : ""}
          </span>
        </div>
      </Link>

      <div className="flex items-center">
        <EntityStatusBadge tone={STATUS_TONE[group.status]}>
          {STATUS_LABEL[group.status]}
        </EntityStatusBadge>
      </div>

      <span className="hidden items-center text-[12px] text-[var(--color-muted-foreground)] lg:flex">
        {FORMAT_LABEL[group.format]}
      </span>

      <span className="hidden items-center gap-1 tabular-nums text-[12px] text-[var(--color-muted-foreground)] lg:flex">
        {group.activeEnrollmentCount}/{group.capacity}
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

function CreateStudyGroupDialog({
  open,
  onClose,
  teacherOptions,
  textareaCls,
}: {
  open: boolean;
  onClose: () => void;
  teacherOptions: ComboboxOption[];
  textareaCls: string;
}) {
  const queryClient = useQueryClient();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [courseId, setCourseId] = useState<string | null>(null);
  const [primaryTeacherId, setPrimaryTeacherId] = useState<string | null>(null);
  const [format, setFormat] = useState<GroupFormat>("Offline");
  const [capacity, setCapacity] = useState("8");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [meetingUrl, setMeetingUrl] = useState("");
  const [notes, setNotes] = useState("");

  useEffect(() => {
    if (!open) {
      setCode("");
      setName("");
      setCourseId(null);
      setPrimaryTeacherId(null);
      setFormat("Offline");
      setCapacity("8");
      setStartDate("");
      setEndDate("");
      setMeetingUrl("");
      setNotes("");
    }
  }, [open]);

  // Only published courses can back a group (server checks IsPublishedAsync).
  const publishedCoursesQuery = useQuery({
    queryKey: ["courses", { pageSize: 100, status: "Published", for: "new-group" }],
    queryFn: () => searchCourses({ status: "Published", pageSize: 100 }),
    enabled: open,
    staleTime: 60_000,
  });
  const courseOptions = useMemo(
    () =>
      (publishedCoursesQuery.data?.items ?? []).map((c) => ({
        value: c.id,
        label: c.title,
      })),
    [publishedCoursesQuery.data],
  );

  const mutation = useMutation({
    mutationFn: (input: CreateStudyGroupInput) => createStudyGroup(input),
    onSuccess: () => {
      toast.success("Группа создана");
      void queryClient.invalidateQueries({ queryKey: ["study-groups"] });
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось создать группу", { description: describe(err) }),
  });

  const cap = Number.parseInt(capacity, 10);
  const valid =
    code.trim().length > 0 &&
    name.trim().length > 0 &&
    !!courseId &&
    !!primaryTeacherId &&
    !Number.isNaN(cap) &&
    cap > 0 &&
    startDate.length > 0;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid || !courseId || !primaryTeacherId) return;
    mutation.mutate({
      code: code.trim(),
      name: name.trim(),
      courseId,
      primaryTeacherId,
      format,
      capacity: cap,
      startDate,
      endDate: endDate || null,
      meetingUrl: meetingUrl.trim() || null,
      notes: notes.trim() || null,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Новая учебная группа</DialogTitle>
            <DialogDescription>
              Группа создаётся в статусе «Набор». Код после создания менять
              нельзя. Курс должен быть опубликован.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="sg-code" label="Код" required hint="Например: ENG-A1-2026">
                <Input
                  id="sg-code"
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                  required
                  autoFocus
                  className="font-mono"
                />
              </Field>
              <Field id="sg-name" label="Название" required>
                <Input
                  id="sg-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                />
              </Field>
            </div>
            <Field id="sg-course" label="Курс" required>
              <Combobox
                id="sg-course"
                label="Курс"
                value={courseId}
                onChange={setCourseId}
                options={courseOptions}
                placeholder={
                  publishedCoursesQuery.isLoading
                    ? "Загрузка…"
                    : courseOptions.length === 0
                      ? "Нет опубликованных курсов"
                      : "Выберите курс"
                }
                searchable
                required
              />
            </Field>
            <Field id="sg-teacher" label="Основной преподаватель" required>
              <Combobox
                id="sg-teacher"
                label="Основной преподаватель"
                value={primaryTeacherId}
                onChange={setPrimaryTeacherId}
                options={teacherOptions}
                searchable
                required
              />
            </Field>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="sg-format" label="Формат" required>
                <Combobox
                  id="sg-format"
                  label="Формат"
                  value={format}
                  onChange={(v) => setFormat((v as GroupFormat) ?? "Offline")}
                  options={GROUP_FORMATS.map((f) => ({
                    value: f,
                    label: FORMAT_LABEL[f],
                  }))}
                />
              </Field>
              <Field id="sg-capacity" label="Вместимость" required>
                <Input
                  id="sg-capacity"
                  type="number"
                  min="1"
                  value={capacity}
                  onChange={(e) => setCapacity(e.target.value)}
                  required
                  className="tabular-nums"
                />
              </Field>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="sg-start" label="Дата старта" required>
                <Input
                  id="sg-start"
                  type="date"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  required
                />
              </Field>
              <Field id="sg-end" label="Дата завершения">
                <Input
                  id="sg-end"
                  type="date"
                  value={endDate}
                  onChange={(e) => setEndDate(e.target.value)}
                />
              </Field>
            </div>
            <Field id="sg-url" label="Ссылка на встречу" hint="Для онлайн/гибрид формата">
              <Input
                id="sg-url"
                type="url"
                value={meetingUrl}
                onChange={(e) => setMeetingUrl(e.target.value)}
                placeholder="https://…"
              />
            </Field>
            <Field id="sg-notes" label="Заметки">
              <textarea
                id="sg-notes"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={2}
                maxLength={2000}
                className={textareaCls}
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || !valid}
              className="gap-1.5"
            >
              <Plus className="h-4 w-4" />
              {mutation.isPending ? "Создание…" : "Создать"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
