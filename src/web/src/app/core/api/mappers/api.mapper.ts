import type {
  BathroomDetailDto,
  BathroomSummaryDto,
  HighScoreEntryDto,
  PagedResponseDto,
  PhotoDto,
  RatingDto,
  ReviewDto,
} from '../dto/api.dto';
import type { Bathroom, HighScore, Paged, Photo, Rating, Review } from '../models/bathroom.model';

/*
  The boundary between the API's shapes and the app's own.

  Everything here is a pure function, which is why these are the most worthwhile unit tests
  in the frontend: if the mapping is right, a backend rename cannot reach a component.
*/

export function toRating(dto: RatingDto): Rating {
  return {
    overall: dto.overall,
    reviewCount: dto.reviewCount,
    byDimension: {
      smell: dto.smell,
      cleanliness: dto.cleanliness,
      amenities: dto.amenities,
      accessibility: dto.accessibility,
      ambience: dto.ambience,
    },
  };
}

export function toPhoto(dto: PhotoDto): Photo {
  return { id: dto.id, url: dto.url, altText: dto.altText };
}

export function toBathroom(dto: BathroomSummaryDto | BathroomDetailDto): Bathroom {
  const photos =
    'photos' in dto
      ? dto.photos.map(toPhoto)
      : dto.primaryPhoto
        ? [toPhoto(dto.primaryPhoto)]
        : [];

  return {
    id: dto.id,
    slug: dto.slug,
    name: dto.name,
    description: dto.description,
    address: dto.address.singleLine,
    city: dto.address.city,
    latitude: dto.location.latitude,
    longitude: dto.location.longitude,
    venue: dto.venue,
    accessNote: dto.accessNote,
    rating: toRating(dto.rating),
    photos,
    isDemoData: dto.isSeedData,
  };
}

export function toReview(dto: ReviewDto): Review {
  return {
    id: dto.id,
    bathroomId: dto.bathroomId,
    bathroomName: dto.bathroomName,
    bathroomSlug: dto.bathroomSlug,
    authorName: dto.authorName,
    headline: dto.headline,
    body: dto.body,
    scores: {
      smell: dto.scores.smell,
      cleanliness: dto.scores.cleanliness,
      amenities: dto.scores.amenities,
      accessibility: dto.scores.accessibility,
      ambience: dto.scores.ambience,
    },
    overall: dto.overall,
    visitedOn: dto.visitedOn ? new Date(dto.visitedOn) : null,
    createdAt: new Date(dto.createdAt),
    isDemoData: dto.isSeedData,
  };
}

export function toHighScore(dto: HighScoreEntryDto): HighScore {
  return {
    rank: dto.rank,
    bathroomId: dto.bathroomId,
    slug: dto.slug,
    name: dto.name,
    city: dto.city,
    rating: toRating(dto.rating),
    rankingScore: dto.rankingScore,
    isDemoData: dto.isSeedData,
  };
}

export function toPaged<TDto, TModel>(
  dto: PagedResponseDto<TDto>,
  map: (item: TDto) => TModel,
): Paged<TModel> {
  return {
    items: dto.items.map(map),
    page: dto.page,
    pageSize: dto.pageSize,
    totalCount: dto.totalCount,
    totalPages: dto.totalPages,
  };
}
