import http from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, join, normalize } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadConfig, createServices } from './lib/config.js';
import { planCommute } from './lib/commute.js';
import { parseSydneyDateTime } from './lib/time.js';

const PUBLIC_DIR = fileURLToPath(new URL('./public/', import.meta.url));
const MIME = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8', '.svg': 'image/svg+xml' };

const sendJson = (res, status, body) => {
  res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store' });
  res.end(JSON.stringify(body));
};

export function createApp(config, services = createServices(config)) {
  const { client, getHazards, driveOptions } = services;

  const routes = {
    '/api/config': async () => ({
      demo: config.demo,
      home: config.home,
      work: config.work,
      drivingSource: config.googleApiKey ? 'google' : 'model',
      hasApiKey: Boolean(config.tfnswApiKey),
    }),

    '/api/locations': async (q) => {
      const query = (q.get('q') ?? '').trim();
      return query.length < 2 ? [] : client.findLocations(query);
    },

    '/api/commute': async (q) => {
      const arriveBy = q.get('mode') === 'arrive';
      const when = q.get('time') ? parseSydneyDateTime(q.get('date'), q.get('time')) : new Date();
      const result = await planCommute({
        client,
        from: q.get('from') || config.home,
        to: q.get('to') || config.work,
        when,
        arriveBy,
        railOnly: q.get('railOnly') === '1',
        getHazards,
        driveOptions,
      });
      if (result.drive) delete result.drive.geometry;
      return result;
    },
  };

  return async (req, res) => {
    const url = new URL(req.url, 'http://localhost');
    try {
      const route = routes[url.pathname];
      if (route) return sendJson(res, 200, await route(url.searchParams));
      if (url.pathname.startsWith('/api/')) return sendJson(res, 404, { error: 'Not found' });

      const rel = normalize(url.pathname === '/' ? 'index.html' : url.pathname.slice(1));
      if (rel.startsWith('..')) return sendJson(res, 400, { error: 'Bad path' });
      const body = await readFile(join(PUBLIC_DIR, rel));
      res.writeHead(200, { 'Content-Type': MIME[extname(rel)] ?? 'application/octet-stream' });
      res.end(body);
    } catch (err) {
      if (err.code === 'ENOENT') return sendJson(res, 404, { error: 'Not found' });
      sendJson(res, 400, { error: err.message });
    }
  };
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const config = loadConfig();
  if (!config.tfnswApiKey) {
    console.warn('Warning: TFNSW_API_KEY is not set. Copy .env.example to .env, or run `npm run demo` for simulated data.');
  }
  http.createServer(createApp(config)).listen(config.port, () => {
    console.log(`NSW Commute running at http://localhost:${config.port}${config.demo ? ' (DEMO: simulated data)' : ''}`);
  });
}
