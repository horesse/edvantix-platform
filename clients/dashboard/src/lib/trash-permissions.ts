/**
 * Permission required to view each Recycle bin tab. Each value mirrors the
 * permission its matching trash endpoint enforces server-side (Curriculum's
 * course trash requires `Courses.Restore`; the tickets trash requires
 * `Tickets.Restore`; the files trash requires `Files.ViewTrash`). Mirrored here
 * so the dashboard can hide tabs — and the Trash nav entry itself — that a user
 * can't access, instead of letting them click into a guaranteed 403. The server
 * keeps enforcing as defence-in-depth.
 *
 * Convention follows the server registries: `Permissions.{Resource}.{Action}`.
 * If a trash endpoint's permission changes, mirror it here.
 *
 * Only school entities that expose BOTH a `/trash` list AND a `/restore` action
 * belong here. Students can be restored (`POST /students/{id}/restore`) but have
 * no trash list — archived students surface through the status filter on
 * `/students`, so they're handled there, not in the recycle bin.
 */
export const TRASH_TAB_PERMISSIONS = {
  courses: "Permissions.Curriculum.Courses.Restore",
  tickets: "Permissions.Tickets.Restore",
  files: "Permissions.Files.ViewTrash",
} as const;

export type TrashTabKey = keyof typeof TRASH_TAB_PERMISSIONS;

/** Flat list of every trash permission — used to gate the Trash nav entry
 *  (visible if the user holds *any* of them). */
export const ALL_TRASH_PERMISSIONS: readonly string[] = Object.values(TRASH_TAB_PERMISSIONS);
