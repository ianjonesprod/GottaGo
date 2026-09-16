import { describe, expect, it } from 'vitest';

import { boundsAround } from './map-bounds';

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
