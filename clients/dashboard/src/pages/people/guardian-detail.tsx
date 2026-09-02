import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ChevronRight,
  HeartHandshake,
  Link2,
  Link2Off,
  Mail,
  Pencil,
  Phone,
  Trash2,
  Users,
} from "lucide-react";
import { toast } from "sonner";
import {
  deleteGuardian,
  getGuardianById,
  getGuardianStudents,
  linkGuardianUser,
  unlinkGuardianUser,
  updateGuardian,
  type GuardianDto,
  type StudentStatus,
  type UpdateGuardianInput,
} from "@/api/people";
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
  EntityDetailAvatar,
  EntityDetailBack,
  EntityDetailHero,
  EntityDetailMeta,
  EntityDetailSection,
  EntityInitialsAvatar,
  EntityStatusBadge,
  Field,
  type EntityStatusTone,
} from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { ConfirmDeleteDialog, LinkUserDialog } from "./student-detail";

const STUDENT_STATUS_TONE: Record<StudentStatus, EntityStatusTone> = {
  Lead: "info",
  Active: "success",
  Paused: "warning",
  Archived: "default",
};
const STUDENT_STATUS_LABEL: Record<StudentStatus, string> = {
  Lead: "Лид",
  Active: "Активен",
  Paused: "Пауза",
  Archived: "Архив",
};

export function GuardianDetailPage() {
  const { guardianId = "" } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const perms = useAuth().user?.permissions ?? [];
  const canUpdate = perms.includes("Permissions.People.Guardians.Update");
  const canDelete = perms.includes("Permissions.People.Guardians.Delete");
  const canViewStudents = perms.includes("Permissions.People.Students.View");

  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [linkOpen, setLinkOpen] = useState(false);

  const query = useQuery({
    queryKey: ["guardian", guardianId],
    queryFn: () => getGuardianById(guardianId),
    enabled: Boolean(guardianId),
  });

  const guardian = query.data;

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["guardian", guardianId] });
    void queryClient.invalidateQueries({ queryKey: ["guardians"] });
  };

  const unlinkMut = useMutation({
    mutationFn: (id: string) => unlinkGuardianUser(id),
    onSuccess: () => {
      toast.success("Учётная запись отвязана");
      invalidate();
    },
    onError: (e) => toast.error("Не удалось отвязать", { description: describe(e) }),
  });

  if (query.isLoading) {
    return (
      <div>
        <EntityDetailBack to="/guardians" label="К списку представителей" />
        <div className="h-40 animate-pulse rounded-xl bg-[var(--color-muted)]" />
      </div>
    );
  }

  if (query.isError || !guardian) {
    return (
      <div>
        <EntityDetailBack to="/guardians" label="К списку представителей" />
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {query.error ? describe(query.error) : "Представитель не найден"}
        </div>
      </div>
    );
  }

  return (
    <div>
      <EntityDetailBack to="/guardians" label="К списку представителей" />

      <EntityDetailHero
        avatar={<EntityDetailAvatar name={guardian.displayName} icon={HeartHandshake} />}
        title={guardian.displayName}
        actions={
          canUpdate ? (
            <>
              <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setEditOpen(true)}>
                <Pencil className="size-3.5" />
                Изменить
              </Button>
              {canDelete && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="gap-1.5 text-[var(--color-destructive)]"
                  onClick={() => setDeleteOpen(true)}
                >
                  <Trash2 className="size-3.5" />
                </Button>
              )}
            </>
          ) : undefined
        }
        meta={
          <>
            <EntityDetailMeta icon={Phone}>{guardian.phone || "—"}</EntityDetailMeta>
            <EntityDetailMeta icon={Mail}>{guardian.email || "—"}</EntityDetailMeta>
          </>
        }
      />

      <div className="space-y-4">
        <EntityDetailSection title="Учётная запись" icon={Link2}>
          {guardian.userId ? (
            <div className="flex items-center justify-between gap-3">
              <div className="min-w-0 text-[13px]">
                <p className="text-[var(--color-foreground)]">Привязана учётная запись</p>
                <code className="text-[12px] text-[var(--color-muted-foreground)]">{guardian.userId}</code>
              </div>
              {canUpdate && (
                <Button
                  variant="outline"
                  size="sm"
                  className="gap-1.5"
                  disabled={unlinkMut.isPending}
                  onClick={() => unlinkMut.mutate(guardian.id)}
                >
                  <Link2Off className="size-3.5" />
                  Отвязать
                </Button>
              )}
            </div>
          ) : (
            <div className="flex items-center justify-between gap-3">
              <p className="text-[13px] text-[var(--color-muted-foreground)]">
                Представитель не привязан к учётной записи — доступ в кабинет невозможен.
              </p>
              {canUpdate && (
                <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setLinkOpen(true)}>
                  <Link2 className="size-3.5" />
                  Привязать
                </Button>
              )}
            </div>
          )}
        </EntityDetailSection>

        {canViewStudents && <WardsSection guardianId={guardian.id} />}
      </div>

      <EditGuardianDialog open={editOpen} onClose={() => setEditOpen(false)} guardian={guardian} onSaved={invalidate} />
      <LinkUserDialog
        open={linkOpen}
        onClose={() => setLinkOpen(false)}
        onSubmit={(userId) => linkGuardianUser(guardian.id, userId)}
        onSaved={invalidate}
      />
      <ConfirmDeleteDialog
        open={deleteOpen}
        onClose={() => setDeleteOpen(false)}
        name={guardian.displayName}
        onConfirm={async () => {
          await deleteGuardian(guardian.id);
          toast.success("Представитель удалён");
          void queryClient.invalidateQueries({ queryKey: ["guardians"] });
          navigate("/guardians");
        }}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Подопечные — the students this guardian is responsible for. Read-only
//  reverse view of the student card's «Представители» tab; the link itself
//  (relation, primary payer) is authored there.
// ───────────────────────────────────────────────────────────────────────

function WardsSection({ guardianId }: { guardianId: string }) {
  const query = useQuery({
    queryKey: ["guardian", guardianId, "students"],
    queryFn: () => getGuardianStudents(guardianId),
    enabled: Boolean(guardianId),
  });

  const items = query.data ?? [];

  return (
    <EntityDetailSection
      title="Подопечные"
      icon={Users}
      description="Связи задаются в карточке ученика на вкладке «Представители»."
    >
      {query.isLoading ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : query.isError ? (
        <p className="text-[13px] text-[var(--color-destructive)]">{describe(query.error)}</p>
      ) : items.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          К этому представителю не привязан ни один ученик.
        </p>
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
          {items.map((link) => (
            <li key={link.id} className="py-3 first:pt-0 last:pb-0">
              <Link
                to={`/students/${link.studentId}`}
                className="flex items-center justify-between gap-3"
              >
                <div className="flex min-w-0 items-center gap-3">
                  <EntityInitialsAvatar name={link.student.displayName} size={36} />
                  <div className="min-w-0">
                    <p className="truncate text-[13px] font-medium text-[var(--color-foreground)]">
                      {link.student.displayName}
                      {link.isPrimaryPayer && (
                        <EntityStatusBadge tone="success" className="ml-2">
                          Плательщик
                        </EntityStatusBadge>
                      )}
                    </p>
                    <p className="truncate text-[11.5px] text-[var(--color-muted-foreground)]">
                      <EntityStatusBadge tone={STUDENT_STATUS_TONE[link.student.status]}>
                        {STUDENT_STATUS_LABEL[link.student.status]}
                      </EntityStatusBadge>{" "}
                      · {link.relation || "—"}
                    </p>
                  </div>
                </div>
                <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </EntityDetailSection>
  );
}

function EditGuardianDialog({
  open,
  onClose,
  guardian,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  guardian: GuardianDto;
  onSaved: () => void;
}) {
  const [lastName, setLastName] = useState(guardian.lastName);
  const [firstName, setFirstName] = useState(guardian.firstName);
  const [phone, setPhone] = useState(guardian.phone);
  const [email, setEmail] = useState(guardian.email);

  useEffect(() => {
    if (open) {
      setLastName(guardian.lastName);
      setFirstName(guardian.firstName);
      setPhone(guardian.phone);
      setEmail(guardian.email);
    }
  }, [open, guardian]);

  const mutation = useMutation({
    mutationFn: (input: UpdateGuardianInput) => updateGuardian(input),
    onSuccess: () => {
      toast.success("Изменения сохранены");
      onSaved();
      onClose();
    },
    onError: (e) => toast.error("Не удалось сохранить", { description: describe(e) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate({
      guardianId: guardian.id,
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
            <DialogTitle>Изменить представителя</DialogTitle>
            <DialogDescription>Контактные данные представителя.</DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="eg-last" label="Фамилия" required>
                <Input id="eg-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
              </Field>
              <Field id="eg-first" label="Имя" required>
                <Input id="eg-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
              </Field>
            </div>
            <Field id="eg-phone" label="Телефон" required>
              <Input id="eg-phone" value={phone} onChange={(e) => setPhone(e.target.value)} required />
            </Field>
            <Field id="eg-email" label="E-mail" required>
              <Input id="eg-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Сохранение…" : "Сохранить"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
