import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { GoogleMap, MapMarker } from '@angular/google-maps';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { Announcer } from '../../core/a11y/announcer.service';
import { GottaGoApi } from '../../core/api/gotta-go-api';
import type { Bathroom } from '../../core/api/models/bathroom.model';
import { GeolocationService } from '../../core/geolocation/geolocation.service';
import { boundsAround } from '../../core/geolocation/map-bounds';
import { MapsLoaderService } from '../../core/maps/maps-loader.service';
import { SearchStore } from '../../core/search/search-store';
import { StarRatingDisplay } from '../../shared/star-rating-display/star-rating-display';

/**
 * The map screen.
 *
 * The results rail beside the map is not a fallback, it is the primary way the data is
 * reachable: every bathroom on the map is in the rail, in distance order, as real links.
 * Sighted keyboard users need that as much as screen reader users do, which is why it is
 * always visible rather than hidden off-screen - and why the page still works in full when
 * the map cannot load at all.
 */
@Component({
  selector: 'gg-map-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    RouterOutlet,
    GoogleMap,
    MapMarker,
    MatCardModule,
    MatListModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressBarModule,
    StarRatingDisplay,
  ],
  templateUrl: './map-page.html',
  styleUrl: './map-page.scss',
})
export class MapPage {
  private readonly api = inject(GottaGoApi);
  private readonly announcer = inject(Announcer);
  private readonly geolocation = inject(GeolocationService);
  private readonly maps = inject(MapsLoaderService);
  private readonly router = inject(Router);
  protected readonly search = inject(SearchStore);

  protected readonly centre = this.geolocation.centre;
  protected readonly locationState = this.geolocation.state;
  protected readonly radiusMiles = signal(20);
  protected readonly mapsStatus = this.maps.status;

  protected readonly bathrooms = rxResource({
    params: () => ({
      keyword: this.search.keyword(),
      centre: this.centre(),
      radius: this.radiusMiles(),
    }),
    stream: ({ params }) =>
      this.api.searchBathrooms({
        keyword: params.keyword || undefined,
        bounds: boundsAround(params.centre, params.radius),
        near: params.centre,
        sort: 'distance',
        pageSize: 200,
      }),
  });

  protected readonly results = computed(() => this.bathrooms.value()?.items ?? []);
  protected readonly total = computed(() => this.results().length);

  protected readonly mapCentre = computed<google.maps.LatLngLiteral>(() => ({
    lat: this.centre().latitude,
    lng: this.centre().longitude,
  }));

  /**
   * Bounds covering the requested radius, which the map fits itself to. Used instead of a
   * fixed zoom level because a zoom of 10 covers very different ground on a phone and on a
   * monitor; giving it a real-world box gets the same coverage on both.
   */
  protected readonly mapBounds = computed<google.maps.LatLngBoundsLiteral>(() => {
    const box = boundsAround(this.centre(), this.radiusMiles());

    return { north: box.north, south: box.south, east: box.east, west: box.west };
  });

  protected readonly mapOptions: google.maps.MapOptions = {
    disableDefaultUI: false,
    mapTypeControl: false,
    streetViewControl: false,
    fullscreenControl: false,
    minZoom: 3,
    maxZoom: 19,
    // Keyboard users can pan and zoom the map itself; the rail is the faster path.
    keyboardShortcuts: true,
  };

  constructor() {
    // Loaded here rather than in index.html: the key is only known after the config call.
    void this.maps.load();

    effect(() => {
      if (this.bathrooms.isLoading() || this.bathrooms.error()) {
        return;
      }

      this.announcer.countOfBathrooms(this.total());
    });
  }

  protected markerOptions(bathroom: Bathroom): google.maps.MarkerOptions {
    return {
      // The pin carries the rating in its accessible name, so a marker is not just a dot
      // with no meaning to anything that cannot see it.
      title: `${bathroom.name}, rated ${bathroom.rating.overall.toFixed(1)} out of 5 from ${bathroom.rating.reviewCount} reviews`,
      position: { lat: bathroom.latitude, lng: bathroom.longitude },
    };
  }

  /**
   * Clicking empty map space starts adding a bathroom there. The coordinates travel in the
   * URL so the form works without the map too - somebody who cannot use a map can still
   * reach it and type an address.
   */
  protected addHere(event: google.maps.MapMouseEvent): void {
    const position = event.latLng;

    if (!position) {
      return;
    }

    void this.router.navigate(['/map', 'new'], {
      queryParams: { lat: position.lat(), lng: position.lng() },
    });
  }

  protected openBathroom(bathroom: Bathroom): void {
    void this.router.navigate(['/map', bathroom.slug]);
  }

  protected useMyLocation(): void {
    this.geolocation.request();
  }
}
