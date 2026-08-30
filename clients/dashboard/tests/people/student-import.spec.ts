import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

const DRY_RUN_RESULT = {
  dryRun: true,
  totalRows: 2,
  successCount: 1,
  errorCount: 1,
  rows: [
    { rowNumber: 1, success: true, studentId: null, error: null },
    { rowNumber: 2, success: false, studentId: null, error: "Некорректная дата рождения" },
  ],
};

const CSV =
  "LastName,FirstName,MiddleName,BirthDate,Phone,Email,ManagerUserId,Source\n" +
  "Иванов,Пётр,,2010-05-01,+7900,petya@acme.com,u-test-1,\n" +
  "Петров,Иван,,bad-date,+7901,ivan@acme.com,u-test-1,\n";

test.describe("people/students/import", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      "Permissions.People.Students.View",
      "Permissions.People.Students.Create",
    ]);
  });

  test("dry-run preview then commit", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/students/import?dryRun=true", DRY_RUN_RESULT);
    await mockJsonResponse(page, "**/api/v1/students/import?dryRun=false", {
      ...DRY_RUN_RESULT,
      dryRun: false,
    });
    await mockJsonResponse(page, "**/api/v1/students?**", {
      items: [],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 1,
      hasPrevious: false,
      hasNext: false,
    });

    await page.goto("/students/import");

    await page.locator('input[type="file"]').setInputFiles({
      name: "students.csv",
      mimeType: "text/csv",
      buffer: Buffer.from(CSV, "utf-8"),
    });

    await page.getByRole("button", { name: /Предпросмотр/ }).click();

    // Per-row table with the failing row's message.
    await expect(page.getByRole("heading", { name: /Результат предпросмотра/ })).toBeVisible();
    await expect(page.getByText("Некорректная дата рождения")).toBeVisible();

    const commit = page.waitForRequest(
      (r) => r.url().includes("dryRun=false") && r.method() === "POST",
    );
    await page.getByRole("button", { name: /Импортировать/ }).click();
    await commit;

    // Redirects back to the students list on success.
    await expect(page).toHaveURL(/\/students$/);
  });
});
