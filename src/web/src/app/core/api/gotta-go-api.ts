import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import type {
  BathroomDetailDto,
  BathroomSummaryDto,
  HighScoreEntryDto,
  PagedResponseDto,
  ReviewDto,
} from './dto/api.dto';
import { toBathroom, toHighScore, toPaged, toReview } from './mappers/api.mapper';
import type { Bathroom, HighScore, Paged, Review } from './models/bathroom.model';

export interface BathroomSearch {
  keyword?: string;
  bounds?: { north: number; south: number; east: number; west: number };
  near?: { latitude: number; longitude: number };
  radiusMiles?: number;
  venue?: string;
  sort?: 'distance' | 'rating' | 'reviewCount' | 'name';
  page?: number;
  pageSize?: number;
}

/**
 * The single place in the app that talks HTTP.
 *
 * Components inject this and get models back; they never see a DTO, a URL or an HttpClient.
 * That is what lets the backend change shape without the UI noticing.
 */
@Injectable({ providedIn: 'root' })
export class GottaGoApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  searchBathrooms(search: BathroomSearch = {}): Observable<Paged<Bathroom>> {
    let params = new HttpParams();

    if (search.keyword) {
      params = params.set('q', search.keyword);
    }

    if (search.bounds) {
      params = params
        .set('north', search.bounds.north)
        .set('south', search.bounds.south)
        .set('east', search.bounds.east)
        .set('west', search.bounds.west);
    }

    if (search.near) {
      params = params.set('lat', search.near.latitude).set('lng', search.near.longitude);
    }

    if (search.radiusMiles !== undefined) {
      params = params.set('radiusMiles', search.radiusMiles);
    }

    if (search.venue) {
      params = params.set('venue', search.venue);
    }

    if (search.sort) {
      params = params.set('sort', search.sort);
    }

    params = params.set('page', search.page ?? 1).set('pageSize', search.pageSize ?? 50);

    return this.http
      .get<PagedResponseDto<BathroomSummaryDto>>(`${this.baseUrl}/bathrooms`, { params })
      .pipe(map((dto) => toPaged(dto, toBathroom)));
  }

  getBathroom(idOrSlug: string): Observable<Bathroom> {
    return this.http
      .get<BathroomDetailDto>(`${this.baseUrl}/bathrooms/${encodeURIComponent(idOrSlug)}`)
      .pipe(map(toBathroom));
  }

  getReviewsFor(bathroomId: string, page = 1, pageSize = 25): Observable<Paged<Review>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);

    return this.http
      .get<PagedResponseDto<ReviewDto>>(`${this.baseUrl}/bathrooms/${bathroomId}/reviews`, { params })
      .pipe(map((dto) => toPaged(dto, toReview)));
  }

  getRecentReviews(keyword?: string, page = 1, pageSize = 25): Observable<Paged<Review>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);

    if (keyword) {
      params = params.set('q', keyword);
    }

    return this.http
      .get<PagedResponseDto<ReviewDto>>(`${this.baseUrl}/reviews`, { params })
      .pipe(map((dto) => toPaged(dto, toReview)));
  }

  getHighScores(dimension?: string, page = 1, pageSize = 25): Observable<Paged<HighScore>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);

    if (dimension) {
      params = params.set('dimension', dimension);
    }

    return this.http
      .get<PagedResponseDto<HighScoreEntryDto>>(`${this.baseUrl}/high-scores`, { params })
      .pipe(map((dto) => toPaged(dto, toHighScore)));
  }
}
