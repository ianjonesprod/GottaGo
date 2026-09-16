export interface LatLng {
  latitude: number;
  longitude: number;
}

export interface Bounds {
  north: number;
  south: number;
  east: number;
  west: number;
}

/** Roughly how many miles one degree of latitude covers. Close enough anywhere on Earth. */
const MILES_PER_DEGREE_LATITUDE = 69.0;

/**
 * A box of roughly `radiusMiles` in every direction from a point.
 *
 * Used instead of a hard-coded zoom level, because a zoom of 10 shows wildly different areas
 * on a phone and on a desktop monitor. Handing the map a real-world box and letting it pick
 * the zoom gives the same coverage on any screen.
 *
 * Longitude lines converge toward the poles, so a degree of longitude is narrower the further
 * you are from the equator - hence dividing by the cosine of the latitude. The clamp stops
 * the maths exploding near the poles, where that cosine approaches zero.
 */
export function boundsAround(centre: LatLng, radiusMiles: number): Bounds {
  const latitudeDelta = radiusMiles / MILES_PER_DEGREE_LATITUDE;

  const latitudeRadians = (centre.latitude * Math.PI) / 180;
  const milesPerDegreeLongitude = MILES_PER_DEGREE_LATITUDE * Math.max(Math.cos(latitudeRadians), 0.01);
  const longitudeDelta = radiusMiles / milesPerDegreeLongitude;

  return {
    north: clampLatitude(centre.latitude + latitudeDelta),
    south: clampLatitude(centre.latitude - latitudeDelta),
    east: centre.longitude + longitudeDelta,
    west: centre.longitude - longitudeDelta,
  };
}

function clampLatitude(value: number): number {
  return Math.min(90, Math.max(-90, value));
}
