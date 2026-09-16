import { ChangeDetectionStrategy, Component, ElementRef, inject, input, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';

import { Announcer } from '../../core/a11y/announcer.service';
import { GottaGoApi } from '../../core/api/gotta-go-api';
import { DIMENSION_LABELS, RATING_DIMENSIONS } from '../../core/api/models/bathroom.model';
import { StarRatingInput } from '../../shared/star-rating-input/star-rating-input';

/**
 * The review form.
 *
 * A routed panel rather than a dialog: five required rating groups and a comment box is a
 * page, not something to cram into a modal, and it gets a shareable URL and back-button
 * behaviour for free.
 */
@Component({
  selector: 'gg-review-submit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    StarRatingInput,
  ],
  templateUrl: './review-submit.html',
  styleUrl: './review-submit.scss',
})
export class ReviewSubmit {
  private readonly api = inject(GottaGoApi);
  private readonly router = inject(Router);
  private readonly announcer = inject(Announcer);
  private readonly formBuilder = inject(FormBuilder);

  readonly slug = input.required<string>();

  private readonly heading = viewChild<ElementRef<HTMLElement>>('heading');

  protected readonly dimensions = RATING_DIMENSIONS;
  protected readonly labels = DIMENSION_LABELS;
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly bathroom = rxResource({
    params: () => ({ slug: this.slug() }),
    stream: ({ params }) => this.api.getBathroom(params.slug),
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    headline: ['', [Validators.maxLength(120)]],
    body: ['', [Validators.required, Validators.maxLength(2000)]],
    smell: this.formBuilder.control<number | null>(null, Validators.required),
    cleanliness: this.formBuilder.control<number | null>(null, Validators.required),
    amenities: this.formBuilder.control<number | null>(null, Validators.required),
    accessibility: this.formBuilder.control<number | null>(null, Validators.required),
    ambience: this.formBuilder.control<number | null>(null, Validators.required),
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();

      const missing = this.countMissingRatings();

      // Say what is wrong rather than only colouring things red, and put focus on the first
      // thing that needs attention so a keyboard user does not have to hunt for it.
      this.announcer.say(
        missing > 0
          ? `Cannot submit yet. ${missing} ${missing === 1 ? 'rating is' : 'ratings are'} missing.`
          : 'Cannot submit yet. Check the form for errors.',
      );

      this.focusFirstInvalid();

      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const id = this.bathroom.value()?.id;

    if (!id) {
      this.submitting.set(false);
      this.errorMessage.set('Still loading this bathroom. Try again in a moment.');
      return;
    }

    const value = this.form.getRawValue();

    this.api
      .submitReview(id, {
        headline: value.headline || null,
        body: value.body,
        scores: {
          smell: value.smell!,
          cleanliness: value.cleanliness!,
          amenities: value.amenities!,
          accessibility: value.accessibility!,
          ambience: value.ambience!,
        },
      })
      .subscribe({
        next: () => {
          this.announcer.say('Review posted. Thank you.');
          void this.router.navigate(['/map', this.slug()]);
        },
        error: () => {
          this.submitting.set(false);
          this.errorMessage.set("Couldn't post that review. Try again.");
        },
      });
  }

  private countMissingRatings(): number {
    return this.dimensions.filter((dimension) => this.form.controls[dimension].invalid).length;
  }

  private focusFirstInvalid(): void {
    const host = this.heading()?.nativeElement.closest('.review-form');
    const firstInvalid = host?.querySelector<HTMLElement>(
      'fieldset:has([aria-invalid="true"]) input, .ng-invalid input, textarea.ng-invalid',
    );

    firstInvalid?.focus();
  }
}
