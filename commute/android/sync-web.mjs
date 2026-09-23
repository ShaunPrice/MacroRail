// Copies the web app (public/) and the planner modules (lib/) into the APK's assets.
// Run before every Android build: node android/sync-web.mjs
import { cpSync, rmSync, mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('..', import.meta.url));
const out = fileURLToPath(new URL('./app/src/main/assets/www/', import.meta.url));

rmSync(out, { recursive: true, force: true });
mkdirSync(out, { recursive: true });
cpSync(`${root}public`, out, { recursive: true });
// config.js reads the server's .env file and is not used in the app.
cpSync(`${root}lib`, `${out}lib`, { recursive: true, filter: (src) => !src.endsWith('config.js') });
console.log(`Web assets copied to ${out}`);
