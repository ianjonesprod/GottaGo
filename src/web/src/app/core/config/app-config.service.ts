import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AppConfig {
  googleMapsApiKey: string | null;
  googleMapsMapId: string | null;
  defaultLatitude: number;
  defaultLongitude: number;
  defaultRadiusMiles: number;
  googleSignInEnabled: boolean;
}

const FALLBACK: AppConfig = {
  googleMapsApiKey: null,
  googleMapsMapId: null,
  defaultLatitude: 41.4993,
  defaultLongitude: -81.6944,
  defaultRadiusMiles: 20,
  googleSignInEnabled: false,
};

/**
 * Settings fetched from the API once at startup.
 *
 * The Maps key lives here rather than in a committed environment file, so it never enters
 * git history and rotating it does not need a rebuild.
 *
 * If the call fails the app carries on with sensible defaults and simply has no map. Losing
 * the map is survivable; refusing to start over it is not.
 */
@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private readonly http = inject(HttpClient);
  private readonly config = signal<AppConfig>(FALLBACK);

  readonly current = this.config.asReadonly();

  get mapsApiKey(): string | null {
    return this.config().googleMapsApiKey;
  }

  async load(): Promise<void> {
    try {
      const loaded = await firstValueFrom(this.http.get<AppConfig>('/api/client-config'));
      this.config.set({ ...FALLBACK, ...loaded });
    } catch {
      console.warn('Could not load app config. Falling back to defaults; the map will not draw.');
    }
  }
}
