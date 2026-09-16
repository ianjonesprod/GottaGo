import { Injectable, signal } from '@angular/core';

/**
 * A nudge that says "the bathroom data changed, reload it".
 *
 * Adding a bathroom or posting a review happens in one feature, but the map and the results
 * list in another need to know - otherwise a bathroom you just added has no pin, and a
 * rating you just gave does not move the average you are looking at.
 *
 * It lives in core because features are not allowed to import each other. Anything that
 * changes data bumps the counter; anything showing data includes it in its query, so a bump
 * re-runs the fetch.
 */
@Injectable({ providedIn: 'root' })
export class BathroomChanges {
  private readonly counter = signal(0);

  /** Include this in a resource's params to make it reload whenever data changes. */
  readonly version = this.counter.asReadonly();

  notifyChanged(): void {
    this.counter.update((value) => value + 1);
  }
}
