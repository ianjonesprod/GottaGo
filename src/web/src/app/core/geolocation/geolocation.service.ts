import { Injectable, signal } from '@angular/core';

import type { LatLng } from './map-bounds';

/** Downtown Cleveland. Where the map starts before anyone grants anything. */
export const CLEVELAND: LatLng = { latitude: 41.4993, longitude: -81.6944 };

export type LocationState = 'default' | 'locating' | 'located' | 'unavailable';

/**
 * Where to centre the map.
 *
 * Deliberately does not prompt on load. An unexpected permission dialog steals focus and
 * interrupts a screen reader mid-sentence, and browsers penalise sites that ask before the
 * user has done anything. So the map opens on Cleveland with real data immediately, and the
 * user asks for their own location by pressing a button.
 *
 * Every failure - denied, timed out, unsupported - lands back on Cleveland and says so,
 * rather than silently moving the user somewhere they did not choose.
 */
@Injectable({ providedIn: 'root' })
export class GeolocationService {
  private readonly centreSignal = signal<LatLng>(CLEVELAND);
  private readonly stateSignal = signal<LocationState>('default');

  readonly centre = this.centreSignal.asReadonly();
  readonly state = this.stateSignal.asReadonly();

  request(): void {
    if (!navigator.geolocation) {
      this.stateSignal.set('unavailable');
      return;
    }

    // Geolocation only works in a secure context. localhost counts, but testing on a phone
    // over http://192.168.x.x silently returns nothing, and the Cleveland fallback would
    // hide that. Say so in the console rather than letting it look like a denial.
    if (!window.isSecureContext) {
      console.warn(
        'Geolocation needs a secure context. Serve over HTTPS or localhost, or this will always fail.',
      );
    }

    this.stateSignal.set('locating');

    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.centreSignal.set({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
        });
        this.stateSignal.set('located');
      },
      () => {
        this.centreSignal.set(CLEVELAND);
        this.stateSignal.set('unavailable');
      },
      { enableHighAccuracy: false, timeout: 8000, maximumAge: 300_000 },
    );
  }
}
