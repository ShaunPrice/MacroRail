// Simulated API responses for DEMO=1 and the test suite.
// Returns responses in the same shape as the real TfNSW, OSRM and Google APIs,
// generated around the requested time so the UI behaves as it would live.
// All numbers here are synthetic; they are not real timetables or incidents.

import { haversine } from './geo.js';
import { sydneyLocalToDate } from './time.js';

const PLACES = [
  ['10101100', 'Central Station, Sydney', -33.8832, 151.2063],
  ['10101101', 'Town Hall Station, Sydney', -33.8731, 151.2069],
  ['10101102', 'Wynyard Station, Sydney', -33.8657, 151.2057],
  ['10101103', 'Martin Place Station, Sydney', -33.8676, 151.2111],
  ['10101104', 'Circular Quay Station, Sydney', -33.8614, 151.2108],
  ['10101105', 'Redfern Station, Redfern', -33.8918, 151.1987],
  ['10101106', 'Strathfield Station, Strathfield', -33.8718, 151.0942],
  ['10101107', 'Burwood Station, Burwood', -33.8773, 151.1036],
  ['10101108', 'Parramatta Station, Parramatta', -33.8173, 151.0053],
  ['10101109', 'Blacktown Station, Blacktown', -33.7690, 150.9055],
  ['10101110', 'Penrith Station, Penrith', -33.7502, 150.6942],
  ['10101111', 'Chatswood Station, Chatswood', -33.7969, 151.1804],
  ['10101112', 'North Sydney Station, North Sydney', -33.8404, 151.2073],
  ['10101113', 'Hornsby Station, Hornsby', -33.7026, 151.0993],
  ['10101114', 'Epping Station, Epping', -33.7726, 151.0820],
  ['10101115', 'Macquarie University Station, Macquarie Park', -33.7753, 151.1147],
  ['10101116', 'Bondi Junction Station, Bondi Junction', -33.8914, 151.2477],
  ['10101117', 'Hurstville Station, Hurstville', -33.9673, 151.1024],
  ['10101118', 'Sutherland Station, Sutherland', -34.0310, 151.0579],
  ['10101119', 'Liverpool Station, Liverpool', -33.9264, 150.9255],
  ['10101120', 'Olympic Park Station, Sydney Olympic Park', -33.8474, 151.0680],
  ['10101121', 'International Airport Station, Mascot', -33.9352, 151.1662],
].map(([id, name, lat, lon]) => ({ id, name, coord: [lat, lon] }));

const HAZARDS = {
  incident: [
    { id: 901, lon: 151.0653, lat: -33.8342, displayName: 'Crash', headline: 'CRASH Concord West', mainStreet: 'Concord Road', crossStreet: 'Victoria Avenue', suburb: 'Concord West', isMajor: true, advice: 'Exercise caution' },
    { id: 902, lon: 151.1850, lat: -33.8090, displayName: 'Breakdown', headline: 'BREAKDOWN Artarmon', mainStreet: 'Pacific Highway', crossStreet: 'Mowbray Road', suburb: 'Artarmon', isMajor: false, advice: 'Reduce speed' },
  ],
  roadwork: [
    { id: 903, lon: 151.2107, lat: -33.8523, displayName: 'Roadwork', headline: 'ROADWORK Sydney Harbour Bridge', mainStreet: 'Bradfield Highway', crossStreet: '', suburb: 'Dawes Point', isMajor: false, advice: 'Lane closed' },
    { id: 904, lon: 151.0300, lat: -33.8330, displayName: 'Roadwork', headline: 'ROADWORK Granville', mainStreet: 'Parramatta Road', crossStreet: 'Good Street', suburb: 'Granville', isMajor: false, advice: 'Allow extra travel time' },
  ],
};

const json = (body, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });

// Addresses and suburbs (not stations) for trying door-to-door and park-and-ride trips.
const ADDRESSES = [
  ['suburb:cherrybrook', 'Cherrybrook', -33.7220, 151.0440, 'suburb'],
  ['suburb:castlehill', 'Castle Hill', -33.7310, 151.0040, 'suburb'],
  ['poi:unsw', 'UNSW Kensington Campus, Kensington', -33.9173, 151.2313, 'poi'],
  ['poi:barangaroo', 'Barangaroo, Sydney', -33.8610, 151.2020, 'poi'],
].map(([id, name, lat, lon, type]) => ({ id, name, coord: [lat, lon], type }));

function parseTpLocation(type, name) {
  if (type === 'coord') {
    const [lon, lat] = name.split(':').map(Number);
    return { name: `${lat.toFixed(4)}, ${lon.toFixed(4)}`, coord: [lat, lon] };
  }
  return [...PLACES, ...ADDRESSES].find((p) => p.id === name) ?? PLACES[0];
}

function nearestStation(coord) {
  return PLACES.reduce((a, b) => (haversine(coord, b.coord) < haversine(coord, a.coord) ? b : a));
}

const iso = (ms) => new Date(Math.round(ms / 60000) * 60000).toISOString().replace('.000', '');

function stopFinder(url) {
  const q = (url.searchParams.get('name_sf') ?? '').toLowerCase().replace(/\bstation\b/g, '').trim();
  const stops = PLACES.filter((p) => p.name.toLowerCase().includes(q)).map((p) => ({ ...p, type: 'stop' }));
  const other = ADDRESSES.filter((p) => p.name.toLowerCase().includes(q));
  const locations = [...stops, ...other].map((p, i) => ({
    id: p.id,
    name: p.name,
    disassembledName: p.name.split(',')[0],
    type: p.type,
    coord: p.coord,
    matchQuality: 1000 - i,
    isBest: i === 0,
  }));
  return json({ version: '10.2.1.42', locations });
}

const CBD = [-33.8832, 151.2063];
const walkMinutes = (a, b) => Math.round((haversine(a, b) * 1.3) / 80); // ~4.8 km/h

// First/last mile: walk when short, otherwise a bus (or light rail near the CBD).
function accessSegment(from, to) {
  const walk = walkMinutes(from.coord, to.coord);
  if (walk <= 0) return null;
  if (walk <= 12) return { kind: 'walk', minutes: walk, from, to };
  const lightRail = haversine(from.coord, CBD) < 6000 && haversine(to.coord, CBD) < 6000;
  return {
    kind: 'transit',
    minutes: Math.round(haversine(from.coord, to.coord) / 1000 / (lightRail ? 0.3 : 0.33)) + 3,
    from,
    to,
    cls: lightRail ? 4 : 5,
    line: lightRail ? 'L2' : '610X',
    product: lightRail ? 'Sydney Light Rail' : 'Sydney Buses Network',
  };
}

// Rail between two stations; changes at a station on the way when the trip is long.
function railSegments(a, b) {
  const rideMin = (x, y) => Math.max(4, Math.round(haversine(x.coord, y.coord) / 1000 / 0.75)); // ~45 km/h
  const direct = haversine(a.coord, b.coord);
  const via = PLACES.filter((p) => p !== a && p !== b)
    .map((p) => ({ p, detour: haversine(a.coord, p.coord) + haversine(p.coord, b.coord) - direct }))
    .sort((x, y) => x.detour - y.detour)[0];
  const train = (from, to, line) => ({ kind: 'transit', minutes: rideMin(from, to), from, to, cls: 1, line, product: 'Sydney Trains Network' });
  if (direct > 15000 && via && via.detour < 1500) return [train(a, via.p, 'T1'), { kind: 'change', minutes: 4 }, train(via.p, b, 'T9')];
  return [train(a, b, 'T1')];
}

function buildLeg(seg, dep, delay, slot, cancelled) {
  const arr = dep + seg.minutes * 60000;
  if (seg.kind === 'walk') {
    return {
      duration: seg.minutes * 60,
      origin: { name: seg.from.name, coord: seg.from.coord },
      destination: { name: seg.to.name, coord: seg.to.coord },
      transportation: { product: { class: 100, name: 'footpath' } },
    };
  }
  const platform = seg.cls === 1 ? `Platform ${1 + (slot % 4)}` : seg.cls === 4 ? 'Light Rail' : 'Stand A';
  return {
    duration: seg.minutes * 60,
    isRealtimeControlled: true,
    realtimeStatus: cancelled ? ['MONITORED', 'CANCELLED'] : ['MONITORED'],
    origin: {
      name: `${seg.from.name}, ${platform}`,
      disassembledName: platform,
      departureTimePlanned: iso(dep),
      departureTimeEstimated: iso(dep + delay * 60000),
    },
    destination: { name: seg.to.name, arrivalTimePlanned: iso(arr), arrivalTimeEstimated: iso(arr + delay * 60000) },
    transportation: {
      number: seg.line,
      disassembledName: seg.line,
      product: { class: seg.cls, name: seg.product },
      destination: { name: seg.to.name.split(',')[0].replace(' Station', '') },
    },
    infos: delay >= 5 ? [{ priority: 'high', subtitle: 'Trains running late due to an earlier signal repair (simulated)' }] : [],
  };
}

function trip(url) {
  const sp = url.searchParams;
  const origin = parseTpLocation(sp.get('type_origin'), sp.get('name_origin'));
  const dest = parseTpLocation(sp.get('type_destination'), sp.get('name_destination'));
  const d = sp.get('itdDate');
  const t = sp.get('itdTime');
  const when = sydneyLocalToDate(+d.slice(0, 4), +d.slice(4, 6), +d.slice(6, 8), +t.slice(0, 2), +t.slice(2, 4)).getTime();
  const arriveBy = sp.get('depArrMacro') === 'arr';
  const count = Number(sp.get('calcNumberOfTrips') ?? 5);
  const railOnly = sp.get('exclMOT_5') === '1';

  const fromStop = nearestStation(origin.coord);
  const toStop = nearestStation(dest.coord);
  const access = (a, b) => {
    const seg = accessSegment(a, b);
    return seg && railOnly && seg.kind === 'transit' ? { ...seg, kind: 'walk', minutes: walkMinutes(a.coord, b.coord) } : seg;
  };
  const segments = [access(origin, fromStop), ...railSegments(fromStop, toStop), access(toStop, dest)].filter(Boolean);

  // Services are timed around the first rail leg, which runs every 10 minutes.
  const railIdx = segments.findIndex((s) => s.cls === 1);
  const before = segments.slice(0, railIdx).reduce((m, s) => m + s.minutes, 0);
  const after = segments.slice(railIdx).reduce((m, s) => m + s.minutes, 0);
  const headway = 10 * 60000;
  const firstRailDep = arriveBy
    ? Math.floor((when - after * 60000) / headway) * headway - (count - 1) * headway
    : Math.ceil((when + before * 60000) / headway) * headway;

  const journeys = [];
  for (let i = 0; i < count; i++) {
    const railDep = firstRailDep + i * headway;
    const slot = Math.floor(railDep / headway);
    const delay = slot % 3 === 1 ? 2 : slot % 7 === 3 ? 6 : 0;
    const cancelled = slot % 11 === 5;
    let clock = railDep - before * 60000;
    const legs = [];
    for (const seg of segments) {
      if (seg.kind !== 'change') legs.push(buildLeg(seg, clock, seg.cls ? delay : 0, slot, cancelled && seg === segments[railIdx]));
      clock += seg.minutes * 60000;
    }
    journeys.push({ legs });
  }
  return json({ version: '10.2.1.42', journeys });
}

function hazards(url) {
  const category = url.pathname.split('/')[4];
  const features = (HAZARDS[category] ?? []).map((h) => ({
    type: 'Feature',
    id: h.id,
    geometry: { type: 'Point', coordinates: [h.lon, h.lat] },
    properties: {
      headline: h.headline,
      displayName: h.displayName,
      mainCategory: h.displayName,
      isMajor: h.isMajor,
      ended: false,
      adviceA: h.advice,
      lastUpdated: Date.now() - 15 * 60000,
      roads: [{ mainStreet: h.mainStreet, crossStreet: h.crossStreet, suburb: h.suburb }],
    },
  }));
  return json({ type: 'FeatureCollection', features });
}

// Road path approximated by a dog-leg through a waypoint, sampled every ~200 m.
function osrm(url) {
  const [a, b] = url.pathname
    .split('/')
    .pop()
    .split(';')
    .map((s) => s.split(',').map(Number)); // [lon, lat]
  const mid = [a[0] + (b[0] - a[0]) * 0.5, a[1] + (b[1] - a[1]) * 0.5 - 0.004];
  const coords = [];
  for (const [p, q] of [[a, mid], [mid, b]]) {
    const n = Math.max(2, Math.ceil(haversine([p[1], p[0]], [q[1], q[0]]) / 200));
    for (let i = 0; i < n; i++) coords.push([p[0] + ((q[0] - p[0]) * i) / n, p[1] + ((q[1] - p[1]) * i) / n]);
  }
  coords.push(b);
  const distance = haversine([a[1], a[0]], [b[1], b[0]]) * 1.3;
  return json({ code: 'Ok', routes: [{ distance, duration: distance / (55 / 3.6), geometry: { type: 'LineString', coordinates: coords } }] });
}

/** A fetch() replacement that serves simulated responses. */
export function createDemoFetch() {
  return async (input) => {
    const url = new URL(String(input));
    if (url.pathname.startsWith('/v1/tp/stop_finder')) return stopFinder(url);
    if (url.pathname.startsWith('/v1/tp/trip')) return trip(url);
    if (url.pathname.startsWith('/v1/live/hazards/')) return hazards(url);
    if (url.pathname.startsWith('/route/v1/driving/')) return osrm(url);
    return json({ error: `demo: no simulated response for ${url.pathname}` }, 404);
  };
}
