import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';

import { Announcer } from '../../core/a11y/announcer.service';
import { GottaGoApi } from '../../core/api/gotta-go-api';
import { GeolocationService } from '../../core/geolocation/geolocation.service';
import { boundsAround } from '../../core/geolocation/map-bounds';
import { SearchStore } from '../../core/search/search-store';
import { StarRatingDisplay } from '../../shared/star-rating-display/star-rating-display';

/**
 * The map screen.
 *
 * The results rail beside the map is not a fallback, it is the primary way the data is
 * reachable: every bathroom on the map is in the rail, in distance order, as real links.
 * Sighted keyboard users need that as much as screen reader users do, which is why it is
 * always visible rather than hidden off-screen.
 */
@Component({
  selector: 'gg-map-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
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
  protected readonly search = inject(SearchStore);

  protected readonly centre = this.geolocation.centre;
  protected readonly locationState = this.geolocation.state;
  protected readonly radiusMiles = signal(20);

  protected readonly bathrooms = rxResource({
    params: () => ({
      keyword: this.search.keyword(),
      centre: this.centre(),
      radius: this.radiusMiles(),
    }),
    stream: ({ params }) => {
      const bounds = boundsAround(params.centre, params.radius);

      return this.api.searchBathrooms({
        keyword: params.keyword || undefined,
        bounds,
        near: params.centre,
        sort: 'distance',
        pageSize: 200,
      });
    },
  });

  protected readonly results = computed(() => this.bathrooms.value()?.items ?? []);
  protected readonly total = computed(() => this.results().length);

  constructor() {
    effect(() => {
      if (this.bathrooms.isLoading() || this.bathrooms.error()) {
        return;
      }

      this.announcer.countOfBathrooms(this.total());
    });
  }

  protected useMyLocation(): void {
    this.geolocation.request();
  }
}
