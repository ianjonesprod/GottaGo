import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * A read-only score shown as stars.
 *
 * The stars are decorative and hidden from assistive technology; the accessible name is the
 * number itself. A screen reader hears "Cleanliness: 4.4 out of 5" rather than counting star
 * glyphs, and the numeral is also rendered visibly so the score never depends on being able
 * to distinguish filled from unfilled shapes.
 */
@Component({
  selector: 'gg-star-rating-display',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './star-rating-display.scss',
  template: `
    <span class="rating" role="img" [attr.aria-label]="accessibleLabel()">
      <span class="stars" aria-hidden="true">
        @for (star of stars(); track $index) {
          <span class="star" [class.star--filled]="star === 'full'" [class.star--half]="star === 'half'"
            >★</span
          >
        }
      </span>
      <span class="value" aria-hidden="true">{{ displayValue() }}</span>
    </span>
  `,
})
export class StarRatingDisplay {
  readonly value = input.required<number>();
  readonly label = input<string>('Rating');
  readonly reviewCount = input<number | null>(null);

  protected readonly displayValue = computed(() => this.value().toFixed(1));

  protected readonly stars = computed(() => {
    const value = this.value();

    return Array.from({ length: 5 }, (_, index) => {
      if (value >= index + 1) {
        return 'full';
      }

      return value >= index + 0.5 ? 'half' : 'empty';
    });
  });

  protected readonly accessibleLabel = computed(() => {
    const count = this.reviewCount();
    const base = `${this.label()}: ${this.displayValue()} out of 5`;

    if (count === null) {
      return base;
    }

    // "1 review" reads better than "1 reviews" and this is read aloud, so it matters.
    return `${base}, from ${count} ${count === 1 ? 'review' : 'reviews'}`;
  });
}
