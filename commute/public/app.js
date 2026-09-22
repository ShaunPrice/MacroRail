const $ = (id) => document.getElementById(id);
const TZ = 'Australia/Sydney';
const REFRESH_MS = 60_000;
const MIN = 60_000;

const timeFmt = new Intl.DateTimeFormat('en-AU', { timeZone: TZ, hour: '2-digit', minute: '2-digit', hourCycle: 'h23' });
const fmt = (d) => timeFmt.format(new Date(d));
const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
const icon = (name, cls = '') => `<svg class="${cls}" aria-hidden="true"><use href="#i-${name}"/></svg>`;
const short = (name) => String(name ?? '').split(',')[0];
const plural = (n, word) => `${n} ${word}${n === 1 ? '' : 's'}`;

const store = {
  get(k) { try { return localStorage.getItem(k); } catch { return null; } },
  set(k, v) { try { localStorage.setItem(k, v); } catch { /* storage unavailable */ } },
};

/* ---------- Modes ---------- */

const MODE_KEYS = { Train: 'train', Metro: 'metro', 'Light Rail': 'lightrail', Bus: 'bus', 'School Bus': 'bus', Coach: 'coach', Ferry: 'ferry', Walk: 'walk', Cycle: 'walk', Drive: 'drive', Park: 'park' };
const MODE_ICONS = { train: 'train', metro: 'metro', lightrail: 'lightrail', bus: 'bus', coach: 'bus', ferry: 'ferry', walk: 'walk', drive: 'drive', park: 'park', transit: 'bus' };
const MODE_NAMES = { drive: 'Car', train: 'Train', metro: 'Metro', lightrail: 'Light rail', bus: 'Bus', ferry: 'Ferry', coach: 'Coach', walk: 'Walk', park: 'Park' };
const modeKey = (mode) => MODE_KEYS[mode] ?? 'transit';
const colour = (key) => `var(--m-${key === 'transit' ? 'bus' : key})`;

/** Time-positioned segments for a public transport journey, with waits between legs. */
function journeySegments(j) {
  const segs = [];
  let cursor = new Date(j.depart).getTime();
  for (const l of j.legs) {
    const key = modeKey(l.mode);
    const dur = (l.durationMinutes ?? 0) * MIN;
    let start = l.depart ? new Date(l.depart).getTime() : cursor;
    if (!l.depart && key === 'walk' && l === j.legs.at(-1)) start = cursor;
    if (start - cursor >= MIN) segs.push({ key: 'wait', start: cursor, end: start });
    const end = l.arrive ? new Date(l.arrive).getTime() : start + dur;
    segs.push({ key, start, end, leg: l, cancelled: l.cancelled });
    cursor = Math.max(cursor, end);
  }
  return segs;
}

function parkRideSegments(o, parkAt) {
  const d0 = new Date(o.drive.depart).getTime();
  const d1 = new Date(o.drive.arrive).getTime();
  const drive = { key: 'drive', start: d0, end: d1, drive: o.drive, station: o.station };
  const trip = journeySegments(o.trip);
  if (parkAt === 'end') {
    const tEnd = trip.at(-1).end;
    const park = { key: 'park', start: tEnd, end: tEnd + o.parkMinutes * MIN, parkMinutes: o.parkMinutes, toCar: true };
    const wait = d0 - park.end >= MIN ? [{ key: 'wait', start: park.end, end: d0 }] : [];
    return [...trip, park, ...wait, drive];
  }
  const park = { key: 'park', start: d1, end: d1 + o.parkMinutes * MIN, parkMinutes: o.parkMinutes };
  const tStart = trip[0].start;
  const wait = tStart - park.end >= MIN ? [{ key: 'wait', start: park.end, end: tStart }] : [];
  return [drive, park, ...wait, ...trip];
}

const driveSegments = (d) => [{ key: 'drive', start: new Date(d.depart).getTime(), end: new Date(d.arrive).getTime(), drive: d }];
const minutesOf = (s) => Math.max(0, Math.round((s.end - s.start) / MIN));

function segLabel(s) {
  if (s.key === 'drive') return `Drive ${minutesOf(s)} min`;
  if (s.key === 'park') return s.toCar ? `Walk to car ${minutesOf(s)} min` : `Park and walk ${minutesOf(s)} min`;
  if (s.key === 'walk') return `Walk ${minutesOf(s)} min`;
  if (s.key === 'wait') return `Wait ${minutesOf(s)} min`;
  return `${MODE_NAMES[s.key] ?? s.leg.mode} ${s.leg.line ?? ''}`.trim();
}

function segTooltip(s) {
  const span = `${fmt(s.start)}–${fmt(s.end)}`;
  if (s.leg && s.key !== 'walk') {
    return `<b>${esc(segLabel(s))}${s.leg.towards ? ` to ${esc(s.leg.towards)}` : ''}</b>${esc(short(s.leg.from))} → ${esc(short(s.leg.to))}<br>${span}${s.leg.delayMinutes > 0 ? ` · ${s.leg.delayMinutes} min late` : ''}${s.cancelled ? ' · cancelled' : ''}`;
  }
  return `<b>${esc(segLabel(s))}</b>${span}`;
}

/* ---------- Small components ---------- */

function pill(s) {
  if (s.key === 'wait') return '';
  const soft = s.key === 'walk' || s.key === 'park';
  const text = soft ? `${minutesOf(s)} min` : s.key === 'drive' ? `${minutesOf(s)} min` : esc(s.leg.line ?? MODE_NAMES[s.key]);
  const title = esc(segLabel(s));
  return `<span class="pill ${soft ? 'soft' : ''}" style="--c:${colour(s.key)}" title="${title}"><span class="mi">${icon(MODE_ICONS[s.key])}</span>${text}<span class="sr-only"> ${title}</span></span>`;
}
const pills = (segs) => segs.filter((s) => s.key !== 'wait').map(pill).join(icon('chevron', 'sep'));

function strip(segs) {
  const total = segs.reduce((m, s) => m + Math.max(1, s.end - s.start), 0);
  return `<div class="j-strip" aria-hidden="true">${segs.map((s) =>
    `<i class="${s.key}" style="--c:${colour(s.key)};flex:${(Math.max(1, s.end - s.start) / total).toFixed(4)}"></i>`).join('')}</div>`;
}

function status(trip) {
  if (trip.cancelled) return { cls: 'bad', icon: 'x', text: 'Cancelled' };
  if (trip.maxDelayMinutes > 0) return { cls: 'warn', icon: 'clock', text: `${trip.maxDelayMinutes} min late` };
  if (trip.maxDelayMinutes < 0) return { cls: 'good', icon: 'check', text: `${-trip.maxDelayMinutes} min early` };
  if (trip.realtime) return { cls: 'good', icon: 'check', text: 'On time' };
  return { cls: 'neutral', icon: 'clock', text: 'Timetabled' };
}
const statusBadge = (t) => { const s = status(t); return `<span class="status ${s.cls}">${icon(s.icon)}${s.text}</span>`; };

function steps(segs, destination) {
  const items = [];
  for (const s of segs) {
    if (s.key === 'wait') continue;
    let title;
    let detail = '';
    if (s.key === 'drive') {
      title = `Drive ${minutesOf(s)} min`;
      detail = `${s.drive.distanceKm} km${s.station ? ` to ${esc(s.station)}` : ''}${s.drive.hazards?.length ? ` · ${plural(s.drive.hazards.length, 'traffic hazard')} on route` : ''}`;
    } else if (s.key === 'park') {
      title = s.toCar ? `Walk to the car, ${minutesOf(s)} min` : `Park and walk to the platform, ${minutesOf(s)} min`;
    } else if (s.key === 'walk') {
      title = `Walk ${minutesOf(s)} min`;
      detail = `to ${esc(s.leg.to)}`;
    } else {
      const l = s.leg;
      title = `${esc(MODE_NAMES[s.key] ?? l.mode)} ${esc(l.line ?? '')}${l.towards ? ` towards ${esc(l.towards)}` : ''}`;
      detail = `${esc(l.from)} → ${esc(l.to)}, arrive ${fmt(s.end)}${l.delayMinutes > 0 ? ` · ${l.delayMinutes} min late` : ''}${l.cancelled ? ' · cancelled' : ''}`;
    }
    items.push(`<li class="step ${s.key}" style="--c:${colour(s.key)}">
      <span class="step-time">${fmt(s.start)}</span>
      <span class="step-rail"><span class="node">${icon(MODE_ICONS[s.key])}</span><span class="wire"></span></span>
      <div class="step-body"><b>${title}</b>${detail ? `<div>${detail}</div>` : ''}</div></li>`);
  }
  const end = segs.at(-1).end;
  items.push(`<li class="step end" style="--c:var(--ink)"><span class="step-time">${fmt(end)}</span>
    <span class="step-rail"><span class="node" style="background:var(--ink);color:var(--surface)">${icon('check')}</span></span>
    <div class="step-body"><b>Arrive</b><div>${esc(destination)}</div></div></li>`);
  return `<details class="steps"><summary>${icon('chevron')}Step-by-step</summary><ol class="step-list">${items.join('')}</ol></details>`;
}

function journeyCard({ segs, depart, arrive, minutes, best, cancelled, leaveBy, meta, notices = [], destination }) {
  return `<li class="journey ${best ? 'best' : ''} ${cancelled ? 'cancelled' : ''}">
    <div class="j-main">
      <div>
        ${leaveBy ? '<div class="j-leave">Leave by</div>' : ''}
        <div class="j-times">${fmt(depart)}<span class="arrow">→</span>${fmt(arrive)}${best ? '<span class="status good">' + icon('check') + 'Recommended</span>' : ''}</div>
      </div>
      <div class="j-dur"><strong>${minutes}</strong> min</div>
      ${strip(segs)}
      <div class="j-legs">${pills(segs)}</div>
      <div class="j-meta">${meta}</div>
      ${notices.map((n) => `<div class="notice">${icon('alert')}<span>${esc(n)}</span></div>`).join('')}
    </div>
    ${steps(segs, destination)}
  </li>`;
}

/* ---------- Panels ---------- */

function renderParkRide(r) {
  const rec = r.recommendation;
  const parkAt = r.query.parkAt;
  if (!r.parkRide.length) return '<p class="panel-empty">Add one or more stations under Park and ride to compare driving to a station.</p>';
  return r.parkRide.map((s, si) => {
    const head = `<div class="station-head"><span class="pill" style="--c:${colour('park')}"><span class="mi">${icon('park')}</span>${esc(short(s.station.name))}</span>
      <span class="muted">${parkAt === 'end' ? 'Public transport to the station, then drive' : 'Drive to the station, then public transport'}</span></div>`;
    if (s.error) return `<div class="station">${head}<div class="banner error">${icon('alert')}<span>${esc(s.error)}</span></div></div>`;
    if (!s.options.length) return `<div class="station">${head}<p class="panel-empty">No connecting services found.</p></div>`;
    const cards = s.options.map((o, i) => {
      const segs = parkRideSegments(o, parkAt);
      const first = o.trip.legs.find((l) => l.mode !== 'Walk');
      const meta = `${statusBadge(o.trip)}<span>${parkAt === 'end'
        ? `Reach ${esc(short(s.station.name))} ${fmt(o.trip.arrive)}`
        : `Catch ${esc(first?.line ?? 'service')} at ${fmt(o.trip.depart)} from ${esc(first?.fromPlatform ?? short(s.station.name))}`}</span>
        ${o.trip.interchanges ? `<span>${plural(o.trip.interchanges, 'change')}</span>` : ''}`;
      const notices = [...o.drive.hazards.map((h) => `${h.headline}${h.road ? ` — ${h.road}` : ''}${h.delayMinutes ? ` (+${h.delayMinutes} min)` : ''}`), ...o.trip.alerts];
      return journeyCard({
        segs, depart: o.depart, arrive: o.arrive, minutes: o.totalMinutes, cancelled: o.cancelled,
        best: rec?.kind === 'parkride' && rec.stationIndex === si && rec.index === i,
        leaveBy: parkAt !== 'end', meta, notices, destination: r.destination.name,
      });
    }).join('');
    return `<div class="station">${head}<ol class="journeys">${cards}</ol></div>`;
  }).join('');
}

function renderTransit(r) {
  if (!r.trips.length) {
    return r.errors.transit ? `<div class="banner error">${icon('alert')}<span>${esc(r.errors.transit)}</span></div>` : '<p class="panel-empty">No journeys found.</p>';
  }
  const rec = r.recommendation;
  return `<ol class="journeys">${r.trips.map((j, i) => {
    const first = j.legs.find((l) => l.mode !== 'Walk');
    const meta = `${statusBadge(j)}${first ? `<span>${esc(first.line ?? first.mode)} from ${esc(short(first.from))}${first.fromPlatform ? `, ${esc(first.fromPlatform)}` : ''} at ${fmt(first.depart)}</span>` : ''}
      <span>${j.interchanges ? plural(j.interchanges, 'change') : 'No changes'}</span>`;
    return journeyCard({
      segs: journeySegments(j), depart: j.depart, arrive: j.arrive, minutes: j.durationMinutes, cancelled: j.cancelled,
      best: rec?.kind === 'transit' && rec.index === i, meta, notices: j.alerts, destination: r.destination.name,
    });
  }).join('')}</ol>`;
}

function renderDrive(r) {
  const d = r.drive;
  if (!d) return `<div class="banner error">${icon('alert')}<span>Driving estimate unavailable: ${esc(r.errors.drive ?? 'unknown error')}</span></div>`;
  const model = d.source !== 'google';
  const stats = [
    ['Leave', fmt(d.depart)], ['Arrive', fmt(d.arrive)], ['Distance', `${d.distanceKm} km`],
    ...(model
      ? [['Free-flow', `${d.freeFlowMinutes} min`], ['Congestion', `×${d.congestionFactor}`], ['Hazard delay', `+${d.incidentDelayMinutes} min`]]
      : [['Without traffic', `${d.freeFlowMinutes} min`]]),
  ];
  const hazards = d.hazards.length
    ? `<ul class="hazards">${d.hazards.map((h) => `<li class="hazard ${h.isMajor ? 'major' : ''}">
        <span class="hi">${icon('alert')}</span>
        <div><b>${esc(h.headline)}</b><div>${esc([h.road, h.suburb].filter(Boolean).join(', '))}${h.advice ? ` · ${esc(h.advice)}` : ''}</div></div>
        ${h.delayMinutes ? `<span class="status ${h.isMajor ? 'bad' : 'warn'}">+${h.delayMinutes} min</span>` : ''}</li>`).join('')}</ul>`
    : `<p class="clear">${icon('check')}No live traffic hazards reported on this route.</p>`;
  return `<div class="drive-top">
      <div><div class="j-leave">Door to door by car</div><div class="big">${d.totalMinutes}<small>min</small></div></div>
      <div class="j-legs">${pills(driveSegments(d))}</div>
    </div>
    <div class="stats">${stats.map(([k, v]) => `<div class="stat"><span>${k}</span><strong>${esc(v)}</strong></div>`).join('')}</div>
    ${hazards}
    <p class="method">${model
      ? 'Model estimate: free-flow route time × Sydney time-of-day congestion profile, plus an allowance for each live hazard within 250 m of the route.'
      : 'Traffic-aware estimate from Google Routes. Hazards are shown for information.'}</p>`;
}

/* ---------- Comparison timeline ---------- */

function pickBest(options, r) {
  const live = options.filter((o) => !o.cancelled);
  if (!live.length) return null;
  const when = new Date(r.query.when);
  if (r.query.arriveBy) {
    const onTime = live.filter((o) => new Date(o.arrive) <= when);
    if (onTime.length) return onTime.reduce((a, b) => (new Date(b.depart) > new Date(a.depart) ? b : a));
  }
  return live.reduce((a, b) => (new Date(b.arrive) < new Date(a.arrive) ? b : a));
}

function comparisonRows(r) {
  const rec = r.recommendation;
  const rows = [];
  if (r.drive) rows.push({ kind: 'drive', label: 'Drive', phrase: 'driving', icon: 'drive', segs: driveSegments(r.drive), depart: r.drive.depart, arrive: r.drive.arrive, minutes: r.drive.totalMinutes, best: rec?.kind === 'drive' });
  const bestTrip = rec?.kind === 'transit' ? r.trips[rec.index] : pickBest(r.trips, r);
  if (bestTrip) {
    rows.push({ kind: 'transit', label: 'Public transport', phrase: 'public transport only', icon: 'train', segs: journeySegments(bestTrip), depart: bestTrip.depart, arrive: bestTrip.arrive, minutes: bestTrip.durationMinutes, best: rec?.kind === 'transit' });
  }
  r.parkRide.forEach((s, si) => {
    const o = rec?.kind === 'parkride' && rec.stationIndex === si ? s.options[rec.index] : pickBest(s.options, r);
    if (o) rows.push({ kind: 'parkride', label: `Via ${short(s.station.name)}`, phrase: `park and ride via ${short(s.station.name)}`, icon: 'park', segs: parkRideSegments(o, r.query.parkAt), depart: o.depart, arrive: o.arrive, minutes: o.totalMinutes, best: rec?.kind === 'parkride' && rec.stationIndex === si });
  });
  return rows;
}

function renderTimeline(r) {
  const rows = comparisonRows(r);
  if (!rows.length) return { html: '<p class="panel-empty">No options to compare.</p>', keys: [] };
  const deadline = r.query.arriveBy ? new Date(r.query.when).getTime() : null;
  const times = rows.flatMap((row) => row.segs.flatMap((s) => [s.start, s.end]));
  if (deadline) times.push(deadline);
  // Aim for a tick label roughly every 70px of track.
  const trackPx = Math.max(200, $('timeline').clientWidth - (innerWidth > 860 ? 332 : 0));
  const spanMin = (Math.max(...times) - Math.min(...times)) / MIN;
  const step = [15, 30, 60].find((m) => (trackPx / (spanMin / m)) >= 70) * MIN || 60 * MIN;
  const t0 = Math.floor(Math.min(...times) / step) * step;
  const t1 = Math.ceil(Math.max(...times) / step) * step;
  const pct = (t) => (((t - t0) / (t1 - t0)) * 100).toFixed(3);
  const ticks = [];
  for (let t = t0; t <= t1; t += step) ticks.push(t);

  const keys = new Set();
  const rowHtml = rows.map((row, ri) => {
    const segs = row.segs.filter((s) => s.key !== 'wait').map((s, si) => {
      keys.add(s.key);
      const w = Math.max(0.6, pct(s.end) - pct(s.start));
      return `<span class="tl-seg ${s.key} ${s.cancelled ? 'cancelled' : ''}" tabindex="0" data-tip="${ri}:${si}"
        style="--c:${colour(s.key)};left:${pct(s.start)}%;width:${w}%" aria-label="${esc(segLabel(s))}, ${fmt(s.start)} to ${fmt(s.end)}">${w > 4 ? icon(MODE_ICONS[s.key]) : ''}</span>`;
    }).join('');
    return `<div class="tl-row ${row.best ? 'best' : ''}">
      <div class="tl-label">${icon(row.icon, 'ico')}<strong>${esc(row.label)}</strong>${row.best ? '<span class="tag">Best</span>' : ''}</div>
      <div class="tl-track">${segs}</div>
      <div class="tl-value">${fmt(row.depart)}–${fmt(row.arrive)} · <b>${row.minutes}</b> min</div>
    </div>`;
  }).join('');

  const nearDeadline = (t) => deadline && Math.abs(pct(t) - pct(deadline)) < 9;
  const tickLabel = (t, i) => (nearDeadline(t) ? '' : `<span class="${i === 0 ? 'first' : i === ticks.length - 1 ? 'last' : ''}" style="left:${pct(t)}%">${fmt(t)}</span>`);
  const html = `<div class="tl-axis">${ticks.map(tickLabel).join('')}</div>
    <div class="tl-rows">
      <div class="tl-grid">${ticks.map((t) => `<i style="left:${pct(t)}%"></i>`).join('')}
        ${deadline ? `<i class="deadline" style="left:${pct(deadline)}%"><b>Arrive by ${fmt(deadline)}</b></i>` : ''}</div>
      ${rowHtml}
    </div>`;
  tipRows = rows;
  return { html, keys: [...keys] };
}

let tipRows = [];
function bindTooltips(container) {
  const tip = $('tooltip');
  const show = (el) => {
    const [ri, si] = el.dataset.tip.split(':').map(Number);
    const seg = tipRows[ri]?.segs.filter((s) => s.key !== 'wait')[si];
    if (!seg) return;
    tip.innerHTML = segTooltip(seg);
    tip.hidden = false;
    const b = el.getBoundingClientRect();
    const tw = tip.offsetWidth;
    tip.style.left = `${Math.min(window.innerWidth - tw - 8, Math.max(8, b.left + b.width / 2 - tw / 2))}px`;
    tip.style.top = `${b.top - tip.offsetHeight - 8}px`;
  };
  const hide = () => { tip.hidden = true; };
  container.querySelectorAll('.tl-seg').forEach((el) => {
    el.addEventListener('mouseenter', () => show(el));
    el.addEventListener('focus', () => show(el));
    el.addEventListener('mouseleave', hide);
    el.addEventListener('blur', hide);
  });
}

/* ---------- Hero ---------- */

function renderHero(r) {
  const rec = r.recommendation;
  if (!rec) return `<div><span class="eyebrow">${icon('alert')}No option available</span><p class="muted">No driving or public transport option could be found for this trip.</p></div>`;
  const rows = comparisonRows(r);
  const best = rows.find((row) => row.best);
  const others = rows.filter((row) => !row.best);
  let compare = '';
  if (others.length) {
    const next = r.query.arriveBy
      ? others.reduce((a, b) => (new Date(b.depart) > new Date(a.depart) ? b : a))
      : others.reduce((a, b) => (new Date(b.arrive) < new Date(a.arrive) ? b : a));
    const diff = r.query.arriveBy
      ? Math.round((new Date(rec.depart) - new Date(next.depart)) / MIN)
      : Math.round((new Date(next.arrive) - new Date(rec.arrive)) / MIN);
    if (diff > 0) compare = `<span>${r.query.arriveBy ? `Leave ${diff} min later than` : `Arrive ${diff} min earlier than`} ${esc(next.phrase)}</span>`;
    else if (diff === 0) compare = `<span>Level with ${esc(next.phrase)}</span>`;
  }
  const untilLeave = Math.round((new Date(rec.depart) - Date.now()) / MIN);
  const soon = untilLeave >= 0 && untilLeave <= 120 ? ` <span class="status ${untilLeave <= 5 ? 'warn' : 'neutral'}">${icon('clock')}${untilLeave === 0 ? 'Now' : `in ${untilLeave} min`}</span>` : '';
  return `<div>
      <span class="eyebrow">${icon('check')}Recommended · ${esc(rec.label)}</span>
      <div class="hero-main">
        <div class="hero-leave num"><small>Leave</small>${fmt(rec.depart)}</div>
        <div class="hero-arrive num">Arrive <b>${fmt(rec.arrive)}</b>${soon}</div>
      </div>
    </div>
    <div class="hero-duration"><strong class="num">${rec.minutes}</strong><span>minutes door to door</span></div>
    <div class="hero-sub">${best ? `<div class="j-legs">${pills(best.segs)}</div>` : ''}${compare}<span>${esc(rec.reason)}</span></div>`;
}

/* ---------- Page state ---------- */

let refreshTimer = null;
let requestSeq = 0;
let activeTab = null;

function selectTab(name) {
  activeTab = name;
  document.querySelectorAll('[role=tab]').forEach((t) => {
    const on = t.dataset.tab === name;
    t.setAttribute('aria-selected', String(on));
    t.tabIndex = on ? 0 : -1;
    $(t.getAttribute('aria-controls')).hidden = !on;
  });
}

function showBanner(kind, text) {
  $('banner').className = `banner ${kind}`;
  $('banner').innerHTML = `${icon('alert')}<span>${esc(text)}</span>`;
  $('banner').hidden = false;
}

function render(r) {
  $('banner').hidden = true;
  const notes = [];
  if (r.errors.traffic) notes.push(`Live traffic unavailable (${r.errors.traffic}); driving times exclude hazards.`);
  if (r.errors.transit && r.trips.length === 0 && r.drive) notes.push(`Public transport unavailable: ${r.errors.transit}`);
  if (notes.length) showBanner('warn', notes.join(' '));

  $('route-title').textContent = `${short(r.origin.name)} → ${short(r.destination.name)}`;
  const whenText = r.query.arriveBy ? `Arrive by ${fmt(r.query.when)}` : `Depart ${fmt(r.query.when)}`;
  $('route-meta').textContent = `${whenText} · Updated ${fmt(r.generatedAt)}${r.query.railOnly ? ' · Trains only' : ''}`;
  $('hero').innerHTML = renderHero(r);

  const tl = renderTimeline(r);
  $('timeline').innerHTML = tl.html;
  $('legend').innerHTML = tl.keys.map((k) => `<span><i style="--c:${colour(k)}"></i>${MODE_NAMES[k] ?? k}</span>`).join('');
  bindTooltips($('timeline'));

  $('panel-parkride').innerHTML = renderParkRide(r);
  $('panel-transit').innerHTML = renderTransit(r);
  $('panel-drive').innerHTML = renderDrive(r);
  $('tab-parkride').hidden = !r.parkRide.length;
  $('tab-parkride').querySelector('.count').textContent = r.parkRide.reduce((n, s) => n + s.options.length, 0);
  $('tab-transit').querySelector('.count').textContent = r.trips.length;

  const kind = r.recommendation?.kind;
  const preferred = kind === 'parkride' ? 'parkride' : kind === 'drive' ? 'drive' : kind === 'transit' ? 'transit' : 'transit';
  const keep = activeTab && !(activeTab === 'parkride' && !r.parkRide.length);
  selectTab(keep ? activeTab : preferred);

  $('empty').hidden = true;
  $('skeleton').hidden = true;
  $('content').hidden = false;
}

const whenMode = () => document.querySelector('input[name=when]:checked').value;
const parkAt = () => document.querySelector('input[name=parkAt]:checked').value;

async function api(path, params) {
  const res = await fetch(`${path}?${new URLSearchParams(params)}`);
  const body = await res.json();
  if (!res.ok) throw new Error(body.error ?? `HTTP ${res.status}`);
  return body;
}

async function estimate({ quiet = false } = {}) {
  const mode = whenMode();
  if (!$('from').value.trim() || !$('to').value.trim()) {
    showBanner('error', 'Enter a start and a destination.');
    return;
  }
  const params = { from: $('from').value, to: $('to').value, mode: mode === 'arrive' ? 'arrive' : 'depart', via: $('via').value, park: $('park').value || '0', parkAt: parkAt() };
  if (mode !== 'now') Object.assign(params, { time: $('time').value, date: $('date').value });
  if ($('rail').checked) params.railOnly = '1';
  for (const k of ['from', 'to', 'via', 'park']) store.set(k, $(k).value);
  store.set('parkAt', params.parkAt);

  const seq = ++requestSeq;
  clearTimeout(refreshTimer);
  $('submit').setAttribute('aria-busy', 'true');
  if (!quiet || $('content').hidden) {
    $('empty').hidden = true;
    $('content').hidden = true;
    $('skeleton').hidden = false;
  }
  try {
    const result = await api('/api/commute', params);
    if (seq === requestSeq) render(result);
  } catch (err) {
    if (seq !== requestSeq) return;
    $('skeleton').hidden = true;
    if ($('content').hidden) $('empty').hidden = false;
    showBanner('error', err.message);
  } finally {
    if (seq === requestSeq) {
      $('submit').removeAttribute('aria-busy');
      scheduleRefresh();
    }
  }
}

function scheduleRefresh() {
  clearTimeout(refreshTimer);
  if ($('auto').checked && !$('content').hidden) refreshTimer = setTimeout(() => estimate({ quiet: true }), REFRESH_MS);
}

function sydneyToday() {
  const p = Object.fromEntries(new Intl.DateTimeFormat('en-AU', { timeZone: TZ, year: 'numeric', month: '2-digit', day: '2-digit' })
    .formatToParts(new Date()).map((x) => [x.type, x.value]));
  return `${p.year}-${p.month}-${p.day}`;
}

function syncWhenFields() {
  const mode = whenMode();
  $('when-fields').hidden = mode === 'now';
  if (mode !== 'now' && !$('time').value) $('time').value = mode === 'arrive' ? '09:00' : '08:00';
  if (!$('date').value) $('date').value = sydneyToday();
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
      list.innerHTML = items.map((l) => `<option value="${esc(l.name)}"></option>`).join('');
    } catch { /* suggestions are optional */ }
  }, 300));
}

function applyTheme(theme) {
  if (theme) document.documentElement.dataset.theme = theme;
  const dark = theme ? theme === 'dark' : matchMedia('(prefers-color-scheme: dark)').matches;
  $('theme').innerHTML = icon(dark ? 'sun' : 'moon');
  $('theme').setAttribute('aria-label', dark ? 'Switch to light theme' : 'Switch to dark theme');
}

async function init() {
  applyTheme(store.get('theme'));
  $('theme').addEventListener('click', () => {
    const dark = document.documentElement.dataset.theme
      ? document.documentElement.dataset.theme === 'dark'
      : matchMedia('(prefers-color-scheme: dark)').matches;
    const next = dark ? 'light' : 'dark';
    store.set('theme', next);
    applyTheme(next);
  });

  const cfg = await api('/api/config', {}).catch(() => ({}));
  $('live').hidden = false;
  $('live').classList.toggle('demo', Boolean(cfg.demo));
  $('live-text').textContent = cfg.demo ? 'Demo data' : 'Live';

  $('from').value = store.get('from') || cfg.home || '';
  $('to').value = store.get('to') || cfg.work || '';
  $('via').value = store.get('via') ?? cfg.via ?? '';
  $('park').value = store.get('park') ?? cfg.parkMinutes ?? 5;
  document.querySelector(`input[name=parkAt][value=${store.get('parkAt') === 'end' ? 'end' : 'start'}]`).checked = true;

  attachSuggestions($('from'), $('from-list'));
  attachSuggestions($('to'), $('to-list'));
  document.querySelectorAll('input[name=when]').forEach((el) => el.addEventListener('change', syncWhenFields));
  $('auto').addEventListener('change', scheduleRefresh);
  $('refresh').addEventListener('click', () => estimate({ quiet: true }));
  $('swap').addEventListener('click', () => {
    [$('from').value, $('to').value] = [$('to').value, $('from').value];
    // The car stays at the station, so the return trip drives last.
    document.querySelector(`input[name=parkAt][value=${parkAt() === 'end' ? 'start' : 'end'}]`).checked = true;
  });
  document.querySelectorAll('[role=tab]').forEach((t) => {
    t.addEventListener('click', () => selectTab(t.dataset.tab));
    t.addEventListener('keydown', (e) => {
      if (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft') return;
      const tabs = [...document.querySelectorAll('[role=tab]:not([hidden])')];
      const next = tabs[(tabs.indexOf(t) + (e.key === 'ArrowRight' ? 1 : tabs.length - 1)) % tabs.length];
      selectTab(next.dataset.tab);
      next.focus();
    });
  });
  $('form').addEventListener('submit', (e) => { e.preventDefault(); estimate(); });
  syncWhenFields();

  if (!cfg.hasApiKey) showBanner('error', 'The server has no TFNSW_API_KEY configured. See the README to add one, or run the demo.');
  else if ($('from').value && $('to').value) estimate();
}

init();
