/*
  What the rest of the app works with. Shaped for rendering rather than for transport.
*/

/** The five things GottaGo scores, in the order they are shown everywhere. */
export const RATING_DIMENSIONS = [
  'smell',
  'cleanliness',
  'amenities',
  'accessibility',
  'ambience',
] as const;

export type RatingDimension = (typeof RATING_DIMENSIONS)[number];

/** Human labels, so no template ever capitalises a key by hand. */
export const DIMENSION_LABELS: Record<RatingDimension, string> = {
  smell: 'Smell',
  cleanliness: 'Cleanliness',
  amenities: 'Amenities',
  accessibility: 'Accessibility',
  ambience: 'Ambience',
};

export interface Rating {
  overall: number;
  reviewCount: number;
  byDimension: Record<RatingDimension, number>;
}

export interface Photo {
  id: string;
  url: string;
  altText: string;
}

export interface Bathroom {
  id: string;
  slug: string;
  name: string;
  description: string | null;
  address: string;
  city: string;
  latitude: number;
  longitude: number;
  venue: string;
  accessNote: string | null;
  rating: Rating;
  photos: Photo[];
  isDemoData: boolean;
}

export interface Review {
  id: string;
  bathroomId: string;
  bathroomName: string;
  bathroomSlug: string;
  authorName: string;
  headline: string | null;
  body: string;
  scores: Record<RatingDimension, number>;
  overall: number;
  visitedOn: Date | null;
  createdAt: Date;
  isDemoData: boolean;
}

export interface HighScore {
  rank: number;
  bathroomId: string;
  slug: string;
  name: string;
  city: string;
  rating: Rating;
  rankingScore: number;
  isDemoData: boolean;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
