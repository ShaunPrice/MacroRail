import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import http from 'node:http';
import { createApp } from '../server.js';
import { loadConfig, createServices } from '../lib/config.js';
import { estimateDrive } from '../lib/driving.js';
import { createDemoFetch } from '../lib/demo.js';
import { parseSydneyDateTime } from '../lib/time.js';

let server;
let base;

before(async () => {
  const config = loadConfig({ DEMO: '1', PORT: '0' });
  server = http.createServer(createApp(config, createServices(config)));
  await new Promise((r) => server.listen(0, r));
  base = `http://127.0.0.1:${server.address().port}`;
});
after(() => server.close());

const get = async (path) => {
  const res = await fetch(base + path);
  return { status: res.status, body: res.headers.get('content-type').includes('json') ? await res.json() : await res.text() };
};

test('serves the web page and config', async () => {
  const page = await get('/');
  assert.equal(page.status, 200);
  assert.match(page.body, /<title>NSW Commute<\/title>/);
  const cfg = await get('/api/config');
  assert.deepEqual(cfg.body, {
    demo: true, home: 'Cherrybrook', work: 'Barangaroo', via: 'Epping Station; Macquarie University Station',
    parkMinutes: 5, drivingSource: 'model', hasApiKey: true,
  });
  assert.equal((await get('/../server.js')).status, 404);
});

test('location search', async () => {
  const { body } = await get('/api/locations?q=wynyard');
  assert.equal(body[0].name, 'Wynyard Station, Sydney');
  assert.deepEqual((await get('/api/locations?q=w')).body, []);
});

test('commute estimate: depart at', async () => {
  const { status, body } = await get('/api/commute?from=Parramatta&to=Wynyard&mode=depart&date=2026-09-23&time=07:30&via=');
  assert.deepEqual(body.parkRide, []);
  assert.equal(status, 200);
  assert.deepEqual(body.errors, {});
  assert.equal(body.trips.length, 6);
  assert.ok(body.trips.every((t) => new Date(t.depart) >= new Date('2026-09-22T21:30:00Z')));
  assert.equal(body.drive.source, 'osrm');
  assert.equal(body.drive.geometry, undefined);
  assert.equal(body.drive.hazards[0].headline, 'CRASH Concord West');
  assert.equal(body.drive.incidentDelayMinutes, 15);
  assert.ok(body.drive.congestionFactor > 1.4, 'weekday AM peak');
  assert.ok(['drive', 'transit'].includes(body.recommendation.kind));
});

test('commute estimate: arrive by, trains only, with lat/lon input', async () => {
  const { body } = await get('/api/commute?from=-33.8173,151.0053&to=Central&mode=arrive&date=2026-09-23&time=08:45&railOnly=1');
  const deadline = new Date('2026-09-22T22:45:00Z');
  // The planner targets the timetable; real-time delays can push a service past the deadline.
  assert.ok(body.trips.filter((t) => !(t.maxDelayMinutes > 0)).every((t) => new Date(t.arrive) <= deadline));
  assert.ok(Math.abs(new Date(body.drive.arrive) - deadline) < 60000);
  assert.ok(new Date(body.recommendation.arrive) <= deadline);
  assert.equal(body.query.railOnly, true);
});

test('bad input returns a readable error', async () => {
  const bad = await get('/api/commute?from=Parramatta&to=Nowhereville');
  assert.equal(bad.status, 400);
  assert.match(bad.body.error, /Could not find a location matching "Nowhereville"/);
  const badTime = await get('/api/commute?from=Parramatta&to=Wynyard&time=25');
  assert.match(badTime.body.error, /Invalid time/);
});

test('partial failures are reported without losing the other results', async () => {
  const demo = createDemoFetch();
  const flaky = async (url, init) => (String(url).includes('/hazards/') || String(url).includes('/route/') ? new Response('down', { status: 503 }) : demo(url, init));
  const config = loadConfig({ DEMO: '1' });
  const s = http.createServer(createApp(config, createServices(config, flaky)));
  await new Promise((r) => s.listen(0, r));
  const res = await fetch(`http://127.0.0.1:${s.address().port}/api/commute?from=Parramatta&to=Wynyard`);
  const body = await res.json();
  s.close();
  assert.equal(body.drive, null);
  assert.match(body.errors.drive, /OSRM routing failed: HTTP 503/);
  assert.match(body.errors.traffic, /HTTP 503/);
  assert.ok(body.trips.length > 0);
  assert.equal(body.recommendation.kind, 'transit');
});

test('Google Routes provider uses traffic-aware duration', async () => {
  let request;
  const fetchImpl = async (url, init) => {
    request = { url: String(url), init };
    return new Response(JSON.stringify({
      routes: [{ duration: '2400s', staticDuration: '1500s', distanceMeters: 24000, polyline: { encodedPolyline: '_p~iF~ps|U_ulLnnqC' } }],
    }));
  };
  const when = parseSydneyDateTime('2026-09-23', '09:00');
  const d = await estimateDrive({ from: [-33.8, 151.0], to: [-33.86, 151.2], when, arriveBy: true, googleApiKey: 'k', fetchImpl });
  assert.match(request.url, /computeRoutes/);
  assert.equal(request.init.headers['X-Goog-Api-Key'], 'k');
  assert.equal(d.source, 'google');
  assert.equal(d.totalMinutes, 40);
  assert.equal(d.freeFlowMinutes, 25);
  assert.equal(d.arrive.getTime(), when.getTime());
});
