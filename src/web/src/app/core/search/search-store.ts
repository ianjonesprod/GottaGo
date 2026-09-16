import { Injectable, signal } from '@angular/core';

/**
 * The keyword from the toolbar, shared by the map, review list and high score views.
 *
 * Lives in core rather than in one feature because all three read it, and features are not
 * allowed to import each other.
 */
@Injectable({ providedIn: 'root' })
export class SearchStore {
  private readonly keywordSignal = signal('');

  readonly keyword = this.keywordSignal.asReadonly();

  setKeyword(value: string): void {
    this.keywordSignal.set(value);
  }
}
