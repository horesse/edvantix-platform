import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";

test.describe("admin forgot-password", () => {
  test("renders the reset-password request form", async ({ page }) => {
    await page.goto("/forgot-password");

    await expect(page.getByRole("heading", { name: /сброс.*пароля/i })).toBeVisible();
    await expect(page.getByText(/отправим одноразовую ссылку/i)).toBeVisible();
    await expect(page.getByLabel("E-mail")).toBeVisible();
    await expect(page.getByLabel("Школа")).toBeVisible();
    await expect(page.getByRole("button", { name: /отправить ссылку/i })).toBeVisible();
  });

  test("posts email + tenant header to /forgot-password", async ({ page }) => {
    await page.goto("/forgot-password");
    await page.getByLabel("E-mail").fill("operator@root.example");
    await page.getByLabel("Школа").fill("root");

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/identity/forgot-password") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await mockJsonResponse(page, "**/api/v1/identity/forgot-password", '""');
    await page.getByRole("button", { name: /отправить ссылку/i }).click();
    const req = await reqPromise;

    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      email: "operator@root.example",
    });
    expect(req.headers().tenant).toBe("root");
  });

  test("shows the 'check your inbox' success state after 200", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/forgot-password", '""');

    await page.goto("/forgot-password");
    await page.getByLabel("E-mail").fill("alice@acme.com");
    await page.getByLabel("Школа").fill("acme");
    await page.getByRole("button", { name: /отправить ссылку/i }).click();

    await expect(page.getByRole("heading", { name: /проверьте.*почту/i })).toBeVisible();
    await expect(page.getByText("alice@acme.com", { exact: true })).toBeVisible();
    await expect(page.getByText("acme", { exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: /другой адрес/i })).toBeVisible();
  });

  test("surfaces server errors inline", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/forgot-password", 500, {
      title: "Tenant resolution failed",
      detail: "No tenant matching the supplied identifier.",
    });

    await page.goto("/forgot-password");
    await page.getByLabel("E-mail").fill("a@b.com");
    await page.getByLabel("Школа").fill("does-not-exist");
    await page.getByRole("button", { name: /отправить ссылку/i }).click();

    await expect(page.getByRole("alert")).toContainText(/no tenant matching/i);
    await expect(page.getByRole("heading", { name: /проверьте.*почту/i })).not.toBeVisible();
  });
});
