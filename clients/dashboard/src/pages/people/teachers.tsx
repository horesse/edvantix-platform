import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronRight, Plus, UserPlus, Users } from "lucide-react";
import { toast } from "sonner";
import {
  createTeacher,
  searchTeachers,
  type CreateTeacherInput,
  type TeacherDto,
  type TeacherStatus,
} from "@/api/people";
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
import { describe } from "@/lib/list-helpers";

const PAGE_SIZE = 20;

const STATUS_TONE: Record<TeacherStatus, EntityStatusTone> = {
  Active: "success",
  Inactive: "default",
};
const STATUS_LABEL: Record<TeacherStatus, string> = {
  Active: "Активен",
  Inactive: "Неактивен",
};

type StatusFilter = TeacherStatus | "all";

const DESKTOP_COLS = "grid-cols-[1fr_120px_24px] lg:grid-cols-[1.6fr_1fr_130px_24px]";

export function TeachersPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canCreate = perms.includes("Permissions.People.Teachers.Create");

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [createOpen, setCreateOpen] = useState(false);

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  useEffect(() => setPageNumber(1), [statusFilter]);

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debouncedSearch || undefined,
      status: statusFilter === "all" ? null : statusFilter,
    }),
    [pageNumber, debouncedSearch, statusFilter],
  );

  const query = useQuery({
    queryKey: ["teachers", queryParams],
    queryFn: () => searchTeachers(queryParams),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0 || statusFilter !== "all";

  const clearFilters = () => {
    setSearch("");
    setStatusFilter("all");
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Users}
        title="Преподаватели"
        total={data?.totalCount ?? null}
        unit="преподаватель"
        description="Преподавательский состав: специализации, ставка, статус."
      >
        {canCreate && (
          <Button
            onClick={() => setCreateOpen(true)}
            className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
          >
            <Plus className="size-4" />
            Новый преподаватель
          </Button>
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
            { value: "Active", label: "Активные" },
            { value: "Inactive", label: "Неактивные" },
          ]}
        />
      </div>

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={Users}
          title={searchActive ? "Ничего не найдено" : "Пока нет преподавателей"}
          body={
            searchActive
              ? "Измените запрос или сбросьте фильтры."
              : "Добавьте первого преподавателя."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={clearFilters} className="h-9 rounded-lg px-4 text-[13px]">
                Сбросить фильтры
              </Button>
            ) : canCreate ? (
              <Button onClick={() => setCreateOpen(true)} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Новый преподаватель
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div>
          <div className="space-y-2 md:hidden">
            {items.map((t) => (
              <TeacherMobileCard key={t.id} teacher={t} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>Преподаватель</span>
              <span className="hidden lg:block">Специализации</span>
              <span>Статус</span>
              <span />
            </EntityListHeader>
            {items.map((t, i) => (
              <TeacherDesktopRow key={t.id} teacher={t} isLast={i === items.length - 1} />
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

      <CreateTeacherDialog open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  );
}

function TeacherMobileCard({ teacher }: { teacher: TeacherDto }) {
  return (
    <EntityMobileCard
      href={`/teachers/${teacher.id}`}
      aria-label={`Открыть преподавателя ${teacher.displayName}`}
      dim={teacher.status === "Inactive"}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={teacher.displayName} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {teacher.displayName}
            </p>
            <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
              {teacher.specializations.join(", ") || teacher.email || "—"}
            </p>
          </div>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 ml-[52px]">
        <EntityStatusBadge tone={STATUS_TONE[teacher.status]}>
          {STATUS_LABEL[teacher.status]}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function TeacherDesktopRow({ teacher, isLast }: { teacher: TeacherDto; isLast: boolean }) {
  return (
    <EntityListRow className={DESKTOP_COLS} isLast={isLast} dim={teacher.status === "Inactive"}>
      <Link to={`/teachers/${teacher.id}`} className="flex min-w-0 items-center gap-3 outline-none">
        <EntityInitialsAvatar name={teacher.displayName} size={36} />
        <div className="min-w-0">
          <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {teacher.displayName}
          </span>
          <span
            className={cn(
              "block truncate text-[12px] text-[var(--color-muted-foreground)]",
              !teacher.email && "italic opacity-60",
            )}
          >
            {teacher.email || teacher.phone || "нет контактов"}
          </span>
        </div>
      </Link>

      <span className="hidden items-center truncate text-[12px] text-[var(--color-muted-foreground)] lg:flex">
        {teacher.specializations.join(", ") || "—"}
      </span>

      <div className="flex items-center">
        <EntityStatusBadge tone={STATUS_TONE[teacher.status]}>
          {STATUS_LABEL[teacher.status]}
        </EntityStatusBadge>
      </div>

      <div className="flex items-center justify-end">
        <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
      </div>
    </EntityListRow>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Create dialog
// ───────────────────────────────────────────────────────────────────────

function CreateTeacherDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [lastName, setLastName] = useState("");
  const [firstName, setFirstName] = useState("");
  const [middleName, setMiddleName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [specializations, setSpecializations] = useState("");
  const [hourlyRate, setHourlyRate] = useState("");
  const [dupCount, setDupCount] = useState(0);

  useEffect(() => {
    if (!open) {
      setLastName("");
      setFirstName("");
      setMiddleName("");
      setPhone("");
      setEmail("");
      setSpecializations("");
      setHourlyRate("");
      setDupCount(0);
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: (input: CreateTeacherInput) => createTeacher(input),
    onSuccess: () => {
      toast.success("Преподаватель создан");
      void queryClient.invalidateQueries({ queryKey: ["teachers"] });
      onClose();
    },
    onError: (err) => toast.error("Не удалось создать преподавателя", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const specs = specializations
      .split(",")
      .map((s) => s.trim())
      .filter(Boolean);
    const rate = hourlyRate.trim() ? Number(hourlyRate) : null;
    mutation.mutate({
      lastName: lastName.trim(),
      firstName: firstName.trim(),
      middleName: middleName.trim() || null,
      phone: phone.trim(),
      email: email.trim(),
      specializations: specs.length > 0 ? specs : null,
      hourlyRate: rate != null && !Number.isNaN(rate) ? rate : null,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Новый преподаватель</DialogTitle>
            <DialogDescription>
              Специализации перечислите через запятую. Ставку и учётную запись можно
              задать позже в карточке.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="t-last" label="Фамилия" required>
                <Input id="t-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required autoFocus />
              </Field>
              <Field id="t-first" label="Имя" required>
                <Input id="t-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
              </Field>
            </div>
            <Field id="t-middle" label="Отчество">
              <Input id="t-middle" value={middleName} onChange={(e) => setMiddleName(e.target.value)} />
            </Field>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="t-phone" label="Телефон" required>
                <Input id="t-phone" value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="+7 …" required />
              </Field>
              <Field id="t-email" label="E-mail" required>
                <Input id="t-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
              </Field>
            </div>
            <Field id="t-specs" label="Специализации" hint="Через запятую: Математика, Физика">
              <Input id="t-specs" value={specializations} onChange={(e) => setSpecializations(e.target.value)} />
            </Field>
            <Field id="t-rate" label="Часовая ставка" hint="Необязательно.">
              <Input id="t-rate" type="number" min="0" step="0.01" value={hourlyRate} onChange={(e) => setHourlyRate(e.target.value)} />
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
