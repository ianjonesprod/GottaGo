import { LiveAnnouncer } from '@angular/cdk/a11y';
import { inject, Injectable } from '@angular/core';

/**
 * Speaks changes that are otherwise only visible.
 *
 * Result counts are the big one: when a filter changes the number of bathrooms on screen,
 * a sighted user sees the list redraw and a screen reader user gets nothing at all unless
 * somebody says so.
 */
@Injectable({ providedIn: 'root' })
export class Announcer {
  private readonly live = inject(LiveAnnouncer);
  private lastMessage = '';

  say(message: string): void {
    // Repeating an identical message is noise, and panning a map fires a lot of updates.
    if (message === this.lastMessage) {
      return;
    }

    this.lastMessage = message;
    void this.live.announce(message, 'polite');
  }

  countOfBathrooms(count: number, context = 'in this area'): void {
    this.say(count === 1 ? `1 bathroom ${context}` : `${count} bathrooms ${context}`);
  }
}
