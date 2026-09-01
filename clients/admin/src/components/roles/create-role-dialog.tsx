import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Shield } from "lucide-react";
import { toast } from "sonner";
import { upsertRole, type RoleDto } from "@/api/roles";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/list";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogBody,
  DialogFooter,
} from "@/components/ui/dialog";
import { ApiRequestError } from "@/lib/api-client";

// ─── Schema (identical to the old create page) ───────────────────────────────

const schema = z.object({
  name: z
    .string()
    .trim()
    .min(2, "Не меньше 2 символов.")
    .max(64, "Не больше 64 символов."),
  description: z.string().trim().max(256, "Не больше 256 символов.").optional(),
});

type FormValues = z.infer<typeof schema>;

// ─── Dialog ───────────────────────────────────────────────────────────────────

export function CreateRoleDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", description: "" },
  });

  const mutation = useMutation<RoleDto, Error, FormValues>({
    // Pass values via mutate(arg) — no closed-over state captured at submit time.
    mutationFn: (values) =>
      upsertRole({
        id: "",
        name: values.name,
        description: values.description?.trim() ? values.description : null,
      }),
    onSuccess: (result) => {
      toast.success(`Роль ${result.name} создана`);
      queryClient.invalidateQueries({ queryKey: ["roles"] });
      handleClose();
      navigate(`/roles/${result.id}`);
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : err.message;
      toast.error("Не удалось создать", { description: detail });
    },
  });

  function handleClose() {
    reset();
    onOpenChange(false);
  }

  const onSubmit = handleSubmit((values) => mutation.mutate(values));
  const submitting = isSubmitting || mutation.isPending;

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) handleClose();
        else onOpenChange(true);
      }}
    >
      <DialogContent size="md">
        {/* ── Header ── */}
        <DialogHeader>
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className="relative grid h-9 w-9 shrink-0 place-items-center overflow-hidden rounded-xl
                bg-[oklch(from_var(--color-primary)_l_c_h_/_0.12)]
                text-[var(--color-primary)]
                ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]"
            >
              <Shield className="h-[18px] w-[18px]" />
            </span>
            <div className="min-w-0">
              <DialogTitle className="text-[16px]">Новая роль</DialogTitle>
            </div>
          </div>
          <DialogDescription className="mt-1">
            Создайте роль, затем выдайте ей права на карточке. Название роли видно при назначении
            ролей пользователям — выбирайте понятное.
          </DialogDescription>
        </DialogHeader>

        {/* ── Form ── */}
        <form onSubmit={onSubmit}>
          <DialogBody className="space-y-4">
            <Field id="cr-name" label="Название" required error={errors.name?.message}>
              <Input
                id="cr-name"
                placeholder="Агент поддержки"
                autoComplete="off"
                aria-invalid={errors.name ? true : undefined}
                {...register("name")}
              />
            </Field>
            <Field
              id="cr-description"
              label="Описание"
              hint="Необязательно. Простыми словами — для чего эта роль."
              error={errors.description?.message}
            >
              <Input
                id="cr-description"
                placeholder="Входящая поддержка · биллинг только для чтения"
                aria-invalid={errors.description ? true : undefined}
                {...register("description")}
              />
            </Field>
          </DialogBody>

          {/* ── Footer ── */}
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={handleClose}
              disabled={submitting}
            >
              Отмена
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting ? "Сохранение…" : "Создать роль"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
