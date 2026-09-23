// Builds the API client, hazard cache and driving options.
// Browser-safe: used by the Node server and by the Android app's WebView.

import { TfnswClient } from './tfnsw.js';
import { createDemoFetch } from './demo.js';

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
