import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

const SUB = {
  id: "wh-1111",
  url: "https://hooks.example.com/fsh",
  events: ["tenant.created", "user.registered"],
  isActive: true,
  createdAtUtc: "2026-05-10T09:00:00Z",
};

const DELIVERY = {
  id: "dlv-1",
  subscriptionId: SUB.id,
  eventType: "tenant.created",
  httpStatusCode: 200,
  success: true,
  attemptCount: 1,
  attemptedAtUtc: "2026-05-20T12:00:00Z",
  errorMessage: null,
};

// GET /api/v1/webhooks/event-types — the catalog powering the create-dialog
// checklist. Includes the school-domain event types added for Curriculum /
// StudyGroups / Scheduling / Payments / People.
const EVENT_CATALOG = [
  { name: "StudentCreatedIntegrationEvent", module: "People", description: "Создан профиль ученика." },
  { name: "StudyGroupCreatedIntegrationEvent", module: "StudyGroups", description: "Создана учебная группа." },
  { name: "SessionScheduledIntegrationEvent", module: "Scheduling", description: "Занятие поставлено в расписание." },
  { name: "CoursePublishedIntegrationEvent", module: "Curriculum", description: "Курс опубликован." },
  { name: "StudentPaymentConfirmedIntegrationEvent", module: "Payments", description: "Оплата по счёту подтверждена." },
];

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("webhooks subscriptions list", () => {
  test("renders the heading and a subscription row from the mock", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/webhooks/subscriptions?*", paged([SUB]));

    await page.goto("/webhooks");

    const main = page.getByRole("main");
    await expect(
      main.getByRole("heading", { name: "Вебхуки", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(main.getByText(SUB.url, { exact: true })).toBeVisible();
    await expect(main.getByText("tenant.created", { exact: true })).toBeVisible();
    // New subscription button present.
    await expect(main.getByRole("button", { name: /новая подписка/i })).toBeVisible();
  });

  test("shows the empty state when there are no subscriptions", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/webhooks/subscriptions?*", paged([]));

    await page.goto("/webhooks");

    const main = page.getByRole("main");
    await expect(
      main.getByText("Подписок вебхуков пока нет.", { exact: true }),
    ).toBeVisible({ timeout: 10_000 });
    await expect(
      main.getByText(
        "Добавьте endpoint и выберите события. Неудачные доставки повторяются автоматически.",
        { exact: true },
      ),
    ).toBeVisible();
  });

  test("the create dialog groups the event catalog by module and lists new school event types", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/webhooks/subscriptions?*", paged([SUB]));
    await mockJsonResponse(page, "**/api/v1/webhooks/event-types", EVENT_CATALOG);

    await page.goto("/webhooks");
    await page.getByRole("button", { name: /новая подписка/i }).click();

    const dialog = page.getByRole("dialog");
    await expect(
      dialog.getByRole("heading", { name: "Новая подписка вебхука" }),
    ).toBeVisible({ timeout: 10_000 });

    // Module legends (Russian labels from MODULE_LABEL).
    await expect(dialog.getByText("Люди", { exact: true })).toBeVisible();
    await expect(dialog.getByText("Учебные группы", { exact: true })).toBeVisible();
    await expect(dialog.getByText("Расписание", { exact: true })).toBeVisible();
    await expect(dialog.getByText("Платежи", { exact: true })).toBeVisible();

    // New school-domain event types from the catalog are selectable.
    await expect(dialog.getByText("StudyGroupCreatedIntegrationEvent", { exact: true })).toBeVisible();
    const paymentRow = dialog.getByText("StudentPaymentConfirmedIntegrationEvent", { exact: true });
    await expect(paymentRow).toBeVisible();

    // Ticking one and creating sends it in the events array.
    await paymentRow.click();
    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/webhooks/subscriptions") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await mockJsonResponse(page, "**/api/v1/webhooks/subscriptions", '"new-sub-id"', { method: "POST" });
    await dialog.getByLabel("URL endpoint").fill("https://hooks.example.com/edvantix");
    await dialog.getByRole("button", { name: /создать подписку/i }).click();
    const req = await reqPromise;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.events).toContain("StudentPaymentConfirmedIntegrationEvent");
  });
});

test.describe("webhook detail (deliveries)", () => {
  test("loads the endpoint sections and a delivery row", async ({ page }) => {
    // Detail finds the sub by listing subscriptions (page 1, big page size).
    await mockJsonResponse(page, "**/api/v1/webhooks/subscriptions?*", paged([SUB], { pageSize: 200 }));
    await mockJsonResponse(
      page,
      `**/api/v1/webhooks/subscriptions/${SUB.id}/deliveries?*`,
      paged([DELIVERY]),
    );

    await page.goto(`/webhooks/${SUB.id}`);

    const main = page.getByRole("main");
    // h1 is the subscription URL.
    await expect(
      main.getByRole("heading", { name: SUB.url, exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // Section titles now render via SettingsSection (h2 with plain titles).
    await expect(
      main.getByRole("heading", { name: "Endpoint", exact: true }),
    ).toBeVisible();
    await expect(
      main.getByRole("heading", { name: "Доставки", exact: true }),
    ).toBeVisible();

    // Delivery row: event type chip + HTTP status badge.
    await expect(main.getByText("tenant.created", { exact: true }).first()).toBeVisible();
    await expect(main.getByText(/HTTP 200/)).toBeVisible();
  });

  test("shows the no-deliveries copy when the subscription has none", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/webhooks/subscriptions?*", paged([SUB], { pageSize: 200 }));
    await mockJsonResponse(
      page,
      `**/api/v1/webhooks/subscriptions/${SUB.id}/deliveries?*`,
      paged([]),
    );

    await page.goto(`/webhooks/${SUB.id}`);

    const main = page.getByRole("main");
    await expect(
      main.getByText(
        "Доставок пока нет. Нажмите «Отправить тестовое событие» выше или дождитесь события.",
        { exact: true },
      ),
    ).toBeVisible({ timeout: 10_000 });
  });
});
