// Geometry helpers. Coordinates are [lat, lon] in degrees throughout.

const EARTH_RADIUS_M = 6371008.8;
const toRad = (d) => (d * Math.PI) / 180;

export function haversine([lat1, lon1], [lat2, lon2]) {
  const dLat = toRad(lat2 - lat1);
  const dLon = toRad(lon2 - lon1);
  const a = Math.sin(dLat / 2) ** 2 + Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) ** 2;
  return 2 * EARTH_RADIUS_M * Math.asin(Math.sqrt(a));
}

// Local equirectangular projection; accurate to well under 1% over a commute-sized area.
function project([lat, lon], lat0) {
  return [toRad(lon) * Math.cos(toRad(lat0)) * EARTH_RADIUS_M, toRad(lat) * EARTH_RADIUS_M];
}

function pointSegmentDistance(p, a, b) {
  const [px, py] = p;
  const [ax, ay] = a;
  const [bx, by] = b;
  const dx = bx - ax;
  const dy = by - ay;
  const len2 = dx * dx + dy * dy;
  const t = len2 === 0 ? 0 : Math.max(0, Math.min(1, ((px - ax) * dx + (py - ay) * dy) / len2));
  return Math.hypot(px - (ax + t * dx), py - (ay + t * dy));
}

/** Bounding box of a polyline, padded by `padMeters`. */
export function boundingBox(line, padMeters = 0) {
  let [minLat, minLon, maxLat, maxLon] = [Infinity, Infinity, -Infinity, -Infinity];
  for (const [lat, lon] of line) {
    minLat = Math.min(minLat, lat);
    maxLat = Math.max(maxLat, lat);
    minLon = Math.min(minLon, lon);
    maxLon = Math.max(maxLon, lon);
  }
  const dLat = padMeters / 111320;
  const dLon = padMeters / (111320 * Math.cos(toRad((minLat + maxLat) / 2)));
  return { minLat: minLat - dLat, maxLat: maxLat + dLat, minLon: minLon - dLon, maxLon: maxLon + dLon };
}

export const inBox = ([lat, lon], b) => lat >= b.minLat && lat <= b.maxLat && lon >= b.minLon && lon <= b.maxLon;

/** Shortest distance in metres from point `p` to polyline `line`. */
export function distanceToPolyline(p, line) {
  if (line.length === 0) return Infinity;
  if (line.length === 1) return haversine(p, line[0]);
  const lat0 = p[0];
  const pp = project(p, lat0);
  let best = Infinity;
  let prev = project(line[0], lat0);
  for (let i = 1; i < line.length; i++) {
    const cur = project(line[i], lat0);
    best = Math.min(best, pointSegmentDistance(pp, prev, cur));
    prev = cur;
  }
  return best;
}

/** Decodes a Google encoded polyline into [lat, lon] pairs. */
export function decodePolyline(str) {
  const out = [];
  let index = 0;
  let lat = 0;
  let lon = 0;
  const next = () => {
    let result = 0;
    let shift = 0;
    let b;
    do {
      b = str.charCodeAt(index++) - 63;
      result |= (b & 0x1f) << shift;
      shift += 5;
    } while (b >= 0x20);
    return result & 1 ? ~(result >> 1) : result >> 1;
  };
  while (index < str.length) {
    lat += next();
    lon += next();
    out.push([lat / 1e5, lon / 1e5]);
  }
  return out;
}

/** Parses "lat,lon" text into a coordinate, or returns null. */
export function parseLatLon(text) {
  const m = /^\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*$/.exec(text ?? '');
  if (!m) return null;
  const lat = Number(m[1]);
  const lon = Number(m[2]);
  if (Math.abs(lat) > 90 || Math.abs(lon) > 180) return null;
  return [lat, lon];
}
