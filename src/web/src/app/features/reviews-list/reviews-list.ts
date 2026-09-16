import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';

import { GottaGoApi } from '../../core/api/gotta-go-api';
import { DIMENSION_LABELS, RATING_DIMENSIONS } from '../../core/api/models/bathroom.model';
import { Announcer } from '../../core/a11y/announcer.service';
import { SearchStore } from '../../core/search/search-store';
import { StarRatingDisplay } from '../../shared/star-rating-display/star-rating-display';

/**
 * Every review, newest first.
 *
 * This is also the accessible equivalent of the map: the same bathrooms, reachable and
 * reviewable entirely through text and links.
 */
@Component({
  selector: 'gg-reviews-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatDividerModule,
    MatProgressBarModule,
    StarRatingDisplay,
  ],
  templateUrl: './reviews-list.html',
  styleUrl: './reviews-list.scss',
})
export class ReviewsList {
  private readonly api = inject(GottaGoApi);
  private readonly announcer = inject(Announcer);
  protected readonly search = inject(SearchStore);

  protected readonly dimensions = RATING_DIMENSIONS;
  protected readonly labels = DIMENSION_LABELS;
  protected readonly page = signal(1);

  protected readonly reviews = rxResource({
    params: () => ({ keyword: this.search.keyword(), page: this.page() }),
    stream: ({ params }) => this.api.getRecentReviews(params.keyword || undefined, params.page),
  });

  protected readonly total = computed(() => this.reviews.value()?.totalCount ?? 0);

  constructor() {
    effect(() => {
      if (this.reviews.isLoading() || this.reviews.error()) {
        return;
      }

      const count = this.total();
      const keyword = this.search.keyword();

      this.announcer.say(
        keyword
          ? `${count} ${count === 1 ? 'review' : 'reviews'} matching ${keyword}`
          : `${count} ${count === 1 ? 'review' : 'reviews'}`,
      );
    });
  }
}
