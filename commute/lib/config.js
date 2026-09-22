import { fileURLToPath } from 'node:url';
import { TfnswClient } from './tfnsw.js';
import { createDemoFetch } from './demo.js';
import { OSRM_DEFAULT_URL } from './driving.js';

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
    home: env.HOME_ADDRESS || (demo ? 'Parramatta Station' : ''),
    work: env.WORK_ADDRESS || (demo ? 'Wynyard Station' : ''),
    hazardCacheSeconds: Number(env.HAZARD_CACHE_SECONDS ?? 60),
  };
}

/** Builds the API client, hazard cache and driving options from config. */
export function createServices(config, fetchImpl = config.demo ? createDemoFetch() : globalThis.fetch) {
  const client = new TfnswClient({ apiKey: config.tfnswApiKey, fetchImpl });
  let cache = { at: 0, promise: null };
  const getHazards = () => {
    if (!cache.promise || Date.now() - cache.at > config.hazardCacheSeconds * 1000) {
      const promise = client.openHazards();
      promise.catch(() => {
        if (cache.promise === promise) cache = { at: 0, promise: null };
      });
      cache = { at: Date.now(), promise };
    }
    return cache.promise;
  };
  return {
    client,
    getHazards,
    driveOptions: { googleApiKey: config.googleApiKey, osrmUrl: config.osrmUrl, fetchImpl },
  };
}
