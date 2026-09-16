import type { LatLng } from './map-bounds';

const EARTH_RADIUS_MILES = 3958.7613;

/**
 * Great-circle distance in miles between two points.
 *
 * Computed here rather than asked of the server: the browser already knows where you are
 * and where every result is, so a round trip would add latency for arithmetic.
 */
export function milesBetween(from: LatLng, to: LatLng): number {
  const toRadians = (degrees: number) => (degrees * Math.PI) / 180;

  const lat1 = toRadians(from.latitude);
  const lat2 = toRadians(to.latitude);
  const deltaLat = toRadians(to.latitude - from.latitude);
  const deltaLng = toRadians(to.longitude - from.longitude);

  const a =
    Math.sin(deltaLat / 2) * Math.sin(deltaLat / 2) +
    Math.cos(lat1) * Math.cos(lat2) * Math.sin(deltaLng / 2) * Math.sin(deltaLng / 2);

  return EARTH_RADIUS_MILES * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

/**
 * Distance phrased the way a person would say it.
 *
 * Precision is deliberately coarse and gets coarser with distance: "2.4 miles" is useful,
 * "2.4173 miles" is noise, and at fifteen miles nobody cares about the decimal. Under a
 * quarter of a mile it switches to a walking time, which is the thing actually being asked.
 */
export function describeDistance(miles: number): string {
  if (miles < 0.25) {
    return 'a few minutes away';
  }

  if (miles < 10) {
    return `${miles.toFixed(1)} miles away`;
  }

  return `${Math.round(miles)} miles away`;
}
