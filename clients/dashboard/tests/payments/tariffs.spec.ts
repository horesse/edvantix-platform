import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import { PERMS, packageTariff, tariff } from "./fixtures";

test.describe("/payments/tariffs — тарифы", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.tariffsView,
      PERMS.tariffsManage,
    ]);
    await mockJsonResponse(page, "**/api/v1/courses?**", {
      items: [],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 1,
      hasNext: false,
      hasPrevious: false,
    });
    await mockJsonResponse(page, "**/api/v1/tariffs**", [tariff(), packageTariff()]);
  });

  test("вид тарифа и валюта — только в создании, не в правке", async ({ page }) => {
    await page.goto("/payments/tariffs");
    await expect(page.getByText("Занятие A1").last()).toBeVisible();

    // Create — kind + currency are present.
    await page.getByRole("button", { name: "Новый тариф" }).first().click();
    const create = page.getByRole("dialog");
    await expect(create.getByText("Новый тариф")).toBeVisible();
    await expect(create.getByLabel("Вид тарифа")).toBeVisible();
    await expect(create.getByLabel("Валюта")).toBeVisible();
    await create.getByRole("button", { name: "Отмена" }).click();

    // Edit — kind + currency are read-only text, no editable controls.
    await page.getByRole("button", { name: "Изменить Занятие A1" }).click();
    const edit = page.getByRole("dialog");
    await expect(edit.getByText("Изменить тариф")).toBeVisible();
    await expect(edit.getByText(/Вид:\s*За занятие/)).toBeVisible();
    await expect(edit.getByText(/Валюта:\s*RUB/)).toBeVisible();
    await expect(edit.getByLabel("Вид тарифа")).toHaveCount(0);
    await expect(edit.getByLabel("Валюта")).toHaveCount(0);
  });

  test("поля пакета (кол-во/срок) — только для PerPackage", async ({ page }) => {
    await page.goto("/payments/tariffs");
    await page.getByRole("button", { name: "Новый тариф" }).first().click();
    const dialog = page.getByRole("dialog");

    // Default kind = PerLesson → no package fields.
    await expect(dialog.getByLabel("Занятий в пакете")).toHaveCount(0);
    await expect(dialog.getByLabel("Срок действия, дней")).toHaveCount(0);

    // Switch to PerPackage → package fields appear. The kind trigger's
    // accessible name comes from its <Field> label ("Вид тарифа").
    await dialog.getByLabel("Вид тарифа").click();
    await page.getByRole("menuitemradio", { name: "Пакет занятий" }).click();

    await expect(dialog.getByLabel("Занятий в пакете")).toBeVisible();
    await expect(dialog.getByLabel("Срок действия, дней")).toBeVisible();
  });
});
