import { apiFetch } from "@/lib/api-client";

// ─────────────────────────────────────────────────────────────────────────
//  Tenant (school) settings — timezone + currency. Read-only here; the
//  editor lives in a later stage (`/settings/school`). `GET /tenants/settings`
//  is IsBasic, so every authenticated user can read it.
//  Backend: Modules.Multitenancy.Contracts → TenantSettingsDto.
// ─────────────────────────────────────────────────────────────────────────

export type TenantSettingsDto = {
  /** IANA time zone id, e.g. "Europe/Moscow". Defaults to "UTC". */
  timeZoneId: string;
  /** ISO 4217 currency code, e.g. "USD". */
  currency: string;
};

export function getTenantSettings(): Promise<TenantSettingsDto> {
  return apiFetch<TenantSettingsDto>("/api/v1/tenants/settings");
}
