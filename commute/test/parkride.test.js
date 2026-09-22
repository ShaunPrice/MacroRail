import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import http from 'node:http';
import { readFileSync } from 'node:fs';
import { createApp } from '../server.js';
import { loadConfig, createServices } from '../lib/config.js';
import { planStation, parseStationList } from '../lib/parkride.js';
import { summariseJourney } from '../lib/tfnsw.js';
import { recommend } from '../lib/commute.js';

const MIN = 60000;
let server;
let base;

before(async () => {
  const config = loadConfig({ DEMO: '1' });
  server = http.createServer(createApp(config, createServices(config)));
  await new Promise((r) => server.listen(0, r));
  base = `http://127.0.0.1:${server.address().port}`;
});
after(() => server.close());

const commute = async (params) => {
  const res = await fetch(`${base}/api/commute?${new URLSearchParams(params)}`);
  return { status: res.status, body: await res.json() };
};
const d = (s) => new Date(s);
const catchTime = (o) => d(o.catchBy);

test('parseStationList splits on semicolons and new lines', () => {
  assert.deepEqual(parseStationList(' Epping Station, Epping ; Hornsby\nCentral;; '), ['Epping Station, Epping', 'Hornsby', 'Central']);
  assert.deepEqual(parseStationList(''), []);
  assert.deepEqual(parseStationList(['A', ' ']), ['A']);
});

test('drive then train: leave-by times reach the platform in time for each service', async () => {
  const when = '2026-09-22T21:30:00Z'; // 07:30 Sydney
  const { body } = await commute({ from: 'Cherrybrook', to: 'UNSW', date: '2026-09-23', time: '07:30', via: 'Epping Station; Macquarie University Station', park: '5' });
  assert.equal(body.parkRide.length, 2);
  for (const s of body.parkRide) {
    assert.equal(s.error, null);
    assert.ok(s.options.length > 0 && s.options.length <= 3);
    for (const o of s.options) {
      assert.ok(d(o.depart) >= d(when) - MIN, 'never leave before the requested time');
      assert.ok(+d(o.drive.arrive) + o.parkMinutes * MIN <= +catchTime(o) + MIN, 'on the platform by the catch time');
      assert.ok(catchTime(o) <= d(o.trip.depart), 'catch time never after the service departs');
      assert.equal(o.arrive, o.trip.arrive);
    }
    const departs = s.options.map((o) => +d(o.depart));
    assert.deepEqual(departs, [...departs].sort((a, b) => a - b));
  }
  // Multi-modal: the trip to UNSW changes trains and finishes on light rail.
  const modes = body.parkRide[0].options[0].trip.legs.map((l) => l.mode);
  assert.ok(modes.filter((m) => m === 'Train').length >= 2, `modes: ${modes}`);
  assert.equal(modes.at(-1), 'Light Rail');
  assert.ok(body.parkRide[0].options[0].trip.interchanges >= 2);
});

test('drive then train, arrive by: every option meets the deadline unless running late', async () => {
  const deadline = d('2026-09-22T22:45:00Z');
  const { body } = await commute({ from: 'Cherrybrook', to: 'Barangaroo', mode: 'arrive', date: '2026-09-23', time: '08:45' });
  const options = body.parkRide.flatMap((s) => s.options);
  assert.ok(options.length > 0);
  for (const o of options) if (!(o.trip.maxDelayMinutes > 0)) assert.ok(d(o.arrive) <= deadline);
  assert.ok(d(body.recommendation.arrive) <= deadline);
});

test('train then drive (car parked at the destination end)', async () => {
  const { body } = await commute({ from: 'Barangaroo', to: 'Cherrybrook', date: '2026-09-23', time: '17:15', parkAt: 'end', via: 'Epping Station' });
  assert.equal(body.query.parkAt, 'end');
  const [s] = body.parkRide;
  assert.ok(s.options.length > 0);
  for (const o of s.options) {
    assert.equal(o.catchBy, null);
    assert.equal(o.depart, o.trip.depart);
    assert.ok(+d(o.drive.depart) >= +d(o.trip.arrive) + o.parkMinutes * MIN, 'walk to the car before driving');
    assert.equal(o.arrive, o.drive.arrive);
    assert.ok(d(o.depart) >= d('2026-09-23T07:14:00Z'));
  }
});

test('an unknown station is reported without affecting the other results', async () => {
  const { status, body } = await commute({ from: 'Cherrybrook', to: 'Barangaroo', via: 'Nowhere Station; Epping Station' });
  assert.equal(status, 200);
  assert.match(body.parkRide[0].error, /Could not find a location matching "Nowhere Station"/);
  assert.ok(body.parkRide[1].options.length > 0);
  assert.ok(body.drive && body.trips.length);
  const bad = await commute({ from: 'Cherrybrook', to: 'Barangaroo', park: '-3' });
  assert.equal(bad.status, 400);
});

test('a late-running service is planned against its timetabled departure', async () => {
  // Fixture: walk 7 min, T1 planned 21:30Z but estimated 21:33Z (3 min late).
  const journey = summariseJourney(JSON.parse(readFileSync(new URL('./fixtures/trip.json', import.meta.url))).journeys[0]);
  assert.equal(journey.depart.toISOString(), '2026-09-22T21:26:00.000Z');
  const client = { planTrips: async () => [journey] };
  const googleStub = async () => new Response(JSON.stringify({ routes: [{ duration: '600s', staticDuration: '600s', distanceMeters: 8000 }] }));
  const [o] = await planStation({
    client,
    origin: { coord: [-33.72, 151.04] },
    destination: { id: 'x' },
    station: { name: 'Parramatta Station', coord: [-33.8173, 151.0053], id: '2150172' },
    when: d('2026-09-22T21:00:00Z'),
    parkMinutes: 5,
    driveOptions: { googleApiKey: 'k', fetchImpl: googleStub },
  });
  assert.equal(o.catchBy.toISOString(), '2026-09-22T21:23:00.000Z'); // 3 min before the delayed start
  assert.equal(o.depart.toISOString(), '2026-09-22T21:08:00.000Z'); // minus 5 min park, 10 min drive
  assert.equal(o.totalMinutes, 59);
});

test('recommend considers park and ride and skips cancelled services', () => {
  const t = (hhmm) => d(`2026-09-22T${hhmm}:00Z`);
  const drive = { depart: t('21:30'), arrive: t('22:30'), totalMinutes: 60 };
  const parkRide = [{
    station: { name: 'Epping Station, Epping' },
    options: [
      { depart: t('21:30'), arrive: t('22:10'), totalMinutes: 40, cancelled: true, catchBy: t('21:50') },
      { depart: t('21:40'), arrive: t('22:20'), totalMinutes: 40, cancelled: false, catchBy: t('22:00') },
    ],
  }];
  const r = recommend({ drive, trips: [], parkRide, when: t('21:30'), arriveBy: false });
  assert.equal(r.kind, 'parkride');
  assert.equal(r.index, 1);
  assert.equal(r.label, 'Drive to Epping Station + public transport');
});
