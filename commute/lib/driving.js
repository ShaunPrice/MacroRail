// Driving time: traffic-aware Google Routes API when a key is configured,
// otherwise OSRM free-flow time adjusted by the congestion model and live hazards.

import { decodePolyline, distanceToPolyline, boundingBox, inBox } from './geo.js';
import { congestionFactor, hazardDelayMinutes, DEFAULT_CONGESTION_PROFILE, DEFAULT_HAZARD_DELAYS } from './traffic.js';

export const OSRM_DEFAULT_URL = 'https://router.project-osrm.org';
const GOOGLE_ROUTES_URL = 'https://routes.googleapis.com/directions/v2:computeRoutes';

export async function osrmRoute(from, to, { fetchImpl = globalThis.fetch, baseUrl = OSRM_DEFAULT_URL } = {}) {
  const coords = `${from[1]},${from[0]};${to[1]},${to[0]}`;
  const url = new URL(`/route/v1/driving/${coords}`, baseUrl);
  url.searchParams.set('overview', 'full');
  url.searchParams.set('geometries', 'geojson');
  const res = await fetchImpl(url, { headers: { 'User-Agent': 'nsw-commute/0.1' } });
  if (!res.ok) throw new Error(`OSRM routing failed: HTTP ${res.status}`);
  const data = await res.json();
  const route = data.routes?.[0];
  if (data.code !== 'Ok' || !route) throw new Error(`OSRM routing failed: ${data.code ?? 'no route'}`);
  return {
    source: 'osrm',
    distanceMeters: route.distance,
    freeFlowSeconds: route.duration,
    trafficSeconds: null,
    geometry: route.geometry.coordinates.map(([lon, lat]) => [lat, lon]),
  };
}

export async function googleRoute(from, to, { apiKey, departAt, fetchImpl = globalThis.fetch }) {
  const body = {
    origin: { location: { latLng: { latitude: from[0], longitude: from[1] } } },
    destination: { location: { latLng: { latitude: to[0], longitude: to[1] } } },
    travelMode: 'DRIVE',
    routingPreference: 'TRAFFIC_AWARE_OPTIMAL',
  };
  // The API rejects departure times in the past; omitting it means "now".
  if (departAt && departAt.getTime() > Date.now() + 60000) body.departureTime = departAt.toISOString();
  const res = await fetchImpl(GOOGLE_ROUTES_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Goog-Api-Key': apiKey,
      'X-Goog-FieldMask': 'routes.duration,routes.staticDuration,routes.distanceMeters,routes.polyline.encodedPolyline',
    },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`Google Routes failed: HTTP ${res.status} ${(await res.text()).slice(0, 200)}`);
  const route = (await res.json()).routes?.[0];
  if (!route) throw new Error('Google Routes returned no route');
  const secs = (d) => (d ? Number(String(d).replace(/s$/, '')) : null);
  return {
    source: 'google',
    distanceMeters: route.distanceMeters,
    freeFlowSeconds: secs(route.staticDuration) ?? secs(route.duration),
    trafficSeconds: secs(route.duration),
    geometry: decodePolyline(route.polyline?.encodedPolyline ?? ''),
  };
}

/** Hazards within `bufferMeters` of the route, nearest first. */
export function hazardsOnRoute(geometry, hazards, bufferMeters = 250) {
  if (!geometry.length) return [];
  const box = boundingBox(geometry, bufferMeters);
  return hazards
    .filter((h) => inBox(h.coord, box))
    .map((h) => ({ ...h, distanceFromRouteMeters: Math.round(distanceToPolyline(h.coord, geometry)) }))
    .filter((h) => h.distanceFromRouteMeters <= bufferMeters)
    .sort((a, b) => a.distanceFromRouteMeters - b.distanceFromRouteMeters);
}

/**
 * Estimates the drive for a departure time, or for an arrival deadline.
 * @param {object} o
 * @param {number[]} o.from [lat, lon]
 * @param {number[]} o.to   [lat, lon]
 * @param {Date} o.when
 * @param {boolean} o.arriveBy
 * @param {Array} o.hazards normalised TfNSW hazards
 */
export async function estimateDrive({
  from,
  to,
  when,
  arriveBy = false,
  hazards = [],
  googleApiKey,
  osrmUrl,
  fetchImpl = globalThis.fetch,
  profile = DEFAULT_CONGESTION_PROFILE,
  hazardDelays = DEFAULT_HAZARD_DELAYS,
  hazardBufferMeters = 250,
}) {
  const route = googleApiKey
    ? await googleRoute(from, to, { apiKey: googleApiKey, departAt: arriveBy ? null : when, fetchImpl })
    : await osrmRoute(from, to, { fetchImpl, baseUrl: osrmUrl });

  const onRoute = hazardsOnRoute(route.geometry, hazards, hazardBufferMeters).map((h) => ({
    ...h,
    delayMinutes: route.source === 'google' ? null : hazardDelayMinutes(h, hazardDelays),
  }));
  const freeFlowMinutes = route.freeFlowSeconds / 60;

  let totalMinutes;
  let factor = null;
  let incidentMinutes = 0;
  let departAt;
  if (route.source === 'google') {
    totalMinutes = (route.trafficSeconds ?? route.freeFlowSeconds) / 60;
    departAt = arriveBy ? new Date(when.getTime() - totalMinutes * 60000) : when;
  } else {
    incidentMinutes = onRoute.reduce((s, h) => s + h.delayMinutes, 0);
    const minutesFor = (dep) => {
      factor = congestionFactor(dep, profile);
      return freeFlowMinutes * factor + incidentMinutes;
    };
    if (arriveBy) {
      // Congestion depends on departure time, which depends on duration: iterate to a fixed point.
      departAt = new Date(when.getTime() - freeFlowMinutes * 60000);
      for (let i = 0; i < 4; i++) {
        totalMinutes = minutesFor(departAt);
        departAt = new Date(when.getTime() - totalMinutes * 60000);
      }
    } else {
      departAt = when;
      totalMinutes = minutesFor(when);
    }
  }

  return {
    source: route.source,
    distanceKm: Math.round(route.distanceMeters / 100) / 10,
    freeFlowMinutes: Math.round(freeFlowMinutes),
    congestionFactor: factor != null ? Math.round(factor * 100) / 100 : null,
    incidentDelayMinutes: incidentMinutes,
    totalMinutes: Math.round(totalMinutes),
    depart: departAt,
    arrive: new Date(departAt.getTime() + totalMinutes * 60000),
    hazards: onRoute,
    geometry: route.geometry,
  };
}
