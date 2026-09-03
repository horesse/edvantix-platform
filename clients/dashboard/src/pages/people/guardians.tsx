import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronRight, HeartHandshake, Plus, UserPlus } from "lucide-react";
import { toast } from "sonner";
import {
  createGuardian,
  searchGuardians,
  type CreateGuardianInput,
  type GuardianDto,
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
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityMobileCard,
  EntityPageHeader,
  EntityPager,
  EntitySearch,
  Field,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";

const PAGE_SIZE = 20;
const DESKTOP_COLS = "grid-cols-[1fr_24px] lg:grid-cols-[1.4fr_1fr_24px]";

export function GuardiansPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canCreate = perms.includes("Permissions.People.Guardians.Create");

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  const queryParams = useMemo(
    () => ({ pageNumber, pageSize: PAGE_SIZE, search: debouncedSearch || undefined }),
    [pageNumber, debouncedSearch],
  );

  const query = useQuery({
    queryKey: ["guardians", queryParams],
    queryFn: () => searchGuardians(queryParams),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={HeartHandshake}
        title="Представители"
        total={data?.totalCount ?? null}
        unit="представитель"
        description="Родители и законные представители учеников."
      >
        {canCreate && (
          <Button
            onClick={() => setCreateOpen(true)}
            className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
          >
            <Plus className="size-4" />
            Новый представитель
          </Button>
        )}
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Поиск по фамилии, имени, телефону, e-mail…"
      />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={HeartHandshake}
          title={searchActive ? "Ничего не найдено" : "Пока нет представителей"}
          body={
            searchActive
              ? "Измените поисковый запрос."
              : "Добавьте представителя или привяжите его к ученику из карточки ученика."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Сбросить поиск
              </Button>
            ) : canCreate ? (
              <Button onClick={() => setCreateOpen(true)} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Новый представитель
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div>
          <div className="space-y-2 md:hidden">
            {items.map((g) => (
              <GuardianMobileCard key={g.id} guardian={g} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>Представитель</span>
              <span className="hidden lg:block">Контакты</span>
              <span />
            </EntityListHeader>
            {items.map((g, i) => (
              <GuardianDesktopRow key={g.id} guardian={g} isLast={i === items.length - 1} />
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

      <CreateGuardianDialog open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  );
}

function GuardianMobileCard({ guardian }: { guardian: GuardianDto }) {
  return (
    <EntityMobileCard href={`/guardians/${guardian.id}`} aria-label={`Открыть представителя ${guardian.displayName}`}>
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={guardian.displayName} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {guardian.displayName}
            </p>
            <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
              {guardian.phone || guardian.email || "нет контактов"}
            </p>
          </div>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
    </EntityMobileCard>
  );
}

function GuardianDesktopRow({ guardian, isLast }: { guardian: GuardianDto; isLast: boolean }) {
  return (
    <EntityListRow className={DESKTOP_COLS} isLast={isLast}>
      <Link to={`/guardians/${guardian.id}`} className="flex min-w-0 items-center gap-3 outline-none">
        <EntityInitialsAvatar name={guardian.displayName} size={36} />
        <div className="min-w-0">
          <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {guardian.displayName}
          </span>
          <span
            className={cn(
              "block truncate text-[12px] text-[var(--color-muted-foreground)] lg:hidden",
              !guardian.email && !guardian.phone && "italic opacity-60",
            )}
          >
            {guardian.phone || guardian.email || "нет контактов"}
          </span>
        </div>
      </Link>

      <span className="hidden items-center truncate text-[12px] text-[var(--color-muted-foreground)] lg:flex">
        {[guardian.phone, guardian.email].filter(Boolean).join(" · ") || "—"}
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

function CreateGuardianDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [lastName, setLastName] = useState("");
  const [firstName, setFirstName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [dupCount, setDupCount] = useState(0);

  useEffect(() => {
    if (!open) {
      setLastName("");
      setFirstName("");
      setPhone("");
      setEmail("");
      setDupCount(0);
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: (input: CreateGuardianInput) => createGuardian(input),
    onSuccess: () => {
      toast.success("Представитель создан");
      void queryClient.invalidateQueries({ queryKey: ["guardians"] });
      onClose();
    },
    onError: (err) => toast.error("Не удалось создать представителя", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate({
      lastName: lastName.trim(),
      firstName: firstName.trim(),
      phone: phone.trim(),
      email: email.trim(),
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Новый представитель</DialogTitle>
            <DialogDescription>
              Привязать представителя к ученику можно из карточки ученика на вкладке
              «Представители».
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="g-last" label="Фамилия" required>
                <Input id="g-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required autoFocus />
              </Field>
              <Field id="g-first" label="Имя" required>
                <Input id="g-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
              </Field>
            </div>
            <Field id="g-phone" label="Телефон" required>
              <Input id="g-phone" value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="+7 …" required />
            </Field>
            <Field id="g-email" label="E-mail" required>
              <Input id="g-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
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
