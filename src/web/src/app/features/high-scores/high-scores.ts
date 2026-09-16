import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';

import { Announcer } from '../../core/a11y/announcer.service';
import { GottaGoApi } from '../../core/api/gotta-go-api';
import { DIMENSION_LABELS, RATING_DIMENSIONS, type RatingDimension } from '../../core/api/models/bathroom.model';
import { StarRatingDisplay } from '../../shared/star-rating-display/star-rating-display';

@Component({
  selector: 'gg-high-scores',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    MatTableModule,
    MatButtonToggleModule,
    MatChipsModule,
    MatIconModule,
    MatProgressBarModule,
    StarRatingDisplay,
  ],
  templateUrl: './high-scores.html',
  styleUrl: './high-scores.scss',
})
export class HighScores {
  private readonly api = inject(GottaGoApi);
  private readonly announcer = inject(Announcer);

  protected readonly dimensions = RATING_DIMENSIONS;
  protected readonly labels = DIMENSION_LABELS;
  protected readonly selectedDimension = signal<RatingDimension | 'overall'>('overall');
  protected readonly columns = ['rank', 'name', 'rating', 'reviews'];

  protected readonly scores = rxResource({
    params: () => ({ dimension: this.selectedDimension() }),
    stream: ({ params }) =>
      this.api.getHighScores(params.dimension === 'overall' ? undefined : params.dimension),
  });

  protected readonly rows = computed(() => this.scores.value()?.items ?? []);

  constructor() {
    effect(() => {
      if (this.scores.isLoading() || this.scores.error()) {
        return;
      }

      const dimension = this.selectedDimension();
      const label = dimension === 'overall' ? 'overall score' : this.labels[dimension];

      this.announcer.say(`Leaderboard sorted by ${label}. ${this.rows().length} bathrooms listed.`);
    });
  }
}
