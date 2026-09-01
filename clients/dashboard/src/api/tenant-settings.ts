import { apiFetch } from "@/lib/api-client";

// ─────────────────────────────────────────────────────────────────────────
//  Tenant (school) settings — time zone + currency.
//  Backend: Modules.Multitenancy → GET/PUT /api/v1/tenants/settings.
//  GET requires Permissions.SchoolSettings.View (IsBasic — every authenticated
//  user can read the school's time zone/currency); PUT requires
//  Permissions.SchoolSettings.Manage (managers only).
//  The command replaces both fields at once (no PATCH), so the editor always
//  sends the full pair.
// ─────────────────────────────────────────────────────────────────────────

export type TenantSettingsDto = {
  /** IANA time zone id, e.g. "Europe/Moscow". Defaults to "UTC". */
  timeZoneId: string;
  /** ISO 4217 currency code, e.g. "USD" (stored upper-case). */
  currency: string;
};

export function getTenantSettings(): Promise<TenantSettingsDto> {
  return apiFetch<TenantSettingsDto>("/api/v1/tenants/settings");
}

export function updateTenantSettings(input: TenantSettingsDto): Promise<void> {
  return apiFetch<void>("/api/v1/tenants/settings", {
    method: "PUT",
    body: JSON.stringify({
      timeZoneId: input.timeZoneId,
      currency: input.currency.toUpperCase(),
    }),
  });
}
