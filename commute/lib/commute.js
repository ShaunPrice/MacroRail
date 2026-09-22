// Combines driving and public transport estimates into a single commute answer.

import { parseLatLon } from './geo.js';
import { estimateDrive } from './driving.js';
import { minutesBetween } from './time.js';
import { planParkAndRide } from './parkride.js';

/** Resolves free text, a stop id or "lat,lon" into { name, coord, id }. */
export async function resolveLocation(client, input) {
  const text = String(input ?? '').trim();
  if (!text) throw new Error('A start and destination are required');
  const coord = parseLatLon(text);
  if (coord) return { name: text, coord, id: null };
  const [best] = await client.findLocations(text, 1);
  if (!best) throw new Error(`Could not find a location matching "${text}"`);
  return { name: best.name, coord: best.coord, id: best.id };
}

/**
 * Picks the best option across driving, public transport and park and ride.
 * Depart mode: earliest arrival. Arrive-by mode: latest departure that still arrives on time.
 * Cancelled services are never recommended.
 */
export function recommend({ drive, trips = [], parkRide = [], when, arriveBy }) {
  const options = [];
  if (drive) options.push({ kind: 'drive', label: 'Drive', depart: drive.depart, arrive: drive.arrive, minutes: drive.totalMinutes });
  trips.forEach((t, index) => {
    if (!t.cancelled) options.push({ kind: 'transit', label: 'Public transport', index, depart: t.depart, arrive: t.arrive, minutes: t.durationMinutes });
  });
  parkRide.forEach((s, stationIndex) =>
    s.options.forEach((o, index) => {
      if (!o.cancelled) {
        options.push({
          kind: 'parkride',
          label: o.catchBy ? `Drive to ${shortName(s.station.name)} + public transport` : `Public transport to ${shortName(s.station.name)} + drive`,
          stationIndex, index, depart: o.depart, arrive: o.arrive, minutes: o.totalMinutes,
        });
      }
    }),
  );
  if (options.length === 0) return null;

  let pick;
  if (arriveBy) {
    const onTime = options.filter((o) => o.arrive <= when);
    pick = onTime.length
      ? onTime.reduce((a, b) => (b.depart > a.depart || (+b.depart === +a.depart && b.minutes < a.minutes) ? b : a))
      : options.reduce((a, b) => (b.arrive < a.arrive ? b : a));
  } else {
    const upcoming = options.filter((o) => o.depart >= new Date(when.getTime() - 60000));
    pick = (upcoming.length ? upcoming : options).reduce((a, b) =>
      b.arrive < a.arrive || (+b.arrive === +a.arrive && b.minutes < a.minutes) ? b : a,
    );
  }

  const late = arriveBy && pick.arrive > when ? Math.ceil(minutesBetween(when, pick.arrive)) : 0;
  const reason = arriveBy
    ? late
      ? `No option arrives on time; this is the earliest arrival (${late} min late).`
      : 'Latest departure that still arrives on time.'
    : 'Earliest arrival.';
  return { ...pick, reason };
}

const shortName = (name) => name.split(',')[0];

/**
 * @param {object} o
 * @param {import('./tfnsw.js').TfnswClient} o.client
 * @param {string} o.from
 * @param {string} o.to
 * @param {Date} o.when
 * @param {boolean} o.arriveBy
 * @param {boolean} o.railOnly
 * @param {string[]} o.via stations to drive to for park and ride (optional)
 * @param {'start'|'end'} o.parkAt whether the car is at the start (drive first) or the destination end
 * @param {number} o.parkMinutes time between the car and the platform
 * @param {() => Promise<Array>} o.getHazards cached hazard loader
 */
export async function planCommute({
  client, from, to, when, arriveBy = false, railOnly = false, via = [], parkAt = 'start', parkMinutes = 5, getHazards, driveOptions = {},
}) {
  const [origin, destination] = await Promise.all([resolveLocation(client, from), resolveLocation(client, to)]);

  const hazardsP = getHazards();
  const tripsP = client.planTrips({ from: origin, to: destination, when, arriveBy, railOnly });
  const driveP = hazardsP
    .catch(() => [])
    .then((hazards) => estimateDrive({ from: origin.coord, to: destination.coord, when, arriveBy, hazards, ...driveOptions }));

  const parkRideP = via.length
    ? hazardsP.catch(() => []).then((hazards) =>
        planParkAndRide({
          stations: via,
          resolve: (name) => resolveLocation(client, name),
          client, origin, destination, parkAt, when, arriveBy, railOnly, parkMinutes, hazards, driveOptions,
        }))
    : Promise.resolve([]);

  const [hazardsR, tripsR, driveR, parkRideR] = await Promise.allSettled([hazardsP, tripsP, driveP, parkRideP]);
  const errors = {};
  if (hazardsR.status === 'rejected') errors.traffic = hazardsR.reason.message;
  if (tripsR.status === 'rejected') errors.transit = tripsR.reason.message;
  if (driveR.status === 'rejected') errors.drive = driveR.reason.message;

  const trips = tripsR.status === 'fulfilled' ? tripsR.value : [];
  const drive = driveR.status === 'fulfilled' ? driveR.value : null;
  const parkRide = parkRideR.status === 'fulfilled' ? parkRideR.value : [];
  if (parkRideR.status === 'rejected') errors.parkRide = parkRideR.reason.message;

  return {
    generatedAt: new Date(),
    query: { when, arriveBy, railOnly, via, parkAt, parkMinutes },
    origin,
    destination,
    drive,
    trips,
    parkRide,
    recommendation: recommend({ drive, trips, parkRide, when, arriveBy }),
    errors,
  };
}
