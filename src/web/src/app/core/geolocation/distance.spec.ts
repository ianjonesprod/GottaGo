import { describe, expect, it } from 'vitest';

import { describeDistance, milesBetween } from './distance';

const CLEVELAND = { latitude: 41.4993, longitude: -81.6944 };
const ROCKY_RIVER = { latitude: 41.434, longitude: -81.8454 };

describe('milesBetween', () => {
  it('measures a real Cleveland distance', () => {
    // Downtown to the Rocky River reservation is roughly nine miles.
    expect(milesBetween(CLEVELAND, ROCKY_RIVER)).toBeCloseTo(9, 0);
  });

  it('is zero for the same point', () => {
    expect(milesBetween(CLEVELAND, CLEVELAND)).toBe(0);
  });

  it('does not care which way round the points are given', () => {
    expect(milesBetween(CLEVELAND, ROCKY_RIVER)).toBeCloseTo(
      milesBetween(ROCKY_RIVER, CLEVELAND),
      6,
    );
  });
});

describe('describeDistance', () => {
  it('talks about time when somewhere is basically here', () => {
    expect(describeDistance(0.1)).toBe('a few minutes away');
  });

  it('keeps one decimal while that is still meaningful', () => {
    expect(describeDistance(2.44)).toBe('2.4 miles away');
  });

  it('drops the decimal once it stops mattering', () => {
    // Nobody choosing between bathrooms cares about a tenth of a mile at this range.
    expect(describeDistance(14.7)).toBe('15 miles away');
  });
});
