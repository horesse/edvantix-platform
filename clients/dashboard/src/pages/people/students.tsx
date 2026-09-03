import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronRight, GraduationCap, Plus, Upload, UserPlus } from "lucide-react";
import { toast } from "sonner";
import {
  createStudent,
  searchStudents,
  type CreateStudentInput,
  type StudentDto,
  type StudentStatus,
} from "@/api/people";
import { searchUsers, type UserDto } from "@/api/identity";
import { useAuth } from "@/auth/use-auth";
import { DuplicatePersonNotice } from "@/components/duplicate-person-notice";
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
  type EntityStatusTone,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe, formatDate } from "@/lib/list-helpers";

const PAGE_SIZE = 20;

const STATUS_TONE: Record<StudentStatus, EntityStatusTone> = {
  Lead: "info",
  Active: "success",
  Paused: "warning",
  Archived: "default",
};

const STATUS_LABEL: Record<StudentStatus, string> = {
  Lead: "Лид",
  Active: "Активен",
  Paused: "Пауза",
  Archived: "Архив",
};

type StatusFilter = StudentStatus | "all";

function userLabel(u: UserDto): string {
  const name = [u.firstName, u.lastName].filter(Boolean).join(" ");
  return name || u.userName || u.email || u.id || "—";
}

const DESKTOP_COLS =
  "grid-cols-[1fr_120px_24px] lg:grid-cols-[1.6fr_150px_160px_24px]";

export function StudentsPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canCreate = perms.includes("Permissions.People.Students.Create");

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [managerFilter, setManagerFilter] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  useEffect(() => setPageNumber(1), [statusFilter, managerFilter]);

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debouncedSearch || undefined,
      status: statusFilter === "all" ? null : statusFilter,
      managerUserId: managerFilter,
    }),
    [pageNumber, debouncedSearch, statusFilter, managerFilter],
  );

  const query = useQuery({
    queryKey: ["students", queryParams],
    queryFn: () => searchStudents(queryParams),
    placeholderData: keepPreviousData,
  });

  // Manager filter options — tenant users (View Users is IsBasic, so any member can list).
  const usersQuery = useQuery({
    queryKey: ["identity", "users", { pageSize: 100, sort: "userName asc" }],
    queryFn: () => searchUsers({ pageSize: 100, sort: "userName asc" }),
    staleTime: 60_000,
  });
  const managerOptions = useMemo(
    () =>
      (usersQuery.data?.items ?? [])
        .filter((u): u is UserDto & { id: string } => Boolean(u.id))
        .map((u) => ({ value: u.id, label: userLabel(u) })),
    [usersQuery.data],
  );

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive =
    debouncedSearch.length > 0 || statusFilter !== "all" || managerFilter !== null;

  const clearFilters = () => {
    setSearch("");
    setStatusFilter("all");
    setManagerFilter(null);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={GraduationCap}
        title="Ученики"
        total={data?.totalCount ?? null}
        unit="ученик"
        description="Профили учеников школы: контакты, статус, ответственный менеджер."
      >
        {canCreate && (
          <>
            <Button
              variant="outline"
              asChild
              className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
            >
              <Link to="/students/import">
                <Upload className="size-4" />
                Импорт CSV
              </Link>
            </Button>
            <Button
              onClick={() => setCreateOpen(true)}
              className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
            >
              <Plus className="size-4" />
              Новый ученик
            </Button>
          </>
        )}
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Поиск по фамилии, имени, телефону, e-mail…"
      />

      <div className="flex flex-wrap items-center gap-2">
        <EntityFilterPill<StatusFilter>
          label="Статус"
          value={statusFilter}
          onChange={setStatusFilter}
          options={[
            { value: "all", label: "Все" },
            { value: "Lead", label: "Лиды" },
            { value: "Active", label: "Активные" },
            { value: "Paused", label: "Пауза" },
            { value: "Archived", label: "Архив" },
          ]}
        />
        <Combobox
          label="Менеджер"
          value={managerFilter}
          onChange={setManagerFilter}
          options={managerOptions}
          variant="filter"
          searchable
          clearable
        />
      </div>

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={GraduationCap}
          title={searchActive ? "Ничего не найдено" : "Пока нет учеников"}
          body={
            searchActive
              ? "Измените запрос или сбросьте фильтры."
              : "Добавьте первого ученика или импортируйте список из CSV."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={clearFilters} className="h-9 rounded-lg px-4 text-[13px]">
                Сбросить фильтры
              </Button>
            ) : canCreate ? (
              <Button onClick={() => setCreateOpen(true)} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Новый ученик
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div>
          <div className="space-y-2 md:hidden">
            {items.map((s) => (
              <StudentMobileCard key={s.id} student={s} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>Ученик</span>
              <span>Статус</span>
              <span className="hidden lg:block">Зачислен</span>
              <span />
            </EntityListHeader>
            {items.map((s, i) => (
              <StudentDesktopRow key={s.id} student={s} isLast={i === items.length - 1} />
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

      <CreateStudentDialog open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  );
}

function StudentMobileCard({ student }: { student: StudentDto }) {
  return (
    <EntityMobileCard
      href={`/students/${student.id}`}
      aria-label={`Открыть ученика ${student.displayName}`}
      dim={student.status === "Archived"}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={student.displayName} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {student.displayName}
            </p>
            <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
              {student.phone || student.email || "нет контактов"}
            </p>
          </div>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 ml-[52px]">
        <EntityStatusBadge tone={STATUS_TONE[student.status]}>
          {STATUS_LABEL[student.status]}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function StudentDesktopRow({ student, isLast }: { student: StudentDto; isLast: boolean }) {
  return (
    <EntityListRow
      className={DESKTOP_COLS}
      isLast={isLast}
      dim={student.status === "Archived"}
    >
      <Link to={`/students/${student.id}`} className="flex min-w-0 items-center gap-3 outline-none">
        <EntityInitialsAvatar name={student.displayName} size={36} />
        <div className="min-w-0">
          <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {student.displayName}
          </span>
          <span
            className={cn(
              "block truncate text-[12px] text-[var(--color-muted-foreground)]",
              !student.email && !student.phone && "italic opacity-60",
            )}
          >
            {student.phone || student.email || "нет контактов"}
          </span>
        </div>
      </Link>

      <div className="flex items-center">
        <EntityStatusBadge tone={STATUS_TONE[student.status]}>
          {STATUS_LABEL[student.status]}
        </EntityStatusBadge>
      </div>

      <span className="hidden items-center text-[12px] text-[var(--color-muted-foreground)] lg:flex">
        {formatDate(student.enrolledAtUtc)}
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

function CreateStudentDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const [lastName, setLastName] = useState("");
  const [firstName, setFirstName] = useState("");
  const [middleName, setMiddleName] = useState("");
  const [birthDate, setBirthDate] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [source, setSource] = useState("");
  const [dupCount, setDupCount] = useState(0);

  useEffect(() => {
    if (!open) {
      setLastName("");
      setFirstName("");
      setMiddleName("");
      setBirthDate("");
      setPhone("");
      setEmail("");
      setSource("");
      setDupCount(0);
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: (input: CreateStudentInput) => createStudent(input),
    onSuccess: () => {
      toast.success("Ученик создан");
      void queryClient.invalidateQueries({ queryKey: ["students"] });
      onClose();
    },
    onError: (err) => toast.error("Не удалось создать ученика", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate({
      lastName: lastName.trim(),
      firstName: firstName.trim(),
      middleName: middleName.trim() || null,
      birthDate,
      phone: phone.trim(),
      email: email.trim(),
      managerUserId: user?.id ?? "",
      source: source.trim() || null,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Новый ученик</DialogTitle>
            <DialogDescription>
              Ответственным менеджером назначается текущий пользователь — его можно
              изменить позже в карточке ученика.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="s-last" label="Фамилия" required>
                <Input id="s-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required autoFocus />
              </Field>
              <Field id="s-first" label="Имя" required>
                <Input id="s-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
              </Field>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="s-middle" label="Отчество">
                <Input id="s-middle" value={middleName} onChange={(e) => setMiddleName(e.target.value)} />
              </Field>
              <Field id="s-birth" label="Дата рождения" required>
                <Input id="s-birth" type="date" value={birthDate} onChange={(e) => setBirthDate(e.target.value)} required />
              </Field>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="s-phone" label="Телефон" required>
                <Input id="s-phone" value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="+7 …" required />
              </Field>
              <Field id="s-email" label="E-mail" required>
                <Input id="s-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
              </Field>
            </div>
            <Field id="s-source" label="Источник" hint="Откуда пришёл ученик (необязательно).">
              <Input id="s-source" value={source} onChange={(e) => setSource(e.target.value)} placeholder="Сайт, рекомендация…" />
            </Field>

            <DuplicatePersonNotice
              lastName={lastName}
              firstName={firstName}
              phone={phone}
              email={email}
              onCountChange={setDupCount}
            />
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending} className="gap-1.5">
              <UserPlus className="h-4 w-4" />
              {mutation.isPending
                ? "Создание…"
                : dupCount > 0
                  ? "Всё равно создать"
                  : "Создать"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
