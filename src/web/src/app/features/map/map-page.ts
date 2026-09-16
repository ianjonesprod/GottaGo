import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, viewChild } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { filter, map as rxMap, startWith } from 'rxjs';
import { GoogleMap, MapMarker } from '@angular/google-maps';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, convertToParamMap, NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';

import { Announcer } from '../../core/a11y/announcer.service';
import { BathroomChanges } from '../../core/api/bathroom-changes';
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
  private readonly changes = inject(BathroomChanges);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  protected readonly search = inject(SearchStore);

  /** The add form puts its coordinates in the URL, so the pin follows the URL. */
  private readonly queryParams = toSignal(this.route.queryParamMap, {
    initialValue: convertToParamMap({}),
  });

  protected readonly centre = this.geolocation.centre;
  protected readonly locationState = this.geolocation.state;
  protected readonly radiusMiles = signal(20);
  protected readonly mapsStatus = this.maps.status;

  protected readonly bathrooms = rxResource({
    params: () => ({
      keyword: this.search.keyword(),
      centre: this.centre(),
      radius: this.radiusMiles(),
      // Reading this makes the search re-run whenever a bathroom is added or reviewed.
      changed: this.changes.version(),
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

  /**
   * Where a new bathroom would go, while the add form is open.
   *
   * Without it the form talks about coordinates the user cannot see, and a pin dropped by
   * clicking the map leaves no trace of where they clicked. This marker is the answer to
   * "where am I actually adding this?".
   */
  protected readonly pendingPin = computed<google.maps.LatLngLiteral | null>(() => {
    const params = this.queryParams();
    const lat = Number(params.get('lat'));
    const lng = Number(params.get('lng'));

    if (!Number.isFinite(lat) || !Number.isFinite(lng) || !params.has('lat')) {
      return null;
    }

    return { lat, lng };
  });

  protected readonly pendingPinOptions: google.maps.MarkerOptions = {
    title: 'New bathroom will be added here',
    // Visibly different from a real listing: this is a proposal, not a place that exists yet.
    icon: {
      path: 0,
      scale: 10,
      fillColor: '#0b4f79',
      fillOpacity: 0.9,
      strokeColor: '#ffffff',
      strokeWeight: 3,
    },
    zIndex: 1000,
  };

  private readonly mapRef = viewChild(GoogleMap);

  /** How close to zoom when a bathroom is opened. Street level, but still showing context. */
  private static readonly SelectedZoom = 16;

  /**
   * The slug currently open in the detail panel, taken from the URL.
   *
   * Read from the URL rather than from the click handler so the map follows however the
   * bathroom was opened - a marker, a row in the results list, a link from the leaderboard,
   * or a pasted address.
   */
  private readonly selectedSlug = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      startWith(null),
      rxMap(() => {
        const match = /^\/map\/([^/?#]+)/.exec(this.router.url);
        const slug = match?.[1];

        return slug && slug !== 'new' ? decodeURIComponent(slug) : null;
      }),
    ),
    { initialValue: null },
  );

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

    effect(() => this.focusSelectedOnMap());
    effect(() => this.fitToResultsWhenSearchChanges());
  }

  /**
   * Reframes the map around whatever a search left behind.
   *
   * Filtering to one bathroom on the far side of the county is useless if the map stays
   * pointed where it was. Only fires when the search text actually changes - refitting on
   * every result update would fight with anyone panning or zooming by hand.
   */
  private fitToResultsWhenSearchChanges(): void {
    const keyword = this.search.keyword();
    const results = this.results();
    const map = this.mapRef()?.googleMap;

    if (!map || this.bathrooms.isLoading()) {
      return;
    }

    if (keyword === this.lastFittedKeyword) {
      return;
    }

    this.lastFittedKeyword = keyword;

    // A search that found nothing has nothing to frame; leave the view alone.
    if (results.length === 0) {
      return;
    }

    // Opening a bathroom takes priority - that zoom is more specific than this one.
    if (this.selectedSlug()) {
      return;
    }

    const bounds = new google.maps.LatLngBounds();

    for (const bathroom of results) {
      bounds.extend({ lat: bathroom.latitude, lng: bathroom.longitude });
    }

    map.fitBounds(bounds, 48);

    // fitBounds on a single point zooms as far in as it will go, which is disorienting.
    if (results.length === 1 && (map.getZoom() ?? 0) > MapPage.SelectedZoom) {
      map.setZoom(MapPage.SelectedZoom);
    }
  }

  /** The search text the map was last reframed for, so it only refits when that changes. */
  private lastFittedKeyword: string | null = null;

  /**
   * Sets the opening view once the map exists.
   *
   * Only frames the whole search area when nothing is selected. Opening a link straight to
   * a bathroom would otherwise zoom to it and then get yanked back out, because the map
   * finishes initialising after that.
   */
  protected onMapReady(map: google.maps.Map): void {
    if (this.selectedSlug()) {
      this.focusSelectedOnMap();
      return;
    }

    map.fitBounds(this.mapBounds());
  }

  /**
   * Centres and zooms the map on whichever bathroom is open.
   *
   * Glides there normally, but jumps straight to it when the visitor has asked for reduced
   * motion - a map sliding across the screen is exactly the kind of movement that setting
   * exists to prevent.
   */
  private focusSelectedOnMap(): void {
    const slug = this.selectedSlug();
    const map = this.mapRef()?.googleMap;

    if (!slug || !map) {
      return;
    }

    const bathroom = this.results().find((candidate) => candidate.slug === slug);

    if (!bathroom) {
      return;
    }

    const position = { lat: bathroom.latitude, lng: bathroom.longitude };
    const prefersReducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;

    if (prefersReducedMotion) {
      map.setCenter(position);
    } else {
      map.panTo(position);
    }

    // Only ever zoom in. Someone who deliberately zoomed further than this should not be
    // yanked back out just for opening a panel.
    if ((map.getZoom() ?? 0) < MapPage.SelectedZoom) {
      map.setZoom(MapPage.SelectedZoom);
    }
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
