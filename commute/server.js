import http from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, join, normalize } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadConfig, createServices } from './lib/config.js';
import { createRoutes } from './lib/routes.js';

const PUBLIC_DIR = fileURLToPath(new URL('./public/', import.meta.url));
const LIB_DIR = fileURLToPath(new URL('./lib/', import.meta.url));
const MIME = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8', '.svg': 'image/svg+xml' };

const sendJson = (res, status, body) => {
  res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store' });
  res.end(JSON.stringify(body));
};

export function createApp(config, services = createServices(config)) {
  const routes = createRoutes(config, services);

  return async (req, res) => {
    const url = new URL(req.url, 'http://localhost');
    try {
      const route = routes[url.pathname];
      if (route) return sendJson(res, 200, await route(url.searchParams));
      if (url.pathname.startsWith('/api/')) return sendJson(res, 404, { error: 'Not found' });

      const rel = normalize(url.pathname === '/' ? 'index.html' : url.pathname.slice(1));
      if (rel.startsWith('..')) return sendJson(res, 400, { error: 'Bad path' });
      // Shared planner modules, for trying the Android app's standalone mode in a browser.
      const isLib = rel.startsWith('lib/') && extname(rel) === '.js' && rel !== 'lib/config.js';
      const body = await readFile(isLib ? join(LIB_DIR, rel.slice(4)) : join(PUBLIC_DIR, rel));
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
