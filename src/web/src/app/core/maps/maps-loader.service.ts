import { DOCUMENT, inject, Injectable, signal } from '@angular/core';

import { AppConfigService } from '../config/app-config.service';

export type MapsStatus = 'idle' | 'loading' | 'ready' | 'unavailable';

/**
 * Loads the Google Maps script on demand.
 *
 * Deliberately not loaded in index.html: the key is only known after the config call, and a
 * visitor who never opens the map should not pay for a third-party script they do not need.
 *
 * The promise is cached, so several components asking at once cause one script tag.
 */
@Injectable({ providedIn: 'root' })
export class MapsLoaderService {
  private readonly document = inject(DOCUMENT);
  private readonly config = inject(AppConfigService);
  private readonly statusSignal = signal<MapsStatus>('idle');
  private loading: Promise<boolean> | null = null;

  readonly status = this.statusSignal.asReadonly();

  load(): Promise<boolean> {
    this.loading ??= this.loadOnce();

    return this.loading;
  }

  private loadOnce(): Promise<boolean> {
    const key = this.config.mapsApiKey;

    if (!key) {
      this.statusSignal.set('unavailable');
      return Promise.resolve(false);
    }

    // Already there, e.g. loaded by a test harness stubbing the API.
    if (this.document.defaultView?.google?.maps) {
      this.statusSignal.set('ready');
      return Promise.resolve(true);
    }

    this.statusSignal.set('loading');

    return new Promise<boolean>((resolve) => {
      const script = this.document.createElement('script');
      const callbackName = '__ggMapsReady';
      const view = this.document.defaultView as (Window & Record<string, unknown>) | null;

      // Google calls this once the API is genuinely usable. The script's own load event
      // fires earlier, before google.maps.importLibrary exists, so resolving on that gives
      // the map component a half-built API and it throws.
      if (view) {
        view[callbackName] = () => {
          delete view[callbackName];
          this.statusSignal.set('ready');
          resolve(true);
        };
      }

      // `loading=async` is required for the marker library, and `v=weekly` keeps us on a
      // supported release without pinning to something that will be retired.
      script.src =
        `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(key)}` +
        `&v=weekly&libraries=marker&loading=async&callback=${callbackName}`;
      script.async = true;

      script.onerror = () => {
        // A rejected key, a blocked request or no network. The rail still works, so this is
        // a degraded map rather than a broken page.
        console.warn('Google Maps failed to load. The results list still works.');
        this.statusSignal.set('unavailable');
        resolve(false);
      };

      this.document.head.appendChild(script);
    });
  }
}
