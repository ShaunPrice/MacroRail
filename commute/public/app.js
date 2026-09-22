const $ = (id) => document.getElementById(id);
const TZ = 'Australia/Sydney';
const REFRESH_MS = 60_000;
const timeFmt = new Intl.DateTimeFormat('en-AU', { timeZone: TZ, hour: '2-digit', minute: '2-digit', hourCycle: 'h23' });
const fmt = (iso) => timeFmt.format(new Date(iso));
const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);

const store = {
  get(k) { try { return localStorage.getItem(k); } catch { return null; } },
  set(k, v) { try { localStorage.setItem(k, v); } catch { /* storage unavailable */ } },
};

let refreshTimer = null;
let requestSeq = 0;

function sydneyToday() {
  const p = Object.fromEntries(new Intl.DateTimeFormat('en-AU', { timeZone: TZ, year: 'numeric', month: '2-digit', day: '2-digit' })
    .formatToParts(new Date()).map((x) => [x.type, x.value]));
  return `${p.year}-${p.month}-${p.day}`;
}

function syncWhenFields() {
  const now = $('mode').value === 'now';
  $('time').hidden = now;
  $('date').hidden = now;
  $('time').required = !now;
  if (!now && !$('time').value) $('time').value = $('mode').value === 'arrive' ? '09:00' : '08:00';
  if (!$('date').value) $('date').value = sydneyToday();
}

async function api(path, params) {
  const res = await fetch(`${path}?${new URLSearchParams(params)}`);
  const body = await res.json();
  if (!res.ok) throw new Error(body.error ?? `HTTP ${res.status}`);
  return body;
}

function debounce(fn, ms) {
  let t;
  return (...a) => { clearTimeout(t); t = setTimeout(() => fn(...a), ms); };
}

function attachSuggestions(input, list) {
  input.addEventListener('input', debounce(async () => {
    const q = input.value.trim();
    if (q.length < 3) return;
    try {
      const items = await api('/api/locations', { q });
      list.innerHTML = items.map((l) => `<option value="${esc(l.name)}">${esc(l.type ?? '')}</option>`).join('');
    } catch { /* suggestions are optional */ }
  }, 300));
}

function renderDrive(d, error) {
  if (!d) return `<p class="status error">Unavailable: ${esc(error ?? 'unknown error')}</p>`;
  const model = d.source !== 'google';
  const rows = [
    ['Leave', fmt(d.depart)],
    ['Arrive', fmt(d.arrive)],
    ['Distance', `${d.distanceKm} km`],
    ...(model
      ? [['Free-flow', `${d.freeFlowMinutes} min`], ['Congestion', `× ${d.congestionFactor}`], ['Hazards', `+${d.incidentDelayMinutes} min`]]
      : [['No traffic', `${d.freeFlowMinutes} min`]]),
  ];
  const hazards = d.hazards.length
    ? `<ul class="hazards">${d.hazards.map((h) => `
        <li class="${h.isMajor ? 'major' : ''}">
          <strong>${esc(h.headline)}</strong>${h.delayMinutes ? ` <span class="badge warn">+${h.delayMinutes} min</span>` : ''}
          <div class="sub">${esc([h.road, h.suburb].filter(Boolean).join(', '))}${h.advice ? ` · ${esc(h.advice)}` : ''}</div>
        </li>`).join('')}</ul>`
    : '<p class="note">No live traffic hazards reported on this route.</p>';
  return `
    <div class="metric">${d.totalMinutes} <small>min</small></div>
    <dl class="kv">${rows.map(([k, v]) => `<dt>${k}</dt><dd>${esc(v)}</dd>`).join('')}</dl>
    ${hazards}
    <p class="note">${model
      ? 'Model estimate: routed free-flow time × Sydney time-of-day congestion profile + allowance for live hazards on the route.'
      : 'Traffic-aware estimate from Google Routes; hazards shown for information.'}</p>`;
}

function statusBadge(j) {
  if (j.cancelled) return '<span class="badge bad">Cancelled</span>';
  if (j.maxDelayMinutes > 0) return `<span class="badge warn">${j.maxDelayMinutes} min late</span>`;
  if (j.maxDelayMinutes < 0) return `<span class="badge good">${-j.maxDelayMinutes} min early</span>`;
  if (j.realtime) return '<span class="badge good">On time</span>';
  return '<span class="badge">Timetabled</span>';
}

function renderTrips(trips, error, bestIndex) {
  if (!trips.length) return `<li class="status ${error ? 'error' : ''}">${esc(error ?? 'No journeys found.')}</li>`;
  return trips.map((j, i) => {
    const legs = j.legs.map((l) => l.mode === 'Walk'
      ? `<span class="leg Walk">Walk ${l.durationMinutes ?? ''}${l.durationMinutes != null ? 'm' : ''}</span>`
      : `<span class="leg ${esc(l.mode)}" title="${esc(`${l.mode} towards ${l.towards ?? ''}`)}">${esc(l.line ?? l.mode)}</span>`).join('');
    const first = j.legs.find((l) => l.mode !== 'Walk');
    return `
      <li class="${i === bestIndex ? 'best' : ''} ${j.cancelled ? 'cancelled' : ''}">
        <div class="trip-head">
          <span class="times">${fmt(j.depart)} → ${fmt(j.arrive)}</span>
          <span class="dur">${j.durationMinutes} min</span>
        </div>
        <div class="legs">${legs}</div>
        <div class="trip-meta">
          ${statusBadge(j)}
          ${first ? ` ${esc(first.from)} ${fmt(first.depart)}` : ''}
          ${j.interchanges ? ` · ${j.interchanges} change${j.interchanges > 1 ? 's' : ''}` : ''}
        </div>
        ${j.alerts.map((a) => `<div class="alert">⚠ ${esc(a)}</div>`).join('')}
      </li>`;
  }).join('');
}

function render(r) {
  const rec = r.recommendation;
  $('recommendation').innerHTML = rec
    ? `<span class="big">${esc(rec.label)}: ${rec.minutes} min</span>
       <span>Leave ${fmt(rec.depart)}, arrive ${fmt(rec.arrive)}</span>
       <span class="muted">${esc(rec.reason)} ${esc(r.origin.name)} → ${esc(r.destination.name)}</span>`
    : '<span class="big">No estimate available</span>';
  $('drive').innerHTML = renderDrive(r.drive, r.errors.drive);
  $('trips').innerHTML = renderTrips(r.trips, r.errors.transit, rec?.kind === 'transit' ? rec.index : -1);
  $('results').hidden = false;
  const notes = [`Updated ${fmt(r.generatedAt)}`];
  if (r.errors.traffic) notes.push(`live traffic unavailable (${r.errors.traffic})`);
  $('status').className = 'status';
  $('status').textContent = notes.join(' · ');
}

async function estimate() {
  const mode = $('mode').value;
  const params = { from: $('from').value, to: $('to').value, mode: mode === 'arrive' ? 'arrive' : 'depart' };
  if (mode !== 'now') Object.assign(params, { time: $('time').value, date: $('date').value });
  if ($('rail').checked) params.railOnly = '1';
  store.set('from', params.from);
  store.set('to', params.to);

  const seq = ++requestSeq;
  clearTimeout(refreshTimer);
  $('status').className = 'status';
  $('status').textContent = 'Fetching live traffic and timetables…';
  try {
    const result = await api('/api/commute', params);
    if (seq === requestSeq) render(result);
  } catch (err) {
    if (seq !== requestSeq) return;
    $('status').className = 'status error';
    $('status').textContent = err.message;
  }
  if (seq === requestSeq) scheduleRefresh();
}

function scheduleRefresh() {
  clearTimeout(refreshTimer);
  if ($('auto').checked && !$('results').hidden) refreshTimer = setTimeout(estimate, REFRESH_MS);
}

async function init() {
  const cfg = await api('/api/config', {}).catch(() => ({}));
  $('demo-badge').hidden = !cfg.demo;
  $('from').value = store.get('from') || cfg.home || '';
  $('to').value = store.get('to') || cfg.work || '';
  if (!cfg.hasApiKey) {
    $('status').className = 'status error';
    $('status').textContent = 'TFNSW_API_KEY is not configured on the server. See README.';
  }
  attachSuggestions($('from'), $('from-list'));
  attachSuggestions($('to'), $('to-list'));
  $('mode').addEventListener('change', syncWhenFields);
  $('auto').addEventListener('change', scheduleRefresh);
  $('swap').addEventListener('click', () => {
    [$('from').value, $('to').value] = [$('to').value, $('from').value];
  });
  $('form').addEventListener('submit', (e) => { e.preventDefault(); estimate(); });
  syncWhenFields();
  if ($('from').value && $('to').value && cfg.hasApiKey) estimate();
}

init();
