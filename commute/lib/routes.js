// Request handlers shared by the Node server and the standalone (Android) app.

import { planCommute } from './commute.js';
import { parseSydneyDateTime } from './time.js';
import { parseStationList } from './parkride.js';

/** @returns {Record<string, (q: URLSearchParams) => Promise<unknown>>} keyed by API path */
export function createRoutes(config, { client, getHazards, driveOptions }) {
  return {
    '/api/config': async () => ({
      demo: config.demo,
      home: config.home,
      work: config.work,
      via: config.via.join('; '),
      parkMinutes: config.parkMinutes,
      drivingSource: config.googleApiKey ? 'google' : 'model',
      hasApiKey: Boolean(config.tfnswApiKey),
    }),

    '/api/locations': async (q) => {
      const query = (q.get('q') ?? '').trim();
      return query.length < 2 ? [] : client.findLocations(query);
    },

    '/api/commute': async (q) => {
      const arriveBy = q.get('mode') === 'arrive';
      const park = Number(q.get('park') ?? config.parkMinutes);
      if (!Number.isFinite(park) || park < 0 || park > 60) throw new Error('Park time must be between 0 and 60 minutes');
      const when = q.get('time') ? parseSydneyDateTime(q.get('date'), q.get('time')) : new Date();
      const result = await planCommute({
        client,
        from: q.get('from') || config.home,
        to: q.get('to') || config.work,
        when,
        arriveBy,
        railOnly: q.get('railOnly') === '1',
        via: parseStationList(q.has('via') ? q.get('via') : config.via),
        parkMinutes: park,
        parkAt: q.get('parkAt') === 'end' ? 'end' : 'start',
        getHazards,
        driveOptions,
      });
      if (result.drive) delete result.drive.geometry;
      return result;
    },
  };
}
