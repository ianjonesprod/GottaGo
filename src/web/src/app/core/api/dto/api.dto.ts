/*
  The wire shapes, exactly as the API sends them.

  These are the only types in the app allowed to mirror the backend. Everything past the
  mapper works with the models in ../models, so a field rename on the server is a compile
  error in one mapper rather than a silent break in a dozen templates.
*/

export interface PagedResponseDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AddressDto {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  singleLine: string;
}

export interface LocationDto {
  latitude: number;
  longitude: number;
}

export interface RatingDto {
  overall: number;
  reviewCount: number;
  smell: number;
  cleanliness: number;
  amenities: number;
  accessibility: number;
  ambience: number;
}

export interface ScoresDto {
  smell: number;
  cleanliness: number;
  amenities: number;
  accessibility: number;
  ambience: number;
}

export interface PhotoDto {
  id: string;
  url: string;
  altText: string;
}

export interface BathroomSummaryDto {
  id: string;
  slug: string;
  name: string;
  description: string | null;
  address: AddressDto;
  location: LocationDto;
  venue: string;
  accessNote: string | null;
  rating: RatingDto;
  primaryPhoto: PhotoDto | null;
  isSeedData: boolean;
}

export interface BathroomDetailDto extends Omit<BathroomSummaryDto, 'primaryPhoto'> {
  photos: PhotoDto[];
}

export interface ReviewDto {
  id: string;
  bathroomId: string;
  bathroomName: string;
  bathroomSlug: string;
  authorName: string;
  headline: string | null;
  body: string;
  scores: ScoresDto;
  overall: number;
  visitedOn: string | null;
  createdAt: string;
  isSeedData: boolean;
}

export interface HighScoreEntryDto {
  rank: number;
  bathroomId: string;
  slug: string;
  name: string;
  city: string;
  rating: RatingDto;
  rankingScore: number;
  isSeedData: boolean;
}
