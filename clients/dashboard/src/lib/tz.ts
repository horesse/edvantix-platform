// ─────────────────────────────────────────────────────────────────────────
//  Timezone helpers for the schedule calendar.
//
//  The Scheduling API returns every instant in UTC (`startUtc`/`endUtc`).
//  The calendar must *display* those in the school's timezone (a tenant
//  setting) regardless of where the viewer's browser is. We avoid pulling
//  in a FullCalendar timezone plugin by running the calendar in
//  `timeZone: "UTC"` and feeding it "wall-clock" strings we compute here:
//  the local time in the school tz, serialised without an offset. Drag-drop
//  hands those wall-clock values back, which `wallClockToUtc` turns into a
//  real UTC instant for the reschedule call.
// ─────────────────────────────────────────────────────────────────────────

type Parts = {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
  second: number;
};

function partsInZone(date: Date, timeZoneId: string): Parts {
  const fmt = new Intl.DateTimeFormat("en-US", {
    timeZone: timeZoneId,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hourCycle: "h23",
  });
  const map: Record<string, string> = {};
  for (const p of fmt.formatToParts(date)) {
    if (p.type !== "literal") map[p.type] = p.value;
  }
  return {
    year: Number(map.year),
    month: Number(map.month),
    day: Number(map.day),
    hour: Number(map.hour === "24" ? "00" : map.hour),
    minute: Number(map.minute),
    second: Number(map.second),
  };
}

function pad(n: number, width = 2): string {
  return String(n).padStart(width, "0");
}

/** UTC ISO instant → the school-tz wall-clock, serialised WITHOUT an offset
 *  (e.g. `"2026-09-01T18:00:00"`). Feed this to FullCalendar with
 *  `timeZone: "UTC"` so it renders the school-local time verbatim. */
export function utcIsoToZonedWallClock(utcIso: string, timeZoneId: string): string {
  const p = partsInZone(new Date(utcIso), timeZoneId);
  return `${pad(p.year, 4)}-${pad(p.month)}-${pad(p.day)}T${pad(p.hour)}:${pad(
    p.minute,
  )}:${pad(p.second)}`;
}

/** Inverse of {@link utcIsoToZonedWallClock}: a wall-clock in `timeZoneId`
 *  (as a `Date` whose UTC fields hold the wall-clock, which is exactly what
 *  FullCalendar hands back in `timeZone: "UTC"` mode) → the real UTC instant.
 *  Two correction passes make it robust across DST boundaries. */
export function wallClockToUtc(wallClock: Date, timeZoneId: string): Date {
  const target = wallClock.getTime();
  let guess = target;
  for (let i = 0; i < 2; i += 1) {
    const p = partsInZone(new Date(guess), timeZoneId);
    const asUtc = Date.UTC(p.year, p.month - 1, p.day, p.hour, p.minute, p.second);
    const diff = target - asUtc;
    if (diff === 0) break;
    guess += diff;
  }
  return new Date(guess);
}

/** School-tz wall-clock string (no offset) → real UTC ISO instant. */
export function zonedWallClockToUtcIso(wallClock: string, timeZoneId: string): string {
  const m = wallClock.match(
    /^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})(?::(\d{2}))?/,
  );
  if (!m) return new Date(wallClock).toISOString();
  const asIfUtc = new Date(
    Date.UTC(
      Number(m[1]),
      Number(m[2]) - 1,
      Number(m[3]),
      Number(m[4]),
      Number(m[5]),
      Number(m[6] ?? "0"),
    ),
  );
  return wallClockToUtc(asIfUtc, timeZoneId).toISOString();
}

/** Format a UTC ISO instant as `"1 сен, 18:00"` in the school timezone. */
export function formatZonedDateTime(utcIso: string, timeZoneId: string): string {
  return new Intl.DateTimeFormat("ru-RU", {
    timeZone: timeZoneId,
    day: "numeric",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).format(new Date(utcIso));
}

/** Format just the time part, `"18:00"`, in the school timezone. */
export function formatZonedTime(utcIso: string, timeZoneId: string): string {
  return new Intl.DateTimeFormat("ru-RU", {
    timeZone: timeZoneId,
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).format(new Date(utcIso));
}
