// Client for the Transport for NSW Open Data APIs.
//   Trip Planner:  https://opendata.transport.nsw.gov.au/dataset/trip-planner-apis
//   Live Traffic:  https://opendata.transport.nsw.gov.au/dataset/live-traffic-hazards

import { tripPlannerDateTime, minutesBetween } from './time.js';

export const TFNSW_BASE_URL = 'https://api.transport.nsw.gov.au';
const TP_VERSION = '10.2.1.42';

export const HAZARD_CATEGORIES = ['incident', 'roadwork', 'fire', 'flood', 'majorevent', 'alpine'];

// Trip Planner product classes (transportation.product.class).
const PRODUCT_CLASSES = {
  1: 'Train',
  2: 'Metro',
  4: 'Light Rail',
  5: 'Bus',
  7: 'Coach',
  9: 'Ferry',
  11: 'School Bus',
  99: 'Walk',
  100: 'Walk',
  107: 'Cycle',
};
const NON_RAIL_CLASSES = [4, 5, 7, 9, 11];
const WALK_CLASSES = new Set([99, 100]);

export class TfnswClient {
  constructor({ apiKey, fetchImpl = globalThis.fetch, baseUrl = TFNSW_BASE_URL } = {}) {
    this.apiKey = apiKey;
    this.fetch = fetchImpl;
    this.baseUrl = baseUrl;
  }

  async get(path, params = {}) {
    if (!this.apiKey) throw new Error('TFNSW_API_KEY is not set');
    const url = new URL(path, this.baseUrl);
    for (const [k, v] of Object.entries(params)) if (v !== undefined) url.searchParams.set(k, String(v));
    const res = await this.fetch(url, {
      headers: { Authorization: `apikey ${this.apiKey}`, Accept: 'application/json' },
    });
    if (!res.ok) {
      const body = await res.text().catch(() => '');
      throw new Error(`TfNSW ${url.pathname} failed: HTTP ${res.status} ${body.slice(0, 200)}`);
    }
    return res.json();
  }

  /** Searches stops, addresses and points of interest. */
  async findLocations(query, limit = 8) {
    const data = await this.get('/v1/tp/stop_finder', {
      outputFormat: 'rapidJSON',
      coordOutputFormat: 'EPSG:4326',
      type_sf: 'any',
      name_sf: query,
      TfNSWSF: 'true',
      version: TP_VERSION,
    });
    return (data.locations ?? [])
      .filter((l) => Array.isArray(l.coord))
      .sort((a, b) => (b.isBest === true) - (a.isBest === true) || (b.matchQuality ?? 0) - (a.matchQuality ?? 0))
      .slice(0, limit)
      .map((l) => ({ id: l.id, name: l.name, type: l.type, coord: l.coord }));
  }

  /**
   * Plans public transport journeys (includes real-time estimates when available).
   * @param {object} o
   * @param {{coord?: number[], id?: string}} o.from
   * @param {{coord?: number[], id?: string}} o.to
   * @param {Date} o.when
   * @param {boolean} o.arriveBy
   * @param {boolean} o.railOnly  exclude bus, coach, ferry and light rail
   */
  async planTrips({ from, to, when, arriveBy = false, railOnly = false, count = 6 }) {
    const { itdDate, itdTime } = tripPlannerDateTime(when);
    const params = {
      outputFormat: 'rapidJSON',
      coordOutputFormat: 'EPSG:4326',
      depArrMacro: arriveBy ? 'arr' : 'dep',
      itdDate,
      itdTime,
      ...locationParams('origin', from),
      ...locationParams('destination', to),
      calcNumberOfTrips: count,
      TfNSWTR: 'true',
      version: TP_VERSION,
    };
    if (railOnly) {
      params.excludedMeans = 'checkbox';
      for (const c of NON_RAIL_CLASSES) params[`exclMOT_${c}`] = 1;
    }
    const data = await this.get('/v1/tp/trip', params);
    return (data.journeys ?? []).map(summariseJourney).filter(Boolean);
  }

  /** Open Live Traffic hazards as normalised records. */
  async openHazards(categories = HAZARD_CATEGORIES) {
    const results = await Promise.allSettled(
      categories.map((c) => this.get(`/v1/live/hazards/${c}/open`).then((fc) => ({ c, fc }))),
    );
    const hazards = [];
    const errors = [];
    for (const r of results) {
      if (r.status === 'rejected') {
        errors.push(r.reason.message);
        continue;
      }
      for (const f of r.value.fc.features ?? []) {
        const h = normaliseHazard(f, r.value.c);
        if (h) hazards.push(h);
      }
    }
    if (hazards.length === 0 && errors.length === categories.length) throw new Error(errors[0]);
    return hazards;
  }
}

function locationParams(prefix, loc) {
  if (loc.coord) {
    const [lat, lon] = loc.coord;
    return { [`type_${prefix}`]: 'coord', [`name_${prefix}`]: `${lon}:${lat}:EPSG:4326` };
  }
  return { [`type_${prefix}`]: 'any', [`name_${prefix}`]: loc.id };
}

function firstCoord(geometry) {
  let c = geometry?.coordinates;
  while (Array.isArray(c) && Array.isArray(c[0])) c = c[0];
  return Array.isArray(c) && c.length >= 2 ? [c[1], c[0]] : null;
}

export function normaliseHazard(feature, category) {
  const coord = firstCoord(feature.geometry);
  if (!coord) return null;
  const p = feature.properties ?? {};
  if (p.ended === true) return null;
  const road = p.roads?.[0] ?? {};
  return {
    id: String(feature.id ?? p.id ?? `${category}-${coord.join(',')}`),
    category,
    type: p.displayName || p.mainCategory || category,
    headline: p.headline || p.displayName || p.mainCategory || category,
    road: [road.mainStreet, road.crossStreet && `near ${road.crossStreet}`].filter(Boolean).join(' '),
    suburb: road.suburb ?? '',
    isMajor: p.isMajor === true,
    advice: [p.adviceA, p.adviceB, p.otherAdvice].filter(Boolean).join(' ').trim(),
    updated: p.lastUpdated ? new Date(p.lastUpdated).toISOString() : null,
    coord,
  };
}

const toDate = (s) => (s ? new Date(s) : null);

function summariseLeg(leg) {
  const cls = leg.transportation?.product?.class;
  const walk = WALK_CLASSES.has(cls) || (!leg.transportation && leg.footPathInfo);
  const depPlanned = toDate(leg.origin?.departureTimePlanned);
  const depEstimated = toDate(leg.origin?.departureTimeEstimated);
  const arrPlanned = toDate(leg.destination?.arrivalTimePlanned);
  const arrEstimated = toDate(leg.destination?.arrivalTimeEstimated);
  const status = leg.realtimeStatus ?? [];
  return {
    mode: walk ? 'Walk' : PRODUCT_CLASSES[cls] ?? leg.transportation?.product?.name ?? 'Transit',
    line: walk ? null : leg.transportation?.disassembledName || leg.transportation?.number || null,
    towards: walk ? null : leg.transportation?.destination?.name ?? null,
    from: leg.origin?.name ?? '',
    fromPlatform: leg.origin?.disassembledName ?? null,
    to: leg.destination?.name ?? '',
    depart: depEstimated ?? depPlanned,
    arrive: arrEstimated ?? arrPlanned,
    delayMinutes: depEstimated && depPlanned ? Math.round(minutesBetween(depPlanned, depEstimated)) : null,
    realtime: leg.isRealtimeControlled === true,
    cancelled: status.includes('CANCELLED'),
    durationMinutes: leg.duration != null ? Math.round(leg.duration / 60) : null,
    alerts: [...new Set((leg.infos ?? []).map((i) => i.subtitle || i.title).filter(Boolean))],
  };
}

/** Condenses a rapidJSON journey into what the app displays. */
export function summariseJourney(journey) {
  const legs = (journey.legs ?? []).map(summariseLeg);
  if (legs.length === 0) return null;
  const transit = legs.filter((l) => l.mode !== 'Walk');
  // Walking legs often carry no times; derive them from the neighbouring transit legs.
  let depart = legs[0].depart;
  let arrive = legs.at(-1).arrive;
  if (!depart && transit[0]?.depart) depart = new Date(transit[0].depart.getTime() - (legs[0].durationMinutes ?? 0) * 60000);
  if (!arrive && transit.at(-1)?.arrive) arrive = new Date(transit.at(-1).arrive.getTime() + (legs.at(-1).durationMinutes ?? 0) * 60000);
  if (!depart || !arrive) return null;
  const delays = transit.map((l) => l.delayMinutes).filter((d) => d != null);
  return {
    depart,
    arrive,
    durationMinutes: Math.round(minutesBetween(depart, arrive)),
    interchanges: Math.max(0, transit.length - 1),
    realtime: transit.some((l) => l.realtime),
    maxDelayMinutes: delays.length ? Math.max(...delays) : null,
    cancelled: transit.some((l) => l.cancelled),
    alerts: [...new Set(transit.flatMap((l) => l.alerts))].slice(0, 5),
    legs,
  };
}
