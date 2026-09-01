import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Fingerprint, ShieldCheck, UserRound } from "lucide-react";
import { toast } from "sonner";
import { getMyProfile, setProfileImage } from "@/api/users";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { ErrorBand, LoadingRow, SettingsSection, SettingsField } from "@/components/list";
import { ImageInput } from "@/components/file/image-input";
import { ApiRequestError } from "@/lib/api-client";

/**
 * ProfileSettings — read-only view of identity fields (server doesn't expose
 * an /update-me endpoint for these yet) plus avatar upload via the presigned
 * ImageInput flow. Username, email, and name are intentionally not editable
 * from here — they require admin involvement, which is correct for a
 * multi-tenant operator console.
 *
 * Avatar fix: uses ImageInput + presigned upload (durable URL via Files module)
 * instead of the old base64 data: URL approach that hit the 2048-char limit.
 */
export function ProfileSettings() {
  const queryClient = useQueryClient();
  const profile = useQuery({ queryKey: ["identity", "profile"], queryFn: getMyProfile });

  const imageMutation = useMutation({
    mutationFn: (url: string | null) => setProfileImage(url),
    onSuccess: () => {
      toast.success("Фото профиля обновлено");
      void queryClient.invalidateQueries({ queryKey: ["identity", "profile"] });
    },
    onError: (err: unknown) => {
      const message =
        err instanceof ApiRequestError
          ? (err.problem?.detail ?? err.problem?.title ?? err.message)
          : "Не удалось обновить фото профиля";
      toast.error(message);
    },
  });

  if (profile.isLoading) return <LoadingRow label="Загрузка профиля" />;
  if (profile.isError) {
    return (
      <ErrorBand
        message={
          profile.error instanceof ApiRequestError
            ? (profile.error.problem?.detail ?? profile.error.message)
            : "Не удалось загрузить профиль."
        }
      />
    );
  }

  const user = profile.data!;
  const displayName =
    [user.firstName, user.lastName].filter(Boolean).join(" ").trim() ||
    user.userName ||
    user.email ||
    "Аккаунт";

  return (
    <div className="space-y-5 fsh-enter">
      {/* Avatar — presigned upload via ImageInput, no base64 data: URLs */}
      <SettingsSection
        title="Аватар"
        icon={UserRound}
        description="Показывается в шапке и в вашей активности. Лучше квадратное изображение — JPG, PNG или WebP."
      >
        <ImageInput
          value={user.imageUrl ?? ""}
          onChange={(next) => imageMutation.mutate(next.length > 0 ? next : null)}
          ownerType="User"
          ownerId={user.id ?? null}
          shape="circle"
        />
      </SettingsSection>

      {/* Identity — read-only; admin must update these server-side */}
      <SettingsSection
        title="Идентификация"
        icon={Fingerprint}
        description="Данные вашей учётки. Их меняет администратор — обратитесь к нему при необходимости."
      >
        <div className="grid gap-5 sm:grid-cols-2">
          <SettingsField id="profile-username" label="Логин">
            <Input
              id="profile-username"
              value={user.userName ?? ""}
              readOnly
              className="font-mono bg-[var(--color-muted)] cursor-not-allowed"
            />
          </SettingsField>
          <SettingsField id="profile-display" label="Отображаемое имя">
            <Input
              id="profile-display"
              value={displayName}
              readOnly
              className="bg-[var(--color-muted)] cursor-not-allowed"
            />
          </SettingsField>
          <SettingsField id="profile-email" label="E-mail">
            <Input
              id="profile-email"
              type="email"
              value={user.email ?? ""}
              readOnly
              className="font-mono bg-[var(--color-muted)] cursor-not-allowed"
            />
            {user.emailConfirmed !== undefined && (
              <p className="mt-1 text-[11px] text-[var(--color-muted-foreground)]">
                {user.emailConfirmed ? "Адрес подтверждён" : "Ещё не подтверждён"}
              </p>
            )}
          </SettingsField>
          <SettingsField id="profile-phone" label="Телефон">
            <Input
              id="profile-phone"
              value={user.phoneNumber ?? "—"}
              readOnly
              className="font-mono bg-[var(--color-muted)] cursor-not-allowed"
            />
          </SettingsField>
        </div>
      </SettingsSection>

      {/* Status badges */}
      <SettingsSection
        title="Состояние аккаунта"
        icon={ShieldCheck}
        description="Флаги этой учётки. Их меняет оператор."
      >
        <div className="flex flex-wrap items-center gap-2">
          <Badge
            variant={user.isActive ? "success" : "muted"}
            className="font-mono uppercase tracking-[0.14em]"
          >
            {user.isActive ? "Активна" : "Отключена"}
          </Badge>
          <Badge
            variant={user.emailConfirmed ? "info" : "warning"}
            className="font-mono uppercase tracking-[0.14em]"
          >
            {user.emailConfirmed ? "E-mail подтверждён" : "E-mail ожидает"}
          </Badge>
          <Badge
            variant={user.twoFactorEnabled ? "success" : "outline"}
            className="font-mono uppercase tracking-[0.14em]"
          >
            {user.twoFactorEnabled ? "2FA включена" : "2FA выкл"}
          </Badge>
        </div>
      </SettingsSection>
    </div>
  );
}

