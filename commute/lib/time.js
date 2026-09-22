// Time helpers pinned to Sydney time, independent of the host's timezone.

export const TZ = 'Australia/Sydney';

const partsFormatter = new Intl.DateTimeFormat('en-AU', {
  timeZone: TZ,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
  hourCycle: 'h23',
});

/** Calendar fields of `date` as seen in Sydney. weekday: 0 = Sunday. */
export function sydneyParts(date) {
  const p = Object.fromEntries(partsFormatter.formatToParts(date).map((x) => [x.type, x.value]));
  const year = Number(p.year);
  const month = Number(p.month);
  const day = Number(p.day);
  return {
    year,
    month,
    day,
    hour: Number(p.hour),
    minute: Number(p.minute),
    second: Number(p.second),
    weekday: new Date(Date.UTC(year, month - 1, day)).getUTCDay(),
  };
}

/** Converts a Sydney wall-clock time to an absolute Date (handles AEST/AEDT). */
export function sydneyLocalToDate(year, month, day, hour, minute) {
  const target = Date.UTC(year, month - 1, day, hour, minute);
  let guess = target;
  for (let i = 0; i < 3; i++) {
    const p = sydneyParts(new Date(guess));
    const seen = Date.UTC(p.year, p.month - 1, p.day, p.hour, p.minute);
    const diff = seen - target;
    if (diff === 0) break;
    guess -= diff;
  }
  return new Date(guess);
}

/** Parses "YYYY-MM-DD" and "HH:MM" (Sydney time); missing date means today. */
export function parseSydneyDateTime(dateStr, timeStr, now = new Date()) {
  const today = sydneyParts(now);
  let [year, month, day] = [today.year, today.month, today.day];
  if (dateStr) {
    const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateStr);
    if (!m) throw new Error(`Invalid date "${dateStr}", expected YYYY-MM-DD`);
    [year, month, day] = [Number(m[1]), Number(m[2]), Number(m[3])];
  }
  const t = /^(\d{1,2}):(\d{2})$/.exec(timeStr ?? '');
  if (!t) throw new Error(`Invalid time "${timeStr}", expected HH:MM`);
  return sydneyLocalToDate(year, month, day, Number(t[1]), Number(t[2]));
}

const pad = (n) => String(n).padStart(2, '0');

/** Trip Planner itdDate / itdTime parameters. */
export function tripPlannerDateTime(date) {
  const p = sydneyParts(date);
  return { itdDate: `${p.year}${pad(p.month)}${pad(p.day)}`, itdTime: `${pad(p.hour)}${pad(p.minute)}` };
}

/** "HH:MM" in Sydney time. */
export function formatTime(date) {
  const p = sydneyParts(date);
  return `${pad(p.hour)}:${pad(p.minute)}`;
}

export const minutesBetween = (a, b) => (b.getTime() - a.getTime()) / 60000;
