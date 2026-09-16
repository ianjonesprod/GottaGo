import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, inject, input, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';

import { GottaGoApi } from '../../core/api/gotta-go-api';
import { DIMENSION_LABELS, RATING_DIMENSIONS, type Paged, type Review } from '../../core/api/models/bathroom.model';
import { StarRatingDisplay } from '../../shared/star-rating-display/star-rating-display';

/**
 * The panel that opens when you pick a bathroom.
 *
 * It is a routed child of the map rather than a dialog, which means the URL is shareable,
 * the browser back button closes it, and the page title updates - all things a dialog would
 * have needed hand-written code to fake.
 *
 * Focus moves here when it opens, because otherwise a keyboard user's focus is left behind
 * on the map and a screen reader never learns anything appeared.
 */
@Component({
  selector: 'gg-bathroom-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDividerModule,
    MatProgressBarModule,
    StarRatingDisplay,
  ],
  templateUrl: './bathroom-detail.html',
  styleUrl: './bathroom-detail.scss',
})
export class BathroomDetail {
  private readonly api = inject(GottaGoApi);
  private readonly router = inject(Router);

  /** Bound from the route parameter by withComponentInputBinding(). */
  readonly slug = input.required<string>();

  private readonly panel = viewChild<ElementRef<HTMLElement>>('panel');

  protected readonly dimensions = RATING_DIMENSIONS;
  protected readonly labels = DIMENSION_LABELS;

  protected readonly bathroom = rxResource({
    params: () => ({ slug: this.slug() }),
    stream: ({ params }) => this.api.getBathroom(params.slug),
  });

  // Reviews load once the bathroom is known, since the id comes from that first response.
  protected readonly reviews = rxResource<Paged<Review> | null, { id: string | undefined }>({
    params: () => ({ id: this.bathroom.value()?.id }),
    stream: ({ params }) => (params.id ? this.api.getReviewsFor(params.id) : of(null)),
  });

  protected readonly mapsUrl = computed(() => {
    const value = this.bathroom.value();

    if (!value) {
      return null;
    }

    const query = encodeURIComponent(`${value.name}, ${value.address}`);

    return `https://www.google.com/maps/search/?api=1&query=${query}`;
  });

  constructor() {
    // Move focus to the panel heading once the content it describes has arrived. Focusing
    // an empty panel would announce nothing useful.
    effect(() => {
      if (this.bathroom.isLoading() || !this.bathroom.value()) {
        return;
      }

      queueMicrotask(() => this.panel()?.nativeElement.focus());
    });
  }

  /** Escape closes the panel, the way any overlay should. */
  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.close();
    }
  }

  protected close(): void {
    void this.router.navigate(['/map']);
  }
}
