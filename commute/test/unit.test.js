import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { sydneyParts, sydneyLocalToDate, parseSydneyDateTime, tripPlannerDateTime, formatTime } from '../lib/time.js';
import { haversine, distanceToPolyline, decodePolyline, parseLatLon } from '../lib/geo.js';
import { congestionFactor, hazardDelayMinutes } from '../lib/traffic.js';
import { summariseJourney, normaliseHazard, TfnswClient } from '../lib/tfnsw.js';
import { hazardsOnRoute } from '../lib/driving.js';
import { recommend } from '../lib/commute.js';

const fixture = (name) => JSON.parse(readFileSync(new URL(`./fixtures/${name}`, import.meta.url)));

test('Sydney time conversion handles AEST and AEDT', () => {
  // AEST (UTC+10) in July, AEDT (UTC+11) in January.
  assert.equal(sydneyLocalToDate(2026, 7, 1, 8, 30).toISOString(), '2026-06-30T22:30:00.000Z');
  assert.equal(sydneyLocalToDate(2026, 1, 15, 8, 30).toISOString(), '2026-01-14T21:30:00.000Z');
  // Daylight saving starts Sunday 4 October 2026: 08:30 is already AEDT.
  assert.equal(sydneyLocalToDate(2026, 10, 4, 8, 30).toISOString(), '2026-10-03T21:30:00.000Z');
  const p = sydneyParts(new Date('2026-09-22T22:05:00Z'));
  assert.deepEqual([p.year, p.month, p.day, p.hour, p.minute, p.weekday], [2026, 9, 23, 8, 5, 3]);
});

test('parseSydneyDateTime and Trip Planner parameters', () => {
  const d = parseSydneyDateTime('2026-09-23', '07:05');
  assert.deepEqual(tripPlannerDateTime(d), { itdDate: '20260923', itdTime: '0705' });
  assert.equal(formatTime(d), '07:05');
  assert.throws(() => parseSydneyDateTime('23/09/2026', '07:05'), /Invalid date/);
  assert.throws(() => parseSydneyDateTime(null, '7am'), /Invalid time/);
});

test('geometry helpers', () => {
  // Central to Wynyard is roughly 2 km.
  const d = haversine([-33.8832, 151.2063], [-33.8657, 151.2057]);
  assert.ok(d > 1900 && d < 2000, `got ${d}`);
  const line = [[-33.80, 151.00], [-33.80, 151.10]];
  const off = distanceToPolyline([-33.801, 151.05], line);
  assert.ok(Math.abs(off - 111) < 3, `got ${off}`);
  assert.ok(Math.abs(distanceToPolyline([-33.80, 151.11], line) - haversine([-33.80, 151.11], [-33.80, 151.10])) < 2);
  // Reference example from Google's polyline documentation.
  assert.deepEqual(decodePolyline('_p~iF~ps|U_ulLnnqC_mqNvxq`@'), [[38.5, -120.2], [40.7, -120.95], [43.252, -126.453]]);
  assert.deepEqual(parseLatLon(' -33.86, 151.2 '), [-33.86, 151.2]);
  assert.equal(parseLatLon('Central'), null);
});

test('congestion profile peaks on weekday commute hours only', () => {
  const at = (date, time) => congestionFactor(parseSydneyDateTime(date, time));
  assert.equal(at('2026-09-23', '03:00'), 1.0); // Wednesday night
  assert.equal(at('2026-09-23', '08:00'), 1.6); // Wednesday AM peak
  assert.equal(at('2026-09-23', '08:30'), (1.6 + 1.35) / 2); // interpolated
  assert.ok(at('2026-09-26', '08:00') < 1.2); // Saturday
  assert.equal(hazardDelayMinutes({ isMajor: true, category: 'roadwork' }), 15);
  assert.equal(hazardDelayMinutes({ isMajor: false, category: 'roadwork' }), 3);
  assert.equal(hazardDelayMinutes({ isMajor: false, category: 'unknown' }), 4);
});

test('summariseJourney parses rapidJSON journeys with real-time data', () => {
  const [j, cancelledBus] = fixture('trip.json').journeys.map(summariseJourney);
  assert.equal(j.depart.toISOString(), '2026-09-22T21:26:00.000Z'); // 7 min walk before 21:33 estimated
  assert.equal(j.arrive.toISOString(), '2026-09-22T22:07:00.000Z'); // 5 min walk after 22:02 estimated
  assert.equal(j.durationMinutes, 41);
  assert.equal(j.maxDelayMinutes, 3);
  assert.equal(j.realtime, true);
  assert.equal(j.interchanges, 0);
  assert.deepEqual(j.alerts, ['Allow extra travel time']);
  assert.deepEqual(j.legs.map((l) => l.mode), ['Walk', 'Train', 'Walk']);
  assert.equal(j.legs[1].line, 'T1');
  assert.equal(j.legs[1].fromPlatform, 'Platform 3');
  assert.equal(cancelledBus.cancelled, true);
  assert.equal(cancelledBus.legs[0].mode, 'Bus');
  assert.equal(cancelledBus.maxDelayMinutes, null);
});

test('normaliseHazard reads Live Traffic GeoJSON and skips ended hazards', () => {
  const hazards = fixture('hazards-incident.json').features.map((f) => normaliseHazard(f, 'incident')).filter(Boolean);
  assert.equal(hazards.length, 2);
  assert.deepEqual(hazards[0].coord, [-33.8342, 151.0653]);
  assert.equal(hazards[0].road, 'Concord Road near Victoria Avenue');
  assert.equal(hazards[0].isMajor, true);
  assert.deepEqual(hazards[1].coord, [-33.8, 151.1]);
  assert.equal(hazards[1].headline, 'Breakdown');
});

test('hazardsOnRoute keeps only hazards within the buffer', () => {
  const route = [[-33.83, 151.00], [-33.83, 151.10]];
  const hazards = [
    { id: 'near', coord: [-33.8315, 151.05] }, // ~170 m
    { id: 'far', coord: [-33.84, 151.05] }, // ~1.1 km
    { id: 'outside', coord: [-33.83, 151.3] },
  ];
  assert.deepEqual(hazardsOnRoute(route, hazards, 250).map((h) => h.id), ['near']);
});

test('TfnswClient sends the API key and Trip Planner parameters', async () => {
  let seen;
  const client = new TfnswClient({
    apiKey: 'secret',
    fetchImpl: async (url, init) => {
      seen = { url: new URL(url), init };
      return new Response(JSON.stringify({ journeys: [] }));
    },
  });
  await client.planTrips({
    from: { coord: [-33.8, 151.0] },
    to: { id: '10101100' },
    when: parseSydneyDateTime('2026-09-23', '08:45'),
    arriveBy: true,
    railOnly: true,
  });
  const q = seen.url.searchParams;
  assert.equal(seen.init.headers.Authorization, 'apikey secret');
  assert.equal(seen.url.pathname, '/v1/tp/trip');
  assert.equal(q.get('depArrMacro'), 'arr');
  assert.equal(q.get('itdTime'), '0845');
  assert.equal(q.get('type_origin'), 'coord');
  assert.equal(q.get('name_origin'), '151:-33.8:EPSG:4326');
  assert.equal(q.get('name_destination'), '10101100');
  assert.equal(q.get('exclMOT_5'), '1');
  assert.equal(q.get('exclMOT_1'), null);
  await client.planTrips({ from: { id: '2150172', coord: [-33.8, 151.0] }, to: { id: 'y' }, when: new Date() });
  assert.equal(seen.url.searchParams.get('name_origin'), '2150172', 'a known stop id is preferred over coordinates');
  await assert.rejects(new TfnswClient({}).findLocations('x'), /TFNSW_API_KEY/);
});

test('recommend picks earliest arrival, or latest on-time departure for arrive-by', () => {
  const t = (hhmm) => new Date(`2026-09-22T${hhmm}:00Z`);
  const drive = { depart: t('21:30'), arrive: t('22:15'), totalMinutes: 45 };
  const trips = [
    { depart: t('21:25'), arrive: t('22:00'), durationMinutes: 35, cancelled: false },
    { depart: t('21:35'), arrive: t('22:05'), durationMinutes: 30, cancelled: true },
    { depart: t('21:45'), arrive: t('22:20'), durationMinutes: 35, cancelled: false },
  ];
  const dep = recommend({ drive, trips, when: t('21:20'), arriveBy: false });
  assert.equal(dep.kind, 'transit');
  assert.equal(dep.index, 0);

  const arr = recommend({ drive, trips, when: t('22:16'), arriveBy: true });
  assert.equal(arr.kind, 'drive'); // leaves 21:30, later than the 21:25 train; cancelled train ignored

  const late = recommend({ drive, trips: [], when: t('22:00'), arriveBy: true });
  assert.match(late.reason, /15 min late/);
  assert.equal(recommend({ drive: null, trips: [], when: t('22:00') }), null);
});
