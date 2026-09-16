import { describe, expect, it } from 'vitest';

import { boundsAround, boundsCentredOn, centroidOf } from './map-bounds';

const CLEVELAND = { latitude: 41.4993, longitude: -81.6944 };

describe('boundsAround', () => {
  it('covers roughly the requested radius north to south', () => {
    const bounds = boundsAround(CLEVELAND, 20);

    // 20 miles each way is about 0.29 degrees of latitude, so the box spans about 0.58.
    expect(bounds.north - bounds.south).toBeCloseTo(0.58, 2);
  });

  it('widens longitude to compensate for converging meridians', () => {
    const bounds = boundsAround(CLEVELAND, 20);

    const latitudeSpan = bounds.north - bounds.south;
    const longitudeSpan = bounds.east - bounds.west;

    // At Cleveland's latitude a degree of longitude is noticeably narrower than a degree of
    // latitude, so the box has to be wider in degrees to cover the same distance on the
    // ground. Using the same delta for both would under-cover east to west.
    expect(longitudeSpan).toBeGreaterThan(latitudeSpan);
  });

  it('is centred on the point it was given', () => {
    const bounds = boundsAround(CLEVELAND, 20);

    expect((bounds.north + bounds.south) / 2).toBeCloseTo(CLEVELAND.latitude, 6);
    expect((bounds.east + bounds.west) / 2).toBeCloseTo(CLEVELAND.longitude, 6);
  });

  it('never produces a latitude beyond the poles', () => {
    const bounds = boundsAround({ latitude: 89.9, longitude: 0 }, 500);

    expect(bounds.north).toBeLessThanOrEqual(90);
    expect(bounds.south).toBeGreaterThanOrEqual(-90);
  });

  it('does not divide by zero at the pole', () => {
    const bounds = boundsAround({ latitude: 90, longitude: 0 }, 20);

    expect(Number.isFinite(bounds.east)).toBe(true);
    expect(Number.isFinite(bounds.west)).toBe(true);
  });
});

describe('centroidOf', () => {
  it('is the point itself when there is only one', () => {
    expect(centroidOf([CLEVELAND])).toEqual(CLEVELAND);
  });

  it('is the average of several', () => {
    const middle = centroidOf([
      { latitude: 41.0, longitude: -81.0 },
      { latitude: 43.0, longitude: -83.0 },
    ]);

    expect(middle.latitude).toBeCloseTo(42.0, 6);
    expect(middle.longitude).toBeCloseTo(-82.0, 6);
  });
});

describe('boundsCentredOn', () => {
  const ROCKY_RIVER = { latitude: 41.434, longitude: -81.8454 };

  it('puts the focus exactly in the middle', () => {
    const box = boundsCentredOn(ROCKY_RIVER, [ROCKY_RIVER, CLEVELAND]);

    // This is the whole point: a single result should land dead centre, not halfway
    // between itself and wherever the viewer happens to be.
    expect((box.north + box.south) / 2).toBeCloseTo(ROCKY_RIVER.latitude, 6);
    expect((box.east + box.west) / 2).toBeCloseTo(ROCKY_RIVER.longitude, 6);
  });

  it('still contains every point it was given', () => {
    const box = boundsCentredOn(ROCKY_RIVER, [ROCKY_RIVER, CLEVELAND]);

    expect(CLEVELAND.latitude).toBeLessThanOrEqual(box.north);
    expect(CLEVELAND.latitude).toBeGreaterThanOrEqual(box.south);
    expect(CLEVELAND.longitude).toBeLessThanOrEqual(box.east);
    expect(CLEVELAND.longitude).toBeGreaterThanOrEqual(box.west);
  });

  it('never collapses to a single point', () => {
    // Google Maps answers a zero-size box by zooming as far in as it can go.
    const box = boundsCentredOn(CLEVELAND, [CLEVELAND]);

    expect(box.north).toBeGreaterThan(box.south);
    expect(box.east).toBeGreaterThan(box.west);
  });

  it('grows to hold the furthest of many points', () => {
    const spread = [
      { latitude: 41.3, longitude: -81.9 },
      { latitude: 41.6, longitude: -81.5 },
    ];
    const box = boundsCentredOn(CLEVELAND, spread);

    for (const point of spread) {
      expect(point.latitude).toBeLessThanOrEqual(box.north);
      expect(point.latitude).toBeGreaterThanOrEqual(box.south);
      expect(point.longitude).toBeLessThanOrEqual(box.east);
      expect(point.longitude).toBeGreaterThanOrEqual(box.west);
    }
  });
});
