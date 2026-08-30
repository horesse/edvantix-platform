import type { Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { paged } from "../helpers/shell-mocks";

// Stable ids reused across the Scheduling specs.
export const GROUP_ID = "a0000000-0000-0000-0000-000000000001";
export const SESSION_ID = "50000000-0000-0000-0000-000000000001";
export const OTHER_SESSION_ID = "50000000-0000-0000-0000-0000000000ff";
export const TEMPLATE_ID = "60000000-0000-0000-0000-000000000001";
export const TEACHER_ID = "70000000-0000-0000-0000-000000000001";
export const ROOM_ID = "80000000-0000-0000-0000-000000000001";
export const STU_1 = "90000000-0000-0000-0000-000000000001";
export const STU_2 = "90000000-0000-0000-0000-000000000002";
export const STU_3 = "90000000-0000-0000-0000-000000000003";

export const PERMS = {
  sessionsView: "Permissions.Scheduling.Sessions.View",
  sessionsViewOwn: "Permissions.Scheduling.Sessions.ViewOwn",
  sessionsUpdate: "Permissions.Scheduling.Sessions.Update",
  sessionsCancel: "Permissions.Scheduling.Sessions.Cancel",
  sessionsReschedule: "Permissions.Scheduling.Sessions.Reschedule",
  sessionsGenerate: "Permissions.Scheduling.Sessions.Generate",
  attendanceView: "Permissions.Scheduling.Attendance.View",
  attendanceMark: "Permissions.Scheduling.Attendance.Mark",
  roomsView: "Permissions.Scheduling.Rooms.View",
  roomsManage: "Permissions.Scheduling.Rooms.Manage",
  templatesView: "Permissions.Scheduling.ScheduleTemplates.View",
  templatesManage: "Permissions.Scheduling.ScheduleTemplates.Manage",
} as const;

export function group(over: Record<string, unknown> = {}) {
  return {
    id: GROUP_ID,
    code: "ENG-A1",
    name: "Английский A1",
    courseId: "c1",
    primaryTeacherId: TEACHER_ID,
    format: "Offline",
    capacity: 8,
    activeEnrollmentCount: 2,
    startDate: "2026-02-01",
    endDate: null,
    status: "Active",
    chatChannelId: null,
    meetingUrl: null,
    roomId: null,
    notes: null,
    createdAtUtc: "2026-01-01T00:00:00Z",
    ...over,
  };
}

export function groupDetail(over: Record<string, unknown> = {}) {
  return {
    ...group(),
    enrollments: [
      {
        id: "e1",
        studyGroupId: GROUP_ID,
        studentId: STU_1,
        enrolledOn: "2026-02-01",
        leftOn: null,
        status: "Active",
        leaveReason: null,
        tariffId: null,
        discountPercent: 0,
      },
      {
        id: "e2",
        studyGroupId: GROUP_ID,
        studentId: STU_2,
        enrolledOn: "2026-02-01",
        leftOn: null,
        status: "Active",
        leaveReason: null,
        tariffId: null,
        discountPercent: 0,
      },
    ],
    teachers: [],
    ...over,
  };
}

export function sessionDetail(over: Record<string, unknown> = {}) {
  return {
    id: SESSION_ID,
    studyGroupId: GROUP_ID,
    lessonId: null,
    teacherId: TEACHER_ID,
    roomId: ROOM_ID,
    startUtc: "2026-09-07T15:00:00Z",
    endUtc: "2026-09-07T16:30:00Z",
    status: "Planned",
    resolvedTopic: "Present Simple",
    meetingUrl: null,
    cancelReason: null,
    rescheduledFromId: null,
    scheduleTemplateId: null,
    teacherComment: null,
    attendance: [],
    ...over,
  };
}

export function attendanceRow(
  studentId: string,
  status = "Present",
  over: Record<string, unknown> = {},
) {
  return {
    id: `att-${studentId}`,
    sessionId: SESSION_ID,
    studentId,
    status,
    comment: null,
    markedByUserId: "u-test-1",
    markedAtUtc: "2026-09-07T16:35:00Z",
    ...over,
  };
}

export function template(over: Record<string, unknown> = {}) {
  return {
    id: TEMPLATE_ID,
    studyGroupId: GROUP_ID,
    dayOfWeek: "Monday",
    startTime: "18:00:00",
    durationMinutes: 90,
    roomId: ROOM_ID,
    teacherId: null,
    validFrom: "2026-09-01",
    validTo: null,
    isActive: true,
    ...over,
  };
}

export function student(id: string, name: string) {
  return {
    id,
    lastName: name.split(" ")[0] ?? name,
    firstName: name.split(" ")[1] ?? "",
    middleName: null,
    displayName: name,
    birthDate: "2010-01-01",
    phone: "",
    email: `${id}@acme.com`,
    userId: null,
    status: "Active",
    source: null,
    avatarFileId: null,
    managerUserId: "u-test-1",
    enrolledAtUtc: "2026-01-01T00:00:00Z",
  };
}

export function teacher(id: string, name: string) {
  return {
    id,
    lastName: name,
    firstName: "",
    middleName: null,
    displayName: name,
    phone: "",
    email: `${id}@acme.com`,
    userId: null,
    status: "Active",
    bio: null,
    specializations: [],
    hourlyRate: null,
    avatarFileId: null,
  };
}

export function room(id: string, name: string, isVirtual = false) {
  return {
    id,
    name,
    capacity: 12,
    location: null,
    isVirtual,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
  };
}

/** Common reference-data mocks every Scheduling screen pulls (timezone,
 *  groups, teachers, rooms, students). UTC timezone keeps wall-clock ==
 *  UTC so time assertions stay simple. */
export async function mockSchedulingRefs(page: Page) {
  await mockJsonResponse(page, "**/api/v1/tenants/settings", {
    timeZoneId: "UTC",
    currency: "USD",
  });
  await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([group()]));
  await mockJsonResponse(page, "**/api/v1/teachers?**", paged([teacher(TEACHER_ID, "Мария Пе")]));
  await mockJsonResponse(page, "**/api/v1/rooms", [room(ROOM_ID, "Кабинет 1")]);
  await mockJsonResponse(
    page,
    "**/api/v1/students?**",
    paged([student(STU_1, "Иванов Пётр"), student(STU_2, "Петров Иван"), student(STU_3, "Сидорова Аня")]),
  );
}
