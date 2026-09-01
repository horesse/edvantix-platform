import { expect, test } from "@playwright/test";
import { captureRequest, mockProblemDetails } from "../helpers/api-mocks";

// Ссылка вида, что кладёт письмо-приглашение (Identity):
//   {origin}/accept-invite?email=…&token=…&tenant=…
const VALID_LINK =
  "/accept-invite?token=INV-tok-123&email=newbie@acme.com&tenant=acme";

test.describe("/accept-invite — приём приглашения по e-mail", () => {
  test("неполная ссылка (нет token/email/tenant) → показывает восстановление", async ({
    page,
  }) => {
    await page.goto("/accept-invite");
    await expect(
      page.getByRole("heading", { name: /ссылка неполная/i }),
    ).toBeVisible();
    await expect(page.getByRole("link", { name: /на страницу входа/i })).toBeVisible();
  });

  test("валидная ссылка → форма установки пароля с email/школой из query", async ({
    page,
  }) => {
    await page.goto(VALID_LINK);
    await expect(page.getByText(/Придумайте пароль для входа/)).toBeVisible();
    await expect(page.getByText("newbie@acme.com")).toBeVisible();
    await expect(page.getByLabel("Новый пароль")).toBeVisible();
    await expect(page.getByLabel("Повторите пароль")).toBeVisible();
  });

  test("сабмит → POST /identity/reset-password с полями из query и редирект на /login", async ({
    page,
  }) => {
    const captured = captureRequest(page, "**/api/v1/identity/reset-password");

    await page.goto(VALID_LINK);
    await page.getByLabel("Новый пароль").fill("VeryStrong!Passw0rd");
    await page.getByLabel("Повторите пароль").fill("VeryStrong!Passw0rd");
    await page.getByRole("button", { name: /установить пароль/i }).click();

    const { body, headers } = await captured.value();
    expect(body).toMatchObject({
      email: "newbie@acme.com",
      password: "VeryStrong!Passw0rd",
      token: "INV-tok-123",
    });
    expect(headers.tenant).toBe("acme");

    await expect(page).toHaveURL(/\/login$/);
  });

  test("ошибка RFC 9457 (протухший токен) показывается инлайн, страница не меняется", async ({
    page,
  }) => {
    await mockProblemDetails(page, "**/api/v1/identity/reset-password", 400, {
      title: "Invalid token",
      detail: "Срок действия ссылки истёк или она уже использована.",
    });

    await page.goto(VALID_LINK);
    await page.getByLabel("Новый пароль").fill("VeryStrong!Passw0rd");
    await page.getByLabel("Повторите пароль").fill("VeryStrong!Passw0rd");
    await page.getByRole("button", { name: /установить пароль/i }).click();

    const alert = page.getByRole("alert");
    await expect(alert).toBeVisible();
    await expect(alert).toContainText(/истёк|использована/i);
    await expect(page).toHaveURL(/\/accept-invite/);
  });
});
