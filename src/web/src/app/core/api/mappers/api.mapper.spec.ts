import { describe, expect, it } from 'vitest';

import type { BathroomDetailDto, BathroomSummaryDto, ReviewDto } from '../dto/api.dto';
import { toBathroom, toPaged, toReview } from './api.mapper';

/*
  These are the most valuable tests in the frontend: this mapping is the boundary that keeps
  a backend field rename from reaching a template.
*/

const summary: BathroomSummaryDto = {
  id: 'b1',
  slug: 'west-side-market',
  name: 'West Side Market',
  description: 'Historic market hall.',
  address: {
    street: '1979 W 25th St',
    city: 'Cleveland',
    state: 'OH',
    postalCode: '44113',
    singleLine: '1979 W 25th St, Cleveland, OH 44113',
  },
  location: { latitude: 41.4847, longitude: -81.703 },
  venue: 'Market',
  accessNote: 'Market hours only.',
  rating: {
    overall: 4.2,
    reviewCount: 6,
    smell: 3.8,
    cleanliness: 4.4,
    amenities: 4,
    accessibility: 4.6,
    ambience: 4.2,
  },
  primaryPhoto: { id: 'p1', url: '/assets/demo/placeholder-1.svg', altText: 'A placeholder.' },
  isSeedData: true,
};

describe('toBathroom', () => {
  it('flattens the address to the single line templates render', () => {
    expect(toBathroom(summary).address).toBe('1979 W 25th St, Cleveland, OH 44113');
  });

  it('reshapes ratings into a dimension lookup', () => {
    const rating = toBathroom(summary).rating;

    expect(rating.byDimension.cleanliness).toBe(4.4);
    expect(rating.byDimension.smell).toBe(3.8);
    expect(rating.reviewCount).toBe(6);
  });

  it('renames the wire is-seed-data flag to the term the UI uses', () => {
    expect(toBathroom(summary).isDemoData).toBe(true);
  });

  it('treats a summary primary photo as a one-item gallery', () => {
    expect(toBathroom(summary).photos).toHaveLength(1);
  });

  it('copes with a summary that has no photo at all', () => {
    expect(toBathroom({ ...summary, primaryPhoto: null }).photos).toEqual([]);
  });

  it('uses the full gallery when given a detail response', () => {
    const detail: BathroomDetailDto = {
      ...summary,
      photos: [
        { id: 'p1', url: '/a.svg', altText: 'One.' },
        { id: 'p2', url: '/b.svg', altText: 'Two.' },
      ],
    };

    expect(toBathroom(detail).photos.map((p) => p.id)).toEqual(['p1', 'p2']);
  });
});

describe('toReview', () => {
  const review: ReviewDto = {
    id: 'r1',
    bathroomId: 'b1',
    bathroomName: 'West Side Market',
    bathroomSlug: 'west-side-market',
    authorName: 'Ada P.',
    headline: 'Surprisingly good',
    body: 'Clean and well stocked.',
    scores: { smell: 4, cleanliness: 5, amenities: 3, accessibility: 5, ambience: 4 },
    overall: 4.2,
    visitedOn: '2026-09-01',
    createdAt: '2026-09-02T10:30:00Z',
    isSeedData: true,
  };

  it('parses the wire date strings into real dates', () => {
    const mapped = toReview(review);

    expect(mapped.createdAt).toBeInstanceOf(Date);
    expect(mapped.visitedOn?.getUTCFullYear()).toBe(2026);
  });

  it('leaves an absent visit date as null rather than an invalid date', () => {
    expect(toReview({ ...review, visitedOn: null }).visitedOn).toBeNull();
  });

  it('keeps all five scores', () => {
    expect(toReview(review).scores).toEqual({
      smell: 4,
      cleanliness: 5,
      amenities: 3,
      accessibility: 5,
      ambience: 4,
    });
  });
});

describe('toPaged', () => {
  it('maps items while preserving the paging numbers', () => {
    const result = toPaged(
      { items: [summary], page: 2, pageSize: 10, totalCount: 25, totalPages: 3 },
      toBathroom,
    );

    expect(result.items[0].name).toBe('West Side Market');
    expect(result.page).toBe(2);
    expect(result.totalCount).toBe(25);
    expect(result.totalPages).toBe(3);
  });
});
