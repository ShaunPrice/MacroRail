#!/usr/bin/env node
// Usage: node cli.js [--from "..."] [--to "..."] [--depart HH:MM | --arrive HH:MM] [--date YYYY-MM-DD] [--rail] [--reverse] [--json]
// Coordinates starting with "-" must use the = form: --from=-33.8173,151.0053

import { parseArgs } from 'node:util';
import { loadConfig, createServices } from './lib/config.js';
import { planCommute } from './lib/commute.js';
import { formatTime, parseSydneyDateTime } from './lib/time.js';

const { values: args } = parseArgs({
  options: {
    from: { type: 'string' },
    to: { type: 'string' },
    depart: { type: 'string' },
    arrive: { type: 'string' },
    date: { type: 'string' },
    rail: { type: 'boolean', default: false },
    reverse: { type: 'boolean', default: false },
    json: { type: 'boolean', default: false },
  },
});

const config = loadConfig();
const services = createServices(config);
let from = args.from ?? config.home;
let to = args.to ?? config.work;
if (args.reverse) [from, to] = [to, from];
const arriveBy = Boolean(args.arrive);
const time = args.arrive ?? args.depart;

const result = await planCommute({
  ...services,
  from,
  to,
  when: time ? parseSydneyDateTime(args.date, time) : new Date(),
  arriveBy,
  railOnly: args.rail,
});

if (args.json) {
  if (result.drive) delete result.drive.geometry;
  console.log(JSON.stringify(result, null, 2));
  process.exit(0);
}

const t = formatTime;
console.log(`\n${result.origin.name}  ->  ${result.destination.name}${config.demo ? '   [DEMO: simulated data]' : ''}`);

const r = result.recommendation;
if (r) console.log(`\nRecommended: ${r.label}, leave ${t(r.depart)}, arrive ${t(r.arrive)} (${r.minutes} min). ${r.reason}`);

const d = result.drive;
if (d) {
  console.log(`\nDRIVE  ${d.totalMinutes} min, ${d.distanceKm} km   leave ${t(d.depart)} -> arrive ${t(d.arrive)}`);
  if (d.source === 'google') console.log('  Traffic-aware estimate from Google Routes.');
  else console.log(`  Free-flow ${d.freeFlowMinutes} min x congestion ${d.congestionFactor} + hazards ${d.incidentDelayMinutes} min (model estimate)`);
  for (const h of d.hazards) console.log(`  ! ${h.headline}${h.road ? ` - ${h.road}` : ''}${h.delayMinutes ? ` (+${h.delayMinutes} min)` : ''}`);
} else console.log(`\nDRIVE  unavailable: ${result.errors.drive}`);

console.log(`\nPUBLIC TRANSPORT${args.rail ? ' (rail only)' : ''}`);
if (!result.trips.length) console.log(`  none found${result.errors.transit ? `: ${result.errors.transit}` : ''}`);
for (const j of result.trips) {
  const legs = j.legs.filter((l) => l.mode !== 'Walk').map((l) => `${l.line ?? l.mode}`).join(' > ');
  const status = j.cancelled ? 'CANCELLED' : j.maxDelayMinutes > 0 ? `+${j.maxDelayMinutes} late` : j.realtime ? 'on time' : 'timetable';
  console.log(`  ${t(j.depart)} -> ${t(j.arrive)}  ${String(j.durationMinutes).padStart(3)} min  ${legs}  [${status}]`);
}
if (result.errors.traffic) console.log(`\nNote: live traffic unavailable: ${result.errors.traffic}`);
console.log();
