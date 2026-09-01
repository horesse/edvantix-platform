import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";

const VALID_LINK = "/reset-password?token=ABC123&email=admin@root.com&tenant=root";

test.describe("admin reset-password", () => {
  test("guards against malformed links", async ({ page }) => {
    await page.goto("/reset-password");
    await expect(page.getByRole("heading", { name: /ссылка.*неполная/i })).toBeVisible();
    await expect(page.getByRole("link", { name: /запросить новую ссылку/i })).toBeVisible();
  });

  test("renders the form with the email + tenant echoed back", async ({ page }) => {
    await page.goto(VALID_LINK);
    await expect(page.getByRole("heading", { name: /новый.*пароль/i })).toBeVisible();
    await expect(page.getByText("admin@root.com", { exact: true })).toBeVisible();
    await expect(page.getByText("root", { exact: true })).toBeVisible();
    await expect(page.getByLabel("Новый пароль")).toBeVisible();
    await expect(page.getByLabel("Повтор пароля")).toBeVisible();
  });

  test("password strength meter reacts to input", async ({ page }) => {
    await page.goto(VALID_LINK);
    const newPw = page.getByLabel("Новый пароль");

    await newPw.fill("short");
    await expect(page.getByText(/^Слабый$/)).toBeVisible();
    await newPw.fill("Abcdefg1");
    await expect(page.getByText(/^Средний$/)).toBeVisible();
    await newPw.fill("VeryStrong!Passw0rd");
    await expect(page.getByText(/^Надёжный$/)).toBeVisible();
  });

  test("submit is disabled until strong + matching", async ({ page }) => {
    await page.goto(VALID_LINK);
    const submit = page.getByRole("button", { name: /установить пароль/i });
    await expect(submit).toBeDisabled();
    await page.getByLabel("Новый пароль").fill("VeryStrong!Passw0rd");
    await page.getByLabel("Повтор пароля").fill("VeryStrong!Passw0rd");
    await expect(submit).toBeEnabled();
  });

  test("posts to /reset-password and bounces to /login on success", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/reset-password", '""');

    await page.goto(VALID_LINK);
    await page.getByLabel("Новый пароль").fill("VeryStrong!Passw0rd");
    await page.getByLabel("Повтор пароля").fill("VeryStrong!Passw0rd");

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/reset-password") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: /установить пароль/i }).click();
    const req = await reqPromise;

    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      email: "admin@root.com",
      password: "VeryStrong!Passw0rd",
      token: "ABC123",
    });
    expect(req.headers().tenant).toBe("root");
    await expect(page).toHaveURL(/\/login$/);
  });

  test("surfaces a token-expired error inline", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/reset-password", 400, {
      title: "Invalid token",
      detail: "The reset token has expired or already been used.",
    });

    await page.goto(VALID_LINK);
    await page.getByLabel("Новый пароль").fill("VeryStrong!Passw0rd");
    await page.getByLabel("Повтор пароля").fill("VeryStrong!Passw0rd");
    await page.getByRole("button", { name: /установить пароль/i }).click();

    await expect(page.getByRole("alert")).toContainText(/token has expired/i);
    await expect(page).toHaveURL(/\/reset-password/);
  });
});
