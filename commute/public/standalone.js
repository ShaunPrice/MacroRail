// Standalone mode (Android app): runs the planner in the page instead of calling
// the Node server. Settings, including API keys, stay in this device's storage.
//
// In the Android APK the lib/ modules are packaged next to this file. The Node
// server also serves them at /lib/ so standalone mode can be tried in a browser
// with ?standalone=1.

import { createRoutes } from './lib/routes.js';
import { createServices } from './lib/services.js';
import { parseStationList } from './lib/parkride.js';
import { OSRM_DEFAULT_URL } from './lib/driving.js';
import { createDemoFetch } from './lib/demo.js';

const SETTINGS_KEY = 'nsw-commute-settings';
const DEFAULTS = { tfnswApiKey: '', googleApiKey: '', home: '', work: '', via: '', parkMinutes: 5, demo: false };

export function loadSettings() {
  try {
    return { ...DEFAULTS, ...JSON.parse(localStorage.getItem(SETTINGS_KEY) || '{}') };
  } catch {
    return { ...DEFAULTS };
  }
}

export function saveSettings(settings) {
  const clean = { ...DEFAULTS, ...settings };
  clean.parkMinutes = Math.min(60, Math.max(0, Number(clean.parkMinutes) || 0));
  try {
    localStorage.setItem(SETTINGS_KEY, JSON.stringify(clean));
  } catch {
    // Storage unavailable: settings apply to this session only.
  }
  routes = null;
  return clean;
}

/** fetch() over the Android bridge, which is not subject to browser CORS rules. */
function nativeFetch() {
  const bridge = window.NativeHttp;
  if (!bridge) return globalThis.fetch.bind(globalThis);
  const pending = new Map();
  let seq = 0;
  window.__nativeHttp = {
    resolve(id, status, body, contentType) {
      const p = pending.get(id);
      if (!p) return;
      pending.delete(id);
      p.resolve(new Response(status === 204 ? null : body, { status, headers: { 'Content-Type': contentType || 'application/json' } }));
    },
    reject(id, message) {
      const p = pending.get(id);
      if (!p) return;
      pending.delete(id);
      p.reject(new TypeError(message));
    },
  };
  return (input, init = {}) =>
    new Promise((resolve, reject) => {
      const id = String(++seq);
      pending.set(id, { resolve, reject });
      bridge.request(id, init.method || 'GET', String(input), JSON.stringify(init.headers || {}), init.body == null ? '' : String(init.body));
    });
}

let routes = null;
let fetchImpl = null;

function getRoutes() {
  if (routes) return routes;
  const s = loadSettings();
  const config = {
    demo: s.demo,
    tfnswApiKey: s.demo ? 'demo' : s.tfnswApiKey.trim(),
    googleApiKey: s.demo ? undefined : s.googleApiKey.trim() || undefined,
    osrmUrl: OSRM_DEFAULT_URL,
    home: s.home || (s.demo ? 'Cherrybrook' : ''),
    work: s.work || (s.demo ? 'Barangaroo' : ''),
    via: parseStationList(s.via || (s.demo ? 'Epping Station; Macquarie University Station' : '')),
    parkMinutes: Number(s.parkMinutes ?? 5),
    hazardCacheSeconds: 60,
  };
  fetchImpl ??= nativeFetch();
  routes = createRoutes(config, createServices(config, config.demo ? createDemoFetch() : fetchImpl));
  return routes;
}

/** Same contract as the server's JSON API: resolves to plain JSON, rejects with a message. */
export async function call(path, params = {}) {
  const route = getRoutes()[path];
  if (!route) throw new Error(`Unknown API path ${path}`);
  const result = await route(new URLSearchParams(params));
  return JSON.parse(JSON.stringify(result));
}
