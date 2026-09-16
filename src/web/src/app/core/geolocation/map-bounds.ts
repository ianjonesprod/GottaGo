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

/** The average position of a set of points. For a single point, that point. */
export function centroidOf(points: readonly LatLng[]): LatLng {
  const total = points.reduce(
    (sum, point) => ({
      latitude: sum.latitude + point.latitude,
      longitude: sum.longitude + point.longitude,
    }),
    { latitude: 0, longitude: 0 },
  );

  return {
    latitude: total.latitude / points.length,
    longitude: total.longitude / points.length,
  };
}

/**
 * A box centred exactly on `focus` that still contains every point in `alsoInclude`.
 *
 * Fitting a box that merely contains a set of points puts the middle of that set at the
 * centre of the screen. When the set is "the one result plus where I am", the result ends
 * up off to one side. Growing the box symmetrically instead - the same distance past the
 * focus as the furthest point sits before it - keeps the focus dead centre and brings
 * everything else along, at the cost of showing some empty space on the far side.
 *
 * The minimum span stops a point that coincides with the focus producing a zero-size box,
 * which Google Maps answers by zooming as far in as it can go.
 */
export function boundsCentredOn(
  focus: LatLng,
  alsoInclude: readonly LatLng[],
  minimumSpanDegrees = 0.01,
): Bounds {
  let latitudeDelta = minimumSpanDegrees;
  let longitudeDelta = minimumSpanDegrees;

  for (const point of alsoInclude) {
    latitudeDelta = Math.max(latitudeDelta, Math.abs(point.latitude - focus.latitude));
    longitudeDelta = Math.max(longitudeDelta, Math.abs(point.longitude - focus.longitude));
  }

  return {
    north: Math.min(90, focus.latitude + latitudeDelta),
    south: Math.max(-90, focus.latitude - latitudeDelta),
    east: focus.longitude + longitudeDelta,
    west: focus.longitude - longitudeDelta,
  };
}
