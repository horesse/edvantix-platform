// Admin demo accounts — mirrors src/Host/Edvantix.DbMigrator/DemoSeed/DemoSeeder.cs
// (keep in sync: BuildRootUsers + MultitenancyConstants.Root.EmailAddress).
//
// Static — no API call — because the login page is unauthenticated and the API
// can't safely advertise credentials. Admin is the platform-operator console:
// it only surfaces root-tenant operators. School-role users (school admin,
// manager, teacher, guardian, student) live in tenants like `acme` and sign in
// through the dashboard app — the operator console's routes are root-gated, so
// listing them here would be misleading.

export type DemoAccount = {
  email: string;
  password: string;
  tenant: string;
  /** Short display label */
  label: string;
  /** Initials rendered in the avatar */
  initials: string;
  /** One-line persona explainer */
  persona: string;
};

export const DEMO_PASSWORD = "Password123!";

/**
 * The two root-tenant operators the demo seeder creates. In the Aspire dev
 * stack `seed-demo` realigns both to the shared DEMO_PASSWORD so they work out
 * of the box. `admin@root.com` is `MultitenancyConstants.Root.EmailAddress`
 * (the framework-provisioned admin); `superadmin@root.com` is the extra
 * operator from `DemoSeeder.BuildRootUsers()`.
 */
export const ADMIN_DEMO_ACCOUNTS: DemoAccount[] = [
  {
    email: "admin@root.com",
    password: DEMO_PASSWORD,
    tenant: "root",
    label: "Оператор платформы",
    initials: "ОП",
    persona: "Владелец платформы Edvantix · все школы и подписки",
  },
  {
    email: "superadmin@root.com",
    password: DEMO_PASSWORD,
    tenant: "root",
    label: "Суперадминистратор",
    initials: "СА",
    persona: "Кросс-тенантный доступ · провижининг и биллинг",
  },
];
