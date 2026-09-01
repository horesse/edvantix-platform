import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";

// SecuritySettings = password change + 2FA. The 2FA branch is driven by the
// profile's twoFactorEnabled flag (GET /api/v1/identity/profile). Password
// change POSTs to /api/v1/identity/users/change-password with
// { password, newPassword, confirmNewPassword }.

const PROFILE_2FA_OFF = {
  id: "u-test-1",
  userName: "rootadmin",
  email: "admin@root.com",
  firstName: "Root",
  lastName: "Admin",
  isActive: true,
  emailConfirmed: true,
  twoFactorEnabled: false,
  imageUrl: null,
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("settings · security", () => {
  test("renders the change-password form and the 2FA-off state", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE_2FA_OFF);

    await page.goto("/settings/security");

    const main = page.getByRole("main");
    // The Password section now renders as a SettingsSection <h2> heading; the
    // actual form lives behind a "Change password" button (Dialog pattern).
    await expect(main.getByRole("heading", { name: "Пароль" })).toBeVisible({ timeout: 10_000 });

    // 2FA disabled → enroll affordance + "выкл" badge.
    await expect(main.getByRole("button", { name: /включить двухфакторную/i })).toBeVisible();
    await expect(main.getByText("выкл", { exact: true })).toBeVisible();

    // Open the change-password dialog; its form portals OUTSIDE <main>.
    await main.getByRole("button", { name: /сменить пароль/i }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByLabel(/^Текущий пароль/)).toBeVisible();
    await expect(dialog.getByLabel(/^Новый пароль/)).toBeVisible();
    await expect(dialog.getByLabel(/^Повтор нового пароля/)).toBeVisible();
  });

  test("POSTs the change-password payload on submit", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE_2FA_OFF);
    await mockJsonResponse(
      page,
      "**/api/v1/identity/users/change-password",
      '"Password changed."',
      { method: "POST" },
    );

    await page.goto("/settings/security");

    const main = page.getByRole("main");
    await main
      .getByRole("button", { name: /сменить пароль/i })
      .click({ timeout: 10_000 });

    // The form is inside the portaled dialog.
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel(/^Текущий пароль/).fill("OldPass123!");
    await dialog.getByLabel(/^Новый пароль/).fill("NewPass456!");
    await dialog.getByLabel(/^Повтор нового пароля/).fill("NewPass456!");

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/users/change-password") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: /обновить пароль/i }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.password).toBe("OldPass123!");
    expect(body.newPassword).toBe("NewPass456!");
    expect(body.confirmNewPassword).toBe("NewPass456!");

    await expect(page.getByText(/пароль изменён/i)).toBeVisible();
  });

  test("shows a validation error and does NOT POST when passwords mismatch", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE_2FA_OFF);

    await page.goto("/settings/security");

    const main = page.getByRole("main");
    await main
      .getByRole("button", { name: /сменить пароль/i })
      .click({ timeout: 10_000 });

    const dialog = page.getByRole("dialog");
    await dialog.getByLabel(/^Текущий пароль/).fill("OldPass123!");
    await dialog.getByLabel(/^Новый пароль/).fill("NewPass456!");
    await dialog.getByLabel(/^Повтор нового пароля/).fill("Different789!");

    let posted = false;
    page.on("request", (r) => {
      if (r.url().includes("/change-password") && r.method() === "POST") posted = true;
    });

    await dialog.getByRole("button", { name: /обновить пароль/i }).click();
    // Zod refine surfaces the mismatch on the confirm Field's error line.
    await expect(dialog.getByText(/пароли не совпадают/i)).toBeVisible();
    expect(posted).toBe(false);
  });

  test("renders the 2FA-enabled disable controls when twoFactorEnabled is true", async ({ page }) => {
    // Override the shell profile to flip the 2FA flag on.
    await mockJsonResponse(page, "**/api/v1/identity/profile", {
      ...PROFILE_2FA_OFF,
      twoFactorEnabled: true,
    });

    await page.goto("/settings/security");

    const main = page.getByRole("main");
    await expect(main.getByRole("button", { name: /отключить двухфакторную/i })).toBeVisible({
      timeout: 10_000,
    });
    await expect(main.getByText("вкл", { exact: true })).toBeVisible();
  });

  test("surfaces a server error toast when change-password is rejected", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/profile", PROFILE_2FA_OFF);
    await mockProblemDetails(
      page,
      "**/api/v1/identity/users/change-password",
      400,
      { title: "Bad Request", detail: "Current password is incorrect." },
    );

    await page.goto("/settings/security");

    const main = page.getByRole("main");
    await main
      .getByRole("button", { name: /сменить пароль/i })
      .click({ timeout: 10_000 });

    const dialog = page.getByRole("dialog");
    await dialog.getByLabel(/^Текущий пароль/).fill("WrongPass1!");
    await dialog.getByLabel(/^Новый пароль/).fill("NewPass456!");
    await dialog.getByLabel(/^Повтор нового пароля/).fill("NewPass456!");
    await dialog.getByRole("button", { name: /обновить пароль/i }).click();

    // The mutation's onError raises a sonner toast whose description carries
    // the ProblemDetails detail; assert on that copy.
    await expect(page.getByText(/current password is incorrect/i)).toBeVisible();
  });
});
