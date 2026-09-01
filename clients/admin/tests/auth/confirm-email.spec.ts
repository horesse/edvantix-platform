import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";

const VALID_LINK = "/confirm-email?userId=u-1&code=verify-code&tenant=root";

test.describe("admin confirm-email", () => {
  test("malformed-link state when params are missing", async ({ page }) => {
    await page.goto("/confirm-email");
    await expect(page.getByText(/не хватает обязательных параметров/i)).toBeVisible();
    await expect(page.getByRole("heading", { name: /не удалось.*подтвердить.*e-mail/i })).toBeVisible();
  });

  test("success state on 2xx with continue-to-signin CTA", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/identity/confirm-email**",
      '"Адрес e-mail подтверждён и готов к работе."',
    );
    await page.goto(VALID_LINK);

    await expect(page.getByText("Адрес e-mail подтверждён и готов к работе.", { exact: true })).toBeVisible();
    const cta = page.getByRole("link", { name: /перейти ко входу/i });
    await expect(cta).toBeVisible();
    await cta.click();
    await expect(page).toHaveURL(/\/login$/);
  });

  test("failure state with recovery affordances", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/identity/confirm-email**", 400, {
      title: "Invalid token",
      detail: "The confirmation token is no longer valid.",
    });
    await page.goto(VALID_LINK);

    await expect(page.getByText(/no longer valid/i)).toBeVisible();
    await expect(page.getByRole("link", { name: "К входу", exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: /сбросить пароль/i })).toBeVisible();
  });
});
