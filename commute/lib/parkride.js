// Park and ride: drive from the origin to a station, then continue by public
// transport (the Trip Planner handles any changes between trains, metro,
// light rail, buses and ferries).

import { getDriveRoute, timeDrive } from './driving.js';
import { minutesBetween } from './time.js';

const MINUTE = 60000;
const ceilMinute = (d) => new Date(Math.ceil(d.getTime() / MINUTE) * MINUTE);
const floorMinute = (d) => new Date(Math.floor(d.getTime() / MINUTE) * MINUTE);

/** Splits "Epping Station; Hornsby Station" (or newline separated) into names. */
export function parseStationList(text) {
  const list = Array.isArray(text) ? text : String(text ?? '').split(/[;\n]/);
  return list.map((s) => s.trim()).filter(Boolean);
}

/**
 * For one service from the station, the latest time to leave the origin and still catch it.
 * @returns combined option, or null if leaving in time is no longer possible
 */
function optionForTrip({ route, trip, station, parkMinutes, hazards, model, notBefore }) {
  // A late-running service can make up time, so plan to reach it by its timetabled departure.
  const lateBy = Math.max(0, trip.legs.find((l) => l.mode !== 'Walk')?.delayMinutes ?? 0);
  const catchBy = new Date(trip.depart.getTime() - lateBy * MINUTE);
  const atStation = new Date(catchBy.getTime() - parkMinutes * MINUTE);
  const drive = timeDrive(route, { when: atStation, arriveBy: true, hazards, ...model });
  const leave = floorMinute(drive.depart);
  if (notBefore && leave < notBefore) return null;
  return {
    station: station.name,
    depart: leave,
    arrive: trip.arrive,
    totalMinutes: Math.round(minutesBetween(leave, trip.arrive)),
    parkMinutes,
    catchBy,
    drive: { ...drive, depart: leave },
    trip,
    cancelled: trip.cancelled,
  };
}

/** Return trip: public transport to the station, then drive from the car park. */
function returnOption({ route, trip, station, parkMinutes, hazards, model }) {
  const atCar = new Date(trip.arrive.getTime() + parkMinutes * MINUTE);
  const drive = timeDrive(route, { when: atCar, hazards, ...model });
  const arrive = ceilMinute(drive.arrive);
  return {
    station: station.name,
    depart: trip.depart,
    arrive,
    totalMinutes: Math.round(minutesBetween(trip.depart, arrive)),
    parkMinutes,
    catchBy: null,
    drive: { ...drive, arrive },
    trip,
    cancelled: trip.cancelled,
  };
}

/**
 * @param {object} o
 * @param {import('./tfnsw.js').TfnswClient} o.client
 * @param {{coord: number[]}} o.origin
 * @param {{coord?: number[], id?: string}} o.destination
 * @param {{name: string, coord: number[], id?: string}} o.station resolved station
 * @param {'start'|'end'} o.parkAt 'start': drive then public transport; 'end': public transport then drive
 * @param {Date} o.when departure time, or arrival deadline when arriveBy
 * @param {number} o.parkMinutes time between the car and the platform
 */
export async function planStation({
  client, origin, destination, station, parkAt = 'start', when, arriveBy = false, railOnly = false,
  parkMinutes = 5, hazards = [], driveOptions = {}, optionsPerStation = 3,
}) {
  const { googleApiKey, osrmUrl, fetchImpl, ...model } = driveOptions;
  const routeOpts = { googleApiKey, osrmUrl, fetchImpl, departAt: arriveBy ? null : when };

  if (parkAt === 'end') {
    const route = await getDriveRoute(station.coord, destination.coord, routeOpts);
    // Arrive-by: the last train must reach the station in time to walk to the car and drive the rest.
    const searchAt = arriveBy
      ? floorMinute(new Date(timeDrive(route, { when, arriveBy: true, hazards, ...model }).depart.getTime() - parkMinutes * MINUTE))
      : when;
    const trips = await client.planTrips({ from: origin, to: station, when: searchAt, arriveBy, railOnly, count: optionsPerStation + 2 });
    const options = trips
      .map((trip) => returnOption({ route, trip, station, parkMinutes, hazards, model }))
      .filter((o) => arriveBy || o.depart >= floorMinute(new Date(when.getTime() - MINUTE)))
      .sort((a, b) => a.depart - b.depart);
    return arriveBy ? options.slice(-optionsPerStation) : options.slice(0, optionsPerStation);
  }

  const route = await getDriveRoute(origin.coord, station.coord, routeOpts);
  // Depart mode: search from the earliest moment we could be on the platform.
  const searchFrom = arriveBy
    ? when
    : ceilMinute(new Date(timeDrive(route, { when, hazards, ...model }).arrive.getTime() + parkMinutes * MINUTE));
  const trips = await client.planTrips({
    from: station,
    to: destination,
    when: searchFrom,
    arriveBy,
    railOnly,
    count: optionsPerStation + 2,
  });

  const notBefore = arriveBy ? null : floorMinute(new Date(when.getTime() - MINUTE));
  const options = trips
    .map((trip) => optionForTrip({ route, trip, station, parkMinutes, hazards, model, notBefore }))
    .filter(Boolean)
    .sort((a, b) => a.depart - b.depart);
  return arriveBy ? options.slice(-optionsPerStation) : options.slice(0, optionsPerStation);
}

/** Plans every candidate station; a failure at one station does not affect the others. */
export async function planParkAndRide({ stations, resolve, ...rest }) {
  return Promise.all(
    stations.map(async (name) => {
      try {
        const station = await resolve(name);
        return { station, options: await planStation({ ...rest, station }), error: null };
      } catch (err) {
        return { station: { name }, options: [], error: err.message };
      }
    }),
  );
}
