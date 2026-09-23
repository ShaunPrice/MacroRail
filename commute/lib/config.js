import { fileURLToPath } from 'node:url';
import { OSRM_DEFAULT_URL } from './driving.js';
import { parseStationList } from './parkride.js';

export function loadConfig(env = process.env) {
  if (env === process.env) {
    try {
      process.loadEnvFile(fileURLToPath(new URL('../.env', import.meta.url)));
    } catch {
      // No .env file: rely on the process environment.
    }
  }
  const demo = env.DEMO === '1' || env.DEMO === 'true';
  return {
    demo,
    port: Number(env.PORT ?? 3000),
    tfnswApiKey: demo ? 'demo' : env.TFNSW_API_KEY,
    googleApiKey: demo ? undefined : env.GOOGLE_MAPS_API_KEY || undefined,
    osrmUrl: env.OSRM_URL || OSRM_DEFAULT_URL,
    home: env.HOME_ADDRESS || (demo ? 'Cherrybrook' : ''),
    work: env.WORK_ADDRESS || (demo ? 'Barangaroo' : ''),
    via: parseStationList(env.PARK_AND_RIDE_STATIONS || (demo ? 'Epping Station; Macquarie University Station' : '')),
    parkMinutes: Number(env.PARK_MINUTES ?? 5),
    hazardCacheSeconds: Number(env.HAZARD_CACHE_SECONDS ?? 60),
  };
}

// Re-exported for existing callers; the implementation is browser-safe in services.js.
export { createServices } from './services.js';
