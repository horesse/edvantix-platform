import { expect, test } from "@playwright/test";
import { captureRequest, mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const TEACHERS = [
  {
    id: "00000000-0000-0000-0000-0000000000t1",
    lastName: "Смирнов",
    firstName: "Олег",
    middleName: null,
    displayName: "Смирнов Олег",
    phone: "+7 900 111-00-11",
    email: "oleg@acme.com",
    userId: null,
    status: "Active",
    bio: null,
    specializations: ["Математика", "Физика"],
    hourlyRate: 1500,
    avatarFileId: null,
  },
];

const ALL_PERMS = [
  "Permissions.People.Teachers.View",
  "Permissions.People.Teachers.Create",
  "Permissions.People.Teachers.Update",
  "Permissions.People.Teachers.Delete",
  "Permissions.Scheduling.Sessions.View",
];

const WORKLOAD = {
  teacherId: TEACHERS[0].id,
  from: "2026-09-03",
  to: "2026-09-10",
  activeGroupsCount: 3,
  sessionsCount: 12,
  totalHours: 18,
};

const TEACHER_GROUPS = [
  {
    id: "00000000-0000-0000-0000-0000000000g1",
    code: "MATH-A",
    name: "Математика — группа A",
    courseId: "00000000-0000-0000-0000-0000000000c1",
    primaryTeacherId: TEACHERS[0].id,
    format: "Offline",
    capacity: 10,
    activeEnrollmentCount: 7,
    startDate: "2026-09-01",
    endDate: null,
    status: "Active",
    chatChannelId: null,
    meetingUrl: null,
    roomId: null,
    notes: null,
    createdAtUtc: "2026-08-01T00:00:00Z",
  },
];

test.describe("people/teachers", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", ALL_PERMS);
    // Teacher-detail side panels — harmless defaults; specs override after.
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([]));
    await mockJsonResponse(page, "**/api/v1/teachers/*/workload**", WORKLOAD);
  });

  test("list renders a teacher row with specializations", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/teachers?**", paged(TEACHERS));
    await page.goto("/teachers");

    await expect(page.getByRole("heading", { name: "Преподаватели", level: 1 })).toBeVisible();
    await expect(page.getByText("Смирнов Олег").last()).toBeVisible();
    await expect(page.getByText("Математика, Физика").last()).toBeVisible();
  });

  test("create dialog posts specializations split on comma", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/teachers?**", paged(TEACHERS));
    const sent = captureRequest(page, "**/api/v1/teachers");
    await page.goto("/teachers");

    await page.getByRole("button", { name: /Новый преподаватель/ }).first().click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Фамилия").fill("Кузнецов");
    await dialog.getByLabel("Имя").fill("Дмитрий");
    await dialog.getByLabel("Телефон").fill("+7 900 222-33-44");
    await dialog.getByLabel("E-mail").fill("dk@acme.com");
    await dialog.getByLabel("Специализации").fill("Химия, Биология");
    await dialog.getByRole("button", { name: /Создать/ }).click();

    const body = (await sent.value()).body as Record<string, unknown>;
    expect(body).toMatchObject({
      lastName: "Кузнецов",
      firstName: "Дмитрий",
      specializations: ["Химия", "Биология"],
    });
  });

  test("detail: deactivate posts to the deactivate endpoint", async ({ page }) => {
    const id = TEACHERS[0].id;
    const deactivate = captureRequest(page, `**/api/v1/teachers/${id}/deactivate`);
    await mockJsonResponse(page, `**/api/v1/teachers/${id}`, TEACHERS[0]);
    await page.goto(`/teachers/${id}`);

    await expect(page.getByRole("heading", { name: "Смирнов Олег", level: 1 })).toBeVisible();
    await page.getByRole("button", { name: "Деактивировать" }).click();
    await deactivate.value();
  });

  test("detail: workload section shows numbers and groups link to the group", async ({ page }) => {
    const id = TEACHERS[0].id;
    await mockJsonResponse(page, `**/api/v1/teachers/${id}`, TEACHERS[0]);
    await mockJsonResponse(page, `**/api/v1/teachers/${id}/workload**`, WORKLOAD);
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged(TEACHER_GROUPS));
    await page.goto(`/teachers/${id}`);

    const workload = page.locator("section", {
      has: page.getByRole("heading", { name: "Нагрузка" }),
    });
    await expect(workload.getByText("активных групп")).toBeVisible();
    await expect(workload.getByText("занятий за период")).toBeVisible();
    await expect(workload.getByText("часов")).toBeVisible();

    const groups = page.locator("section", {
      has: page.getByRole("heading", { name: "Группы преподавателя" }),
    });
    await expect(
      groups.getByRole("link", { name: /Математика — группа A/ }),
    ).toHaveAttribute("href", `/study-groups/${TEACHER_GROUPS[0].id}`);
    await expect(
      groups.getByRole("link", { name: "Расписание преподавателя" }),
    ).toHaveAttribute("href", `/schedule?teacherId=${id}`);
  });

  test("detail: workload section is gated on Scheduling.Sessions.View", async ({ page }) => {
    const id = TEACHERS[0].id;
    await mockJsonResponse(
      page,
      "**/api/v1/identity/permissions",
      ALL_PERMS.filter((p) => p !== "Permissions.Scheduling.Sessions.View"),
    );
    await mockJsonResponse(page, `**/api/v1/teachers/${id}`, TEACHERS[0]);
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged(TEACHER_GROUPS));
    await page.goto(`/teachers/${id}`);

    await expect(
      page.getByText("Недостаточно прав для просмотра нагрузки преподавателя."),
    ).toBeVisible();
  });
});
