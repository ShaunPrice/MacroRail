// Driving-time model used when no traffic-aware routing provider is configured.
//
// The congestion profile is an ASSUMPTION: multipliers applied to free-flow
// driving time, shaped on published Sydney congestion patterns (AM peak around
// 08:00, PM peak around 17:00, weekends lighter). Tune it in config for your own
// route by comparing predictions against trips you have actually driven.

import { sydneyParts } from './time.js';

export const DEFAULT_CONGESTION_PROFILE = {
  //        00    01    02    03    04    05    06    07    08    09    10    11
  weekday: [1.00, 1.00, 1.00, 1.00, 1.00, 1.05, 1.20, 1.45, 1.60, 1.35, 1.20, 1.20,
  //        12    13    14    15    16    17    18    19    20    21    22    23
            1.20, 1.20, 1.25, 1.40, 1.50, 1.60, 1.40, 1.15, 1.05, 1.00, 1.00, 1.00],
  weekend: [1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.05, 1.10, 1.15, 1.25, 1.30,
            1.30, 1.30, 1.25, 1.20, 1.20, 1.15, 1.10, 1.05, 1.00, 1.00, 1.00, 1.00],
};

/** Congestion multiplier for a departure at `date`, linearly interpolated between hours. */
export function congestionFactor(date, profile = DEFAULT_CONGESTION_PROFILE) {
  const p = sydneyParts(date);
  const table = p.weekday === 0 || p.weekday === 6 ? profile.weekend : profile.weekday;
  const a = table[p.hour];
  const b = table[(p.hour + 1) % 24];
  return a + (b - a) * (p.minute / 60);
}

// Minutes of delay assumed per hazard on the route (model mode only).
export const DEFAULT_HAZARD_DELAYS = {
  major: 15,
  incident: 6,
  fire: 10,
  flood: 10,
  alpine: 5,
  majorevent: 5,
  roadwork: 3,
  default: 4,
};

export function hazardDelayMinutes(hazard, delays = DEFAULT_HAZARD_DELAYS) {
  if (hazard.isMajor) return delays.major;
  return delays[hazard.category] ?? delays.default;
}
