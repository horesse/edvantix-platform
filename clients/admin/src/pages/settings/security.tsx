import { forwardRef, useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  AlertCircle,
  ClipboardCheck,
  Copy,
  Eye,
  EyeOff,
  KeyRound,
  ShieldCheck,
  ShieldOff,
} from "lucide-react";
import { toast } from "sonner";
import { changePassword, getMyProfile } from "@/api/users";
import {
  disableTwoFactor,
  enrollTwoFactor,
  verifyEnrollTwoFactor,
  type TwoFactorEnrollmentResponse,
} from "@/api/two-factor";
import { Button } from "@/components/ui/button";
import { Input, type InputProps } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import {
  ErrorBand,
  Field,
  LoadingRow,
  SettingsSection,
} from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

/**
 * SecuritySettings — combines password change + 2FA enrollment/disable
 * into one tab. Profile query fuels both: we read twoFactorEnabled to
 * decide whether to render the enroll wizard or the disable controls.
 * Password change is driven through a Dialog (mirrors dashboard pattern).
 */
export function SecuritySettings() {
  const profile = useQuery({ queryKey: ["identity", "profile"], queryFn: getMyProfile });

  if (profile.isLoading) return <LoadingRow label="Загрузка настроек безопасности" />;
  if (profile.isError) {
    return (
      <ErrorBand
        message={
          profile.error instanceof ApiRequestError
            ? (profile.error.problem?.detail ?? profile.error.message)
            : "Не удалось загрузить настройки безопасности."
        }
      />
    );
  }

  const twoFactorEnabled = profile.data?.twoFactorEnabled ?? false;

  return (
    <div className="space-y-5 fsh-enter">
      <PasswordSection />
      <TwoFactorSection enabled={twoFactorEnabled} />
    </div>
  );
}

// ─── Password section ────────────────────────────────────────────────────

const passwordSchema = z
  .object({
    current: z.string().min(1, "Обязательное поле."),
    next: z.string().min(8, "Не меньше 8 символов."),
    confirm: z.string().min(8),
  })
  .refine((v) => v.next === v.confirm, {
    path: ["confirm"],
    message: "Пароли не совпадают.",
  })
  .refine((v) => v.next !== v.current, {
    path: ["next"],
    message: "Новый пароль должен отличаться от текущего.",
  });

type PasswordValues = z.infer<typeof passwordSchema>;

/**
 * RevealInput — password Input with a first-class show/hide eye toggle
 * (mirrors the login screen). Forwards the ref so react-hook-form can
 * register the field for `reset()` and validation focus.
 */
const RevealInput = forwardRef<HTMLInputElement, InputProps>(
  ({ className, ...props }, ref) => {
    const [show, setShow] = useState(false);
    return (
      <div className="relative">
        <Input
          ref={ref}
          type={show ? "text" : "password"}
          className={cn("pr-10", className)}
          {...props}
        />
        <button
          type="button"
          tabIndex={-1}
          onClick={() => setShow((s) => !s)}
          aria-label={show ? "Скрыть пароль" : "Показать пароль"}
          className="absolute right-1.5 top-1/2 grid size-6 -translate-y-1/2 place-items-center rounded-md text-[var(--color-muted-foreground)] outline-none transition-colors hover:text-[var(--color-foreground)] focus-visible:ring-2 focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]"
        >
          {show ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
        </button>
      </div>
    );
  },
);
RevealInput.displayName = "RevealInput";

function PasswordSection() {
  const [dialogOpen, setDialogOpen] = useState(false);

  return (
    <>
      <SettingsSection
        title="Пароль"
        icon={KeyRound}
        description="Используется для входа в консоль. Выберите надёжную уникальную фразу от 16 символов."
      >
        <div className="flex items-center justify-between gap-4">
          <p className="text-sm text-[var(--color-muted-foreground)]">
            Смена пароля не отзывает другие сессии автоматически — завершите их на вкладке «Сессии».
          </p>
          <Button variant="outline" size="sm" onClick={() => setDialogOpen(true)}>
            Сменить пароль
          </Button>
        </div>
      </SettingsSection>

      <ChangePasswordDialog open={dialogOpen} onOpenChange={setDialogOpen} />
    </>
  );
}

function ChangePasswordDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (next: boolean) => void;
}) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<PasswordValues>({
    resolver: zodResolver(passwordSchema),
    defaultValues: { current: "", next: "", confirm: "" },
  });

  // Reset form every time the dialog opens so stale values don't bleed.
  useEffect(() => {
    if (open) reset({ current: "", next: "", confirm: "" });
  }, [open, reset]);

  const mutation = useMutation({
    mutationFn: (v: PasswordValues) =>
      changePassword({
        password: v.current,
        newPassword: v.next,
        confirmNewPassword: v.confirm,
      }),
    onSuccess: () => {
      toast.success("Пароль изменён", {
        description: "Другие активные сессии остаются действительными, пока вы их не отзовёте.",
      });
      onOpenChange(false);
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? (err.problem?.detail ?? err.problem?.title ?? err.message)
          : (err as Error).message;
      toast.error("Не удалось изменить", { description: detail });
    },
  });

  const onSubmit = handleSubmit((v) => mutation.mutate(v));
  const submitting = isSubmitting || mutation.isPending;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <div className="flex items-center gap-3">
            <span className="grid size-9 shrink-0 place-items-center rounded-lg bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.20)]">
              <KeyRound className="size-4 text-[var(--color-primary)]" />
            </span>
            <DialogTitle>Смена пароля</DialogTitle>
          </div>
          <DialogDescription>
            Выход на других устройствах не происходит автоматически — завершите их на вкладке «Сессии» после смены пароля.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="contents" noValidate>
          <DialogBody className="space-y-4">
            <Field id="pw-current" label="Текущий пароль" required error={errors.current?.message}>
              <RevealInput
                id="pw-current"
                autoComplete="current-password"
                className="font-mono"
                aria-invalid={errors.current ? true : undefined}
                autoFocus
                {...register("current")}
              />
            </Field>
            <Field
              id="pw-next"
              label="Новый пароль"
              required
              hint="Не меньше 8 символов."
              error={errors.next?.message}
            >
              <RevealInput
                id="pw-next"
                autoComplete="new-password"
                className="font-mono"
                aria-invalid={errors.next ? true : undefined}
                {...register("next")}
              />
            </Field>
            <Field
              id="pw-confirm"
              label="Повтор нового пароля"
              required
              error={errors.confirm?.message}
            >
              <RevealInput
                id="pw-confirm"
                autoComplete="new-password"
                className="font-mono"
                aria-invalid={errors.confirm ? true : undefined}
                {...register("confirm")}
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => onOpenChange(false)}
              disabled={submitting}
            >
              Отмена
            </Button>
            <Button type="submit" disabled={submitting}>
              <KeyRound className="mr-1 h-3.5 w-3.5" />
              {submitting ? "Обновление…" : "Обновить пароль"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─── Two-factor section ──────────────────────────────────────────────────

function TwoFactorSection({ enabled }: { enabled: boolean }) {
  if (enabled) return <TwoFactorDisable />;
  return <TwoFactorEnroll />;
}

function TwoFactorEnroll() {
  const queryClient = useQueryClient();
  const [enrollment, setEnrollment] = useState<TwoFactorEnrollmentResponse | null>(null);
  const [code, setCode] = useState("");
  const [qrSvg, setQrSvg] = useState<string | null>(null);
  const [copiedKey, setCopiedKey] = useState(false);

  const beginMutation = useMutation({
    mutationFn: enrollTwoFactor,
    onSuccess: (data) => setEnrollment(data),
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? (err.problem?.detail ?? err.problem?.title ?? err.message)
          : (err as Error).message;
      toast.error("Не удалось начать подключение", { description: detail });
    },
  });

  const verifyMutation = useMutation({
    mutationFn: (otp: string) => verifyEnrollTwoFactor(otp),
    onSuccess: (data) => {
      if (data.success) {
        toast.success("Двухфакторная включена", {
          description: "Дальнейшие входы требуют 6-значный код из приложения-аутентификатора.",
        });
        setEnrollment(null);
        setCode("");
        setQrSvg(null);
        void queryClient.invalidateQueries({ queryKey: ["identity", "profile"] });
      } else {
        toast.error("Проверка не прошла", { description: "Код не совпал. Попробуйте ещё раз." });
      }
    },
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? (err.problem?.detail ?? err.problem?.title ?? err.message)
          : (err as Error).message;
      toast.error("Проверка не прошла", { description: detail });
    },
  });

  // Render the QR as inline SVG when the otpauth URI changes — keeps the
  // image source-of-truth in JS without an extra <canvas>.
  useEffect(() => {
    if (!enrollment) {
      setQrSvg(null);
      return;
    }
    let cancelled = false;
    // Lazy-load the ~50 KB qrcode lib only when 2FA enrollment actually starts.
    import("qrcode")
      .then(({ default: QRCode }) =>
        QRCode.toString(enrollment.authenticatorUri, {
          type: "svg",
          margin: 1,
          width: 200,
          errorCorrectionLevel: "M",
        }),
      )
      .then((svg) => {
        if (cancelled) return;
        // Strip the default white background so the QR adapts to dark mode.
        const themed = svg
          .replace(/fill="#ffffff"/gi, 'fill="transparent"')
          .replace(/fill="#FFFFFF"/gi, 'fill="transparent"')
          .replace(/fill="#000000"/gi, 'fill="currentColor"')
          .replace(/fill="#000"/gi, 'fill="currentColor"');
        setQrSvg(themed);
      })
      .catch(() => setQrSvg(null));
    return () => {
      cancelled = true;
    };
  }, [enrollment]);

  const copyKey = async () => {
    if (!enrollment) return;
    try {
      await navigator.clipboard.writeText(enrollment.sharedKey);
      setCopiedKey(true);
      window.setTimeout(() => setCopiedKey(false), 1500);
    } catch {
      /* clipboard unavailable — silently noop */
    }
  };

  return (
    <SettingsSection
      title="Двухфакторная аутентификация"
      icon={ShieldCheck}
      description={
        <span className="flex flex-wrap items-center gap-2">
          Добавляет код из приложения-аутентификатора поверх пароля. Рекомендуется для каждого
          операторского аккаунта.
          <Badge variant="outline" className="font-mono uppercase tracking-[0.14em]">
            выкл
          </Badge>
        </span>
      }
    >
      {!enrollment ? (
        <div className="flex flex-wrap items-center gap-3">
          <Button
            onClick={() => beginMutation.mutate()}
            disabled={beginMutation.isPending}
          >
            <ShieldCheck className="mr-1.5 h-3.5 w-3.5" />
            {beginMutation.isPending ? "Генерация…" : "Включить двухфакторную"}
          </Button>
          <span className="text-xs text-[var(--color-muted-foreground)]">
            Отсканируйте QR-код в приложении-аутентификаторе (1Password, Google Authenticator, Authy…).
          </span>
        </div>
      ) : (
        <div className="space-y-5">
          <div className="grid gap-5 sm:grid-cols-[14rem_1fr] sm:items-start">
            <div className="grid h-52 w-52 place-items-center rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] p-2 text-[var(--color-foreground)]">
              {qrSvg ? (
                <div
                  aria-label="QR-код двухфакторной"
                  role="img"
                  className="h-full w-full [&_svg]:h-full [&_svg]:w-full"
                  // qrSvg is locally generated by the qrcode lib from the
                  // authenticator URI (not server/user HTML) — safe to inline.
                  dangerouslySetInnerHTML={{ __html: qrSvg }}
                />
              ) : (
                <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  Отрисовка…
                </span>
              )}
            </div>
            <div className="space-y-3">
              <div>
                <div className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  Не сканируется? Введите вручную
                </div>
                <div className="mt-1 flex items-center gap-2">
                  <code className="break-all rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-2 py-1 font-mono text-[11px]">
                    {enrollment.sharedKey}
                  </code>
                  <button
                    type="button"
                    onClick={copyKey}
                    className="inline-flex h-7 items-center gap-1 rounded-md px-2 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
                  >
                    {copiedKey ? (
                      <>
                        <ClipboardCheck className="h-3 w-3" /> скопировано
                      </>
                    ) : (
                      <>
                        <Copy className="h-3 w-3" /> копировать
                      </>
                    )}
                  </button>
                </div>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="totp-code">6-значный код из приложения</Label>
                <Input
                  id="totp-code"
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  placeholder="123 456"
                  maxLength={8}
                  value={code}
                  onChange={(e) => setCode(e.target.value.replace(/\s/g, ""))}
                  className={cn(
                    "font-mono text-lg tracking-[0.4em]",
                    code.length >= 6 && "border-[var(--color-accent-signal)]",
                  )}
                />
              </div>

              <div className="flex flex-wrap items-center gap-2 pt-1">
                <Button
                  onClick={() => verifyMutation.mutate(code)}
                  disabled={code.length < 6 || verifyMutation.isPending}
                  variant="signal"
                >
                  {verifyMutation.isPending ? "Проверка…" : "Подтвердить и включить"}
                </Button>
                <Button
                  variant="ghost"
                  onClick={() => {
                    setEnrollment(null);
                    setCode("");
                  }}
                  disabled={verifyMutation.isPending}
                >
                  Отмена
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </SettingsSection>
  );
}

function TwoFactorDisable() {
  const queryClient = useQueryClient();
  const [password, setPassword] = useState("");

  const mutation = useMutation({
    mutationFn: (pw: string) => disableTwoFactor(pw),
    onSuccess: (data) => {
      if (data.success) {
        toast.success("Двухфакторная отключена");
        setPassword("");
        void queryClient.invalidateQueries({ queryKey: ["identity", "profile"] });
      } else {
        toast.error("Не удалось отключить", { description: "Пароль не подтверждён." });
      }
    },
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? (err.problem?.detail ?? err.problem?.title ?? err.message)
          : (err as Error).message;
      toast.error("Не удалось отключить", { description: detail });
    },
  });

  return (
    <SettingsSection
      title="Двухфакторная аутентификация"
      icon={ShieldOff}
      description={
        <span className="flex flex-wrap items-center gap-2">
          Двухфакторная сейчас включена. Подтвердите пароль, чтобы отключить — секрет аутентификатора
          сменится, при повторном подключении будет новый QR.
          <Badge variant="success" className="font-mono uppercase tracking-[0.14em]">
            вкл
          </Badge>
        </span>
      }
      footer={
        <div className="flex items-center justify-end gap-2">
          <Button
            type="button"
            variant="destructive"
            onClick={() => mutation.mutate(password)}
            disabled={password.length === 0 || mutation.isPending}
          >
            <ShieldOff className="mr-1 h-3.5 w-3.5" />
            {mutation.isPending ? "Отключение…" : "Отключить двухфакторную"}
          </Button>
        </div>
      }
    >
      <div className="grid gap-3 sm:grid-cols-[1fr_auto] sm:items-end">
        <div className="space-y-1.5">
          <Label htmlFor="disable-pw">Текущий пароль</Label>
          <Input
            id="disable-pw"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="font-mono"
          />
        </div>
        {/* Inline action for the sm breakpoint — the footer handles the
            final CTA at all widths but this grid-col-auto slot keeps
            things tidy on desktop. */}
        <div className="hidden sm:block">
          <Button
            type="button"
            variant="destructive"
            onClick={() => mutation.mutate(password)}
            disabled={password.length === 0 || mutation.isPending}
          >
            <ShieldOff className="mr-1 h-3.5 w-3.5" />
            {mutation.isPending ? "Отключение…" : "Отключить"}
          </Button>
        </div>
      </div>

      {mutation.isError && (
        <div
          role="alert"
          className="mt-3 flex items-start gap-2 rounded-md border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.40)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
          <span>
            {mutation.error instanceof ApiRequestError
              ? (mutation.error.problem?.detail ?? mutation.error.message)
              : (mutation.error as Error).message}
          </span>
        </div>
      )}
    </SettingsSection>
  );
}
