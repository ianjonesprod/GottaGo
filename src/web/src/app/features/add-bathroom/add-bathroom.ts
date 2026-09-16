import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { Announcer } from '../../core/a11y/announcer.service';
import { BathroomChanges } from '../../core/api/bathroom-changes';
import { GottaGoApi } from '../../core/api/gotta-go-api';
import { DIMENSION_LABELS, RATING_DIMENSIONS } from '../../core/api/models/bathroom.model';
import { StarRatingInput } from '../../shared/star-rating-input/star-rating-input';

/** Matches the server's VenueKind. Shown as a picker rather than expecting a magic string. */
const VENUES = [
  'Library',
  'Park',
  'Transit',
  'Museum',
  'Stadium',
  'Market',
  'Airport',
  'Campus',
  'RecreationCenter',
  'Other',
] as const;

/**
 * Adds a bathroom at a dropped pin.
 *
 * The coordinates arrive in the URL, so the form is reachable and shareable without having
 * to click the map - which also means someone who cannot use the map can still add a place
 * by typing an address and coordinates.
 */
@Component({
  selector: 'gg-add-bathroom',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    StarRatingInput,
  ],
  templateUrl: './add-bathroom.html',
  styleUrl: '../review-submit/review-submit.scss',
})
export class AddBathroom {
  private readonly api = inject(GottaGoApi);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly announcer = inject(Announcer);
  private readonly formBuilder = inject(FormBuilder);
  private readonly changes = inject(BathroomChanges);

  private readonly heading = viewChild<ElementRef<HTMLElement>>('heading');

  protected readonly venues = VENUES;
  protected readonly dimensions = RATING_DIMENSIONS;
  protected readonly labels = DIMENSION_LABELS;
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
    street: ['', [Validators.maxLength(200)]],
    city: ['Cleveland', [Validators.required, Validators.maxLength(100)]],
    state: ['OH', [Validators.required, Validators.maxLength(50)]],
    postalCode: ['', [Validators.maxLength(20)]],
    venue: ['Other', [Validators.required]],
    accessNote: ['', [Validators.maxLength(300)]],
    body: ['', [Validators.maxLength(2000)]],
    smell: this.formBuilder.control<number | null>(null),
    cleanliness: this.formBuilder.control<number | null>(null),
    amenities: this.formBuilder.control<number | null>(null),
    accessibility: this.formBuilder.control<number | null>(null),
    ambience: this.formBuilder.control<number | null>(null),
  });

  /*
    Read from the live query params, not a snapshot. Clicking the map again while this panel
    is open changes the URL without rebuilding the component, so a snapshot taken once would
    keep showing the first spot you clicked while the pin moved.
  */
  private readonly params = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });

  protected readonly latitude = computed(() => Number(this.params().get('lat') ?? 41.4993));

  protected readonly longitude = computed(() => Number(this.params().get('lng') ?? -81.6944));

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.announcer.say('Cannot submit yet. Check the form for errors.');
      this.heading()?.nativeElement.focus();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue();

    // The first review is optional, but it is all or nothing: a partial set of scores would
    // skew the average, so it only counts when every dimension is rated.
    const allRated = this.dimensions.every((dimension) => value[dimension] !== null);

    this.api
      .createBathroom({
        name: value.name,
        description: value.description || null,
        street: value.street || null,
        city: value.city,
        state: value.state,
        postalCode: value.postalCode || null,
        latitude: this.latitude(),
        longitude: this.longitude(),
        venue: value.venue,
        accessNote: value.accessNote || null,
        firstReviewScores:
          allRated && value.body
            ? {
                smell: value.smell!,
                cleanliness: value.cleanliness!,
                amenities: value.amenities!,
                accessibility: value.accessibility!,
                ambience: value.ambience!,
              }
            : null,
        firstReviewBody: allRated && value.body ? value.body : null,
      })
      .subscribe({
        next: (created) => {
          // Tell the map and the results list to refetch, so the new pin actually appears
          // rather than the bathroom existing but being invisible until a reload.
          this.changes.notifyChanged();
          this.announcer.say(`${created.name} added.`);
          void this.router.navigate(['/map', created.slug]);
        },
        error: () => {
          this.submitting.set(false);
          this.errorMessage.set("Couldn't add that bathroom. Check the details and try again.");
        },
      });
  }
}
